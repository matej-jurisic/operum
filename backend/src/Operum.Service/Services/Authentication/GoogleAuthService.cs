using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs.Auth;
using Operum.Model.Enums;
using Operum.Model.Models;
using RestSharp;
using System.Text.Json;

namespace Operum.Service.Services.Authentication
{
    public class GoogleAuthService(UserManager<ApplicationUser> userManager, OperumContext db, IConfiguration configuration, ILogger<GoogleAuthService> logger) : IGoogleAuthService
    {
        private readonly string googleClientId = configuration["Google:ClientId"] ?? throw new ArgumentNullException("Google:ClientId configuration missing");

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public async Task<ServiceResponse<GoogleTokenPayloadDto>> ValidateTokenAsync(string idToken)
        {
            try
            {
                var client = new RestClient("https://oauth2.googleapis.com");
                var request = new RestRequest($"/tokeninfo?id_token={idToken}", Method.Get);
                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful || response.Content == null)
                {
                    logger.LogError("Failed to validate Google token. Status: {StatusCode}, Content: {Content}",
                                     response.StatusCode, response.Content);
                    return ServiceResponse.Failure(StatusCodeEnum.BadRequest);
                }

                var payload = JsonSerializer.Deserialize<GoogleTokenPayloadDto>(response.Content, jsonOptions);
                if (payload == null)
                {
                    logger.LogError("Failed to deserialize Google token payload");
                    return ServiceResponse.Failure(StatusCodeEnum.BadRequest);
                }

                if (payload.Audience != googleClientId)
                {
                    logger.LogError("Google token audience mismatch. Expected: {Expected}, Actual: {Actual}", googleClientId, payload.Audience);
                    return ServiceResponse.Failure(StatusCodeEnum.BadRequest);
                }

                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (payload.ExpirationTime < currentTime)
                {
                    logger.LogError("Google token has expired");
                    return ServiceResponse.Failure(StatusCodeEnum.BadRequest);
                }

                return ServiceResponse.Success(payload);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating Google token");
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest);
            }
        }

        public async Task<ServiceResponse<ApplicationUser>> FindOrCreateUserAsync(GoogleTokenPayloadDto payload)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await userManager.FindByEmailAsync(payload.Email);
                if (existingUser != null)
                {
                    if (!existingUser.EmailConfirmed)
                    {
                        existingUser.EmailConfirmed = true;
                        await userManager.UpdateAsync(existingUser);
                    }

                    await transaction.CommitAsync();
                    return ServiceResponse.Success(existingUser);
                }

                var userName = await GenerateUniqueUsername(payload.GivenName ?? payload.Email.Split('@')[0]);
                var newUser = new ApplicationUser(payload.Email, userName)
                {
                    EmailConfirmed = payload.EmailVerified
                };

                var result = await userManager.CreateAsync(newUser);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create Google user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    await transaction.RollbackAsync();
                    return ServiceResponse.Failure(StatusCodeEnum.BadRequest);
                }

                var roleResult = await userManager.AddToRoleAsync(newUser, "User");
                if (!roleResult.Succeeded)
                {
                    logger.LogError("Failed to assign role to Google user: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    await transaction.RollbackAsync();
                    return ServiceResponse.Failure(StatusCodeEnum.BadRequest);
                }

                await transaction.CommitAsync();
                return ServiceResponse.Success(newUser);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Google user");
                await transaction.RollbackAsync();
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest);
            }
        }

        private async Task<string> GenerateUniqueUsername(string baseName)
        {
            var username = new string(baseName.ToLower().Where(char.IsLetterOrDigit).ToArray());
            username = username.Length >= 3 ? username : "user" + DateTime.UtcNow.Ticks % 1000000;

            var original = username;
            var counter = 1;
            while (await userManager.FindByNameAsync(username) != null)
            {
                username = $"{original}{counter++}";
            }

            return username;
        }
    }
}
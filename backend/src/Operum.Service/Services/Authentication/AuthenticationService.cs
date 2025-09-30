using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs.Auth;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.DTOs.Users;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Integrations.MailSender;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;
using Operum.Service.Services.Token;
using RestSharp;
using System.Text.Json;

namespace Operum.Service.Services.Authentication
{
    public class AuthenticationService(UserManager<ApplicationUser> userManager, IGoogleAuthService googleAuthService, IMailSender mailSender, IConfiguration configuration, SignInManager<ApplicationUser> signInManager, OperumContext db, ITokenService tokenService, IAuthorizationService authorizationService, IMapper mapper, ILogger<AuthenticationService> logger) : IAuthenticationService
    {
        private readonly string? _googleClientId = configuration["Google:ClientId"];
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };


        public async Task<Result<AuthResponseDto>> Login(LoginRequestDto loginRequest)
        {
            var normalizedCredentials = loginRequest.Credentials.ToUpper();
            var user = await userManager.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedCredentials || x.NormalizedUserName == normalizedCredentials);

            if (user == null)
            {
                logger.LogWarning("Failed login attempt. Reason: User not found.");
                return Result.Failure(StatusCodeEnum.BadRequest, "Invalid login attempt.");
            }

            if (!user.EmailConfirmed)
            {
                logger.LogWarning("Failed login attempt. Reason: Email not confirmed.");
                return Result.Failure(StatusCodeEnum.BadRequest, "Email address has not been confirmed.");
            }

            var signInResult = await signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, true);

            if (signInResult.IsLockedOut)
            {
                logger.LogWarning("Failed login attempt. Reason: User is locked out.");
                return Result.Failure(StatusCodeEnum.BadRequest, "You are currently locked out.");
            }
            if (!signInResult.Succeeded)
            {
                logger.LogWarning("Failed login attempt. Reason: Wrong password.");
                return Result.Failure(StatusCodeEnum.BadRequest, "Invalid login attempt.");
            }

            var userDto = await AuthenticateUser(user);
            logger.LogInformation("User {userId} logged in successfully.", userDto.Id);

            return Result.Success(userDto, "Successfully logged in!");
        }

        public async Task<Result> Logout()
        {
            var token = tokenService.GetRefreshToken();
            if (token != null)
            {
                var existingToken = await db.RefreshTokens
                    .AsTracking()
                    .FirstOrDefaultAsync(rt => rt.Token == token);
                if (existingToken != null)
                {
                    existingToken.IsRevoked = true;
                    await db.SaveChangesAsync();
                }
            }

            tokenService.ClearAuthCookies();
            return Result.Success();
        }

        public async Task<Result> Register(RegisterRequestDto registerRequest)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            var normalizedUserNameRequest = registerRequest.UserName.ToUpper();
            if (await userManager.Users.AnyAsync(x => x.NormalizedUserName == normalizedUserNameRequest))
            {
                await transaction.RollbackAsync();
                return Result.Failure(StatusCodeEnum.Conflict, $"User with username {registerRequest.UserName} already exists!");
            }
            var normalizedEmailRequest = registerRequest.Email.ToUpper();
            if (await userManager.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmailRequest))
            {
                await transaction.RollbackAsync();
                return Result.Failure(StatusCodeEnum.Conflict, $"User with email {registerRequest.Email} already exists!");
            }

            var newUser = new ApplicationUser(registerRequest.Email, registerRequest.UserName);
            IdentityResult registerResult = await userManager.CreateAsync(newUser, registerRequest.Password);

            if (!registerResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return Result.Failure(StatusCodeEnum.BadRequest, registerResult.Errors.Select(x => x.Description));
            }

            IdentityResult roleResult = await userManager.AddToRoleAsync(newUser, "User");
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return Result.Failure(StatusCodeEnum.InternalServerError, roleResult.Errors.Select(x => x.Description));
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(newUser);

            var baseUrl = configuration.GetValue<string?>("ServerUrl");
            if (baseUrl == null)
            {
                await transaction.RollbackAsync();
                return Result.Failure(StatusCodeEnum.InternalServerError, "Missing configuration for mail sender.");
            }
            var confirmationLink = $"?userId={newUser.Id}&token={token}";

            RestResponse mailSenderResult = await mailSender.SendMailConfirmationMail(registerRequest.UserName, registerRequest.Email, confirmationLink);
            if (!mailSenderResult.IsSuccessStatusCode)
            {
                await transaction.RollbackAsync();
                var responseContent = mailSenderResult.Content;
                logger.LogError("Failed to send confirmation mail. StatusCode: {StatusCode}, Response: {ResponseContent}",
                                mailSenderResult.StatusCode,
                                responseContent);
                return Result.Failure(StatusCodeEnum.InternalServerError, "Error sending confirmation mail.");
            }

            await transaction.CommitAsync();
            return Result.Success("A confirmation mail has been sent to your inbox!");
        }

        public async Task<Result<AuthResponseDto>> GoogleLogin(GoogleLoginRequestDto request)
        {
            try
            {
                var payload = await googleAuthService.ValidateTokenAsync(request.Credential);
                if (!payload.IsSuccess)
                {
                    return Result.Failure(StatusCodeEnum.BadRequest, "Invalid Google token.");
                }

                var user = await googleAuthService.FindOrCreateUserAsync(payload.Data);
                if (!user.IsSuccess)
                {
                    return Result.Failure(StatusCodeEnum.InternalServerError, "Failed to create or find user.");
                }

                var userDto = await AuthenticateUser(user.Data);
                logger.LogInformation("User {userId} logged in successfully via Google.", userDto.Id);

                return Result.Success(userDto, "Successfully logged in with Google!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Google authentication");
                return Result.Failure(StatusCodeEnum.InternalServerError, "Google authentication failed.");
            }
        }

        public async Task<Result<ApplicationUserDto>> GetCurrentApplicationUser()
        {
            var userId = authorizationService.GetCurrentUserDto().Id;
            var foundUser = await userManager.FindByIdAsync(userId);
            if (foundUser == null) return Result.Failure(StatusCodeEnum.BadRequest, "User not found!");
            var user = mapper.Map<ApplicationUser, ApplicationUserDto>(foundUser);
            var roles = await userManager.GetRolesAsync(foundUser);
            user.Roles = [.. roles];
            return Result.Success(user);
        }

        public async Task<Result<AuthResponseDto>> RefreshToken()
        {
            var token = tokenService.GetRefreshToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                return Result.Failure(StatusCodeEnum.Unauthorized);
            }

            var storedToken = await db.RefreshTokens
                .AsTracking()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);

            if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return Result.Failure(StatusCodeEnum.Unauthorized);
            }

            var userDto = await AuthenticateUser(storedToken.User);

            storedToken.IsRevoked = true;
            await db.SaveChangesAsync();

            return Result.Success(userDto);
        }

        public async Task<AuthResponseDto> AuthenticateUser(ApplicationUser user)
        {
            Result<DateTime> expiry = await tokenService.SetAuthTokenCookie(user);
            await tokenService.SetRefreshTokenCookie(user);

            var roles = await userManager.GetRolesAsync(user);

            return new AuthResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                TokenExpiry = expiry.Data,
                Roles = [.. roles]
            };
        }

        public async Task<Result> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return Result.Failure(StatusCodeEnum.BadRequest, "User ID and token are required");

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure(StatusCodeEnum.NotFound, "User not found");
            if (user.EmailConfirmed) return Result.Success("Email already confirmed!");

            var result = await userManager.ConfirmEmailAsync(user, token.Replace(' ', '+'));
            if (result.Succeeded)
                return Result.Success("Email confirmed successfully!");

            return Result.Failure(StatusCodeEnum.BadRequest, "Error while confirming mail address!");
        }
    }
}
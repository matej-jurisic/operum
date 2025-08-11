using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Integrations.MailSender;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;
using Operum.Service.Services.Token;
using RestSharp;

namespace Operum.Service.Services.Authentication
{
    public class AuthenticationService(UserManager<ApplicationUser> userManager, IMailSender mailSender, IConfiguration configuration, SignInManager<ApplicationUser> signInManager, OperumContext db, ITokenService tokenService, IAuthorizationService authorizationService, IMapper mapper, ILogger<AuthenticationService> logger) : IAuthenticationService
    {
        public async Task<ServiceResponse<ApplicationUserDto>> Login(LoginRequestDto loginRequest)
        {
            var normalizedCredentials = loginRequest.Credentials.ToUpper();
            var user = await userManager.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedCredentials || x.NormalizedUserName == normalizedCredentials);

            if (user == null)
            {
                logger.LogWarning("Failed login attempt for credentials {credentials}. Reason: User not found.", loginRequest.Credentials);
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "Invalid login attempt.");
            }

            if (!user.EmailConfirmed)
            {
                logger.LogWarning("Failed login attempt for credentials {credentials}. Reason: Email not confirmed.", loginRequest.Credentials);
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "Email address has not been confirmed.");
            }

            var signInResult = await signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, true);

            if (signInResult.IsLockedOut)
            {
                logger.LogWarning("Failed login attempt for credentials {credentials}. Reason: User is locked out.", loginRequest.Credentials);
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "You are currently locked out.");
            }
            if (!signInResult.Succeeded)
            {
                logger.LogWarning("Failed login attempt for credentials {credentials}. Reason: Wrong password.", loginRequest.Credentials);
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "Invalid login attempt.");
            }

            await AuthenticateUser(user);

            ApplicationUserDto userDto = new()
            {
                Id = user.Id,
                UserName = user.UserName
            };

            logger.LogInformation("User {userId} logged in successfully.", loginRequest.Credentials);

            return ServiceResponse.Success(userDto, "Successfully logged in!");
        }

        public async Task<ServiceResponse> Logout()
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
            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse> Register(RegisterRequestDto registerRequest)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            var normalizedUserNameRequest = registerRequest.UserName.ToUpper();
            if (await userManager.Users.AnyAsync(x => x.NormalizedUserName == normalizedUserNameRequest))
            {
                await transaction.RollbackAsync();
                return ServiceResponse.Failure(StatusCodeEnum.Conflict, $"User with username {registerRequest.UserName} already exists!");
            }
            var normalizedEmailRequest = registerRequest.Email.ToUpper();
            if (await userManager.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmailRequest))
            {
                await transaction.RollbackAsync();
                return ServiceResponse.Failure(StatusCodeEnum.Conflict, $"User with email {registerRequest.Email} already exists!");
            }

            var newUser = new ApplicationUser(registerRequest.Email, registerRequest.UserName);
            IdentityResult registerResult = await userManager.CreateAsync(newUser, registerRequest.Password);

            if (!registerResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, registerResult.Errors.Select(x => x.Description));
            }

            IdentityResult roleResult = await userManager.AddToRoleAsync(newUser, "User");
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return ServiceResponse.Failure(StatusCodeEnum.InternalServerError, roleResult.Errors.Select(x => x.Description));
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(newUser);

            var baseUrl = configuration.GetValue<string?>("ServerUrl");
            if (baseUrl == null)
            {
                await transaction.RollbackAsync();
                return ServiceResponse.Failure(StatusCodeEnum.InternalServerError, "Missing configuration for mail sender.");
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
                return ServiceResponse.Failure(StatusCodeEnum.InternalServerError, "Error sending confirmation mail.");
            }

            await transaction.CommitAsync();
            return ServiceResponse.Success("A confirmation mail has been sent to your inbox!");
        }

        public async Task<ServiceResponse<ApplicationUserDto>> GetCurrentApplicationUser()
        {
            var userId = authorizationService.GetCurrentUserDto().Id;
            var foundUser = await userManager.FindByIdAsync(userId);
            if (foundUser == null) return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "User not found!");
            var user = mapper.Map<ApplicationUser, ApplicationUserDto>(foundUser);
            return ServiceResponse.Success(user);
        }

        public async Task<ServiceResponse<ApplicationUserDto>> RefreshToken()
        {
            var token = tokenService.GetRefreshToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                return ServiceResponse.Failure(StatusCodeEnum.Unauthorized);
            }

            var storedToken = await db.RefreshTokens
                .AsTracking()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);

            if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return ServiceResponse.Failure(StatusCodeEnum.Unauthorized);
            }

            await AuthenticateUser(storedToken.User);

            storedToken.IsRevoked = true;
            await db.SaveChangesAsync();

            var user = storedToken.User;

            ApplicationUserDto userDto = new()
            {
                Id = user.Id,
                UserName = user.UserName
            };

            return ServiceResponse.Success(userDto);
        }
        private async Task AuthenticateUser(ApplicationUser user)
        {
            await tokenService.SetAuthTokenCookie(user);
            await tokenService.SetRefreshTokenCookie(user);
        }
    }
}

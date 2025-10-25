using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Auth;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.DTOs.Users;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;
using RestSharp;

namespace Operum.Service.Services.Authentication
{
    public class AuthenticationService(
        ILogger<AuthenticationService> logger,
        IMapper mapper,
        OperumContext db,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ICurrentUserService currentUserService,
        IMailSender mailSender,
        IGoogleAuthService googleAuthService,
        IConfiguration configuration,
        ITokenService tokenService
    ) : IAuthenticationService
    {
        public async Task<Result<AuthResponseDto>> Login(LoginDto loginRequest)
        {
            var normalizedCredentials = loginRequest.Credentials.ToUpper();
            var user = await userManager.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedCredentials || x.NormalizedUserName == normalizedCredentials);

            if (user == null)
            {
                logger.LogWarning("Failed login attempt. Reason: User not found.");
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.InvalidLoginAttempt);
            }

            if (!user.EmailConfirmed)
            {
                logger.LogWarning("Failed login attempt. Reason: Email not confirmed.");
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.EmailAddressNotConfirmed);
            }

            var signInResult = await signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, true);

            if (signInResult.IsLockedOut)
            {
                logger.LogWarning("Failed login attempt. Reason: User is locked out.");
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.AccountLockedOut);
            }
            if (!signInResult.Succeeded)
            {
                logger.LogWarning("Failed login attempt. Reason: Wrong password.");
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.InvalidLoginAttempt);
            }

            var userDto = await AuthenticateUser(user);
            logger.LogInformation("User {userId} logged in successfully.", userDto.Id);

            return Result.Success(userDto, Messages.SuccessfulLogin);
        }

        public async Task<Result> Logout()
        {
            var token = tokenService.GetRefreshToken();
            if (token != null)
            {
                var existingToken = await db.RefreshTokens
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

        public async Task<Result> Register(RegisterDto registerRequest)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            var normalizedUserNameRequest = registerRequest.UserName.ToUpper();
            if (await userManager.Users.AnyAsync(x => x.NormalizedUserName == normalizedUserNameRequest))
            {
                await transaction.RollbackAsync();
                return Result.Failure(ResultStatusCodes.Conflict, string.Format(Messages.UsernameTaken, registerRequest.UserName));
            }
            var normalizedEmailRequest = registerRequest.Email.ToUpper();
            if (await userManager.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmailRequest))
            {
                await transaction.RollbackAsync();
                return Result.Failure(ResultStatusCodes.Conflict, string.Format(Messages.UsernameTaken, registerRequest.Email));
            }

            var newUser = new User(registerRequest.Email, registerRequest.UserName);
            IdentityResult registerResult = await userManager.CreateAsync(newUser, registerRequest.Password);

            if (!registerResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return Result.Failure(ResultStatusCodes.BadRequest, registerResult.Errors.Select(x => x.Description));
            }

            IdentityResult roleResult = await userManager.AddToRoleAsync(newUser, RoleNames.User);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return Result.Failure(ResultStatusCodes.Error, roleResult.Errors.Select(x => x.Description));
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(newUser);

            var baseUrl = configuration.GetValue<string?>("ServerUrl");
            if (baseUrl == null)
            {
                await transaction.RollbackAsync();
                return Result.Failure(ResultStatusCodes.Error);
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
                return Result.Failure(ResultStatusCodes.Error, Messages.ConfirmationMailError);
            }

            await transaction.CommitAsync();
            return Result.Success(Messages.ConfirmationMailSent);
        }

        public async Task<Result<UserDto>> GetCurrentApplicationUser()
        {
            var userId = currentUserService.GetCurrentUser().Id;
            var foundUser = await userManager.FindByIdAsync(userId);
            if (foundUser == null) return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("user"));
            var user = mapper.Map<User, UserDto>(foundUser);
            var roles = await userManager.GetRolesAsync(foundUser);
            user.Roles = [.. roles];
            return Result.Success(user);
        }

        public async Task<Result<AuthResponseDto>> RefreshToken()
        {
            var token = tokenService.GetRefreshToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                return Result.Failure(ResultStatusCodes.Unauthorized);
            }

            var storedToken = await db.RefreshTokens
                .AsTracking()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);

            if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return Result.Failure(ResultStatusCodes.Unauthorized);
            }

            var userDto = await AuthenticateUser(storedToken.User);

            storedToken.IsRevoked = true;
            await db.SaveChangesAsync();

            return Result.Success(userDto);
        }

        public async Task<AuthResponseDto> AuthenticateUser(User user)
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

        public async Task<Result> ConfirmEmail(ConfirmEmailDto request)
        {
            var user = await userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("user"));
            }
            if (user.EmailConfirmed)
            {
                return Result.Success(Messages.EmailAlreadyConfirmed);
            }

            var result = await userManager.ConfirmEmailAsync(user, request.Token.Replace(' ', '+'));
            if (result.Succeeded)
                return Result.Success("Email confirmed successfully!");

            return Result.Failure(ResultStatusCodes.BadRequest, "Error while confirming mail address!");
        }

        public async Task<Result<AuthResponseDto>> LoginWithGoogle(GoogleLoginDto request)
        {
            var googleUser = await googleAuthService.GetUserInfoAsync(request.IdToken);

            if (googleUser == null)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, "Invalid Google token.");
            }

            if (!googleUser.EmailVerified)
            {
                return Result.Failure(ResultStatusCodes.BadRequest, "Email not verified by Google.");
            }

            var user = await userManager.FindByEmailAsync(googleUser.Email);

            if (user == null)
            {
                user = new User(googleUser.Email, googleUser.Email.Split('@')[0])
                {
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                    return Result.Failure(ResultStatusCodes.Error, createResult.Errors.Select(e => e.Description));

                await userManager.AddToRoleAsync(user, RoleNames.User);
            }

            var authResponse = await AuthenticateUser(user);
            return Result.Success(authResponse, Messages.SuccessfulLogin);
        }
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs;
using Operum.Model.DTOs.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;
using Operum.Service.Services.Token;

namespace Operum.Service.Services.Authentication
{
    public class AuthenticationService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, OperumContext dbContext, ITokenService tokenService, IAuthorizationService authorizationService, IMapper mapper) : IAuthenticationService
    {
        public async Task<ServiceResponse> Login(LoginRequestDto loginRequest)
        {
            var normalizedCredentials = loginRequest.Credentials.ToUpper();
            var user = await userManager.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedCredentials || x.NormalizedUserName == normalizedCredentials);

            if (user == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.Unauthorized, ["Invalid login attempt."]);
            }

            var signInResult = await signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, true);

            if (signInResult.IsLockedOut)
            {
                return ServiceResponse.Failure(StatusCodeEnum.Unauthorized);
            }
            if (!signInResult.Succeeded)
            {
                return ServiceResponse.Failure(StatusCodeEnum.Unauthorized, ["Invalid login attempt."]);
            }

            await AuthenticateUser(user);
            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse> Logout()
        {
            var token = tokenService.GetRefreshToken();
            if (token != null)
            {
                var existingToken = await dbContext.RefreshTokens
                    .AsTracking()
                    .FirstOrDefaultAsync(rt => rt.Token == token);
                if (existingToken != null)
                {
                    existingToken.IsRevoked = true;
                    await dbContext.SaveChangesAsync();
                }
            }

            tokenService.ClearAuthCookies();
            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse> Register(RegisterRequestDto registerRequest)
        {
            var normalizedUserNameRequest = registerRequest.UserName.ToUpper();
            if (await userManager.Users.AnyAsync(x => x.NormalizedUserName == normalizedUserNameRequest))
            {
                return ServiceResponse.Failure(StatusCodeEnum.Conflict, [$"User with username {registerRequest.UserName} already exists!"]);
            }
            var normalizedEmailRequest = registerRequest.Email.ToUpper();
            if (await userManager.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmailRequest))
            {
                return ServiceResponse.Failure(StatusCodeEnum.Conflict, [$"User with email {registerRequest.Email} already exists!"]);
            }

            ApplicationUser newUser = new(registerRequest.Email, registerRequest.UserName);

            IdentityResult registerResult = await userManager.CreateAsync(newUser, registerRequest.Password);
            if (!registerResult.Succeeded)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, registerResult.Errors.Select(x => x.Description));
            }

            IdentityResult roleResult = await userManager.AddToRoleAsync(newUser, "User");
            if (!roleResult.Succeeded)
            {
                return ServiceResponse.Failure(StatusCodeEnum.InternalServerError, roleResult.Errors.Select(x => x.Description));
            }

            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse<ApplicationUserDto>> GetCurrentApplicationUser()
        {
            var userId = authorizationService.GetCurrentApplicationUserDto().Id;
            var foundUser = await userManager.FindByIdAsync(userId);
            if (foundUser == null) return ServiceResponse.Failure(StatusCodeEnum.BadRequest, ["User not found!"]);
            var user = mapper.Map<ApplicationUser, ApplicationUserDto>(foundUser);
            return ServiceResponse.Success(user);
        }

        public async Task<ServiceResponse> RefreshToken()
        {
            var token = tokenService.GetRefreshToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                return ServiceResponse.Failure(StatusCodeEnum.Unauthorized);
            }

            var storedToken = await dbContext.RefreshTokens
                .AsTracking()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);

            if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return ServiceResponse.Failure(StatusCodeEnum.Unauthorized);
            }

            await AuthenticateUser(storedToken.User);

            storedToken.IsRevoked = true;
            await dbContext.SaveChangesAsync();

            return ServiceResponse.Success();
        }
        private async Task AuthenticateUser(ApplicationUser user)
        {
            await tokenService.SetAuthTokenCookie(user);
            await tokenService.SetRefreshTokenCookie(user);
        }
    }
}

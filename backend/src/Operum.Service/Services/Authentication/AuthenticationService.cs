using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.Model.Common;
using Operum.Model.DTOs;
using Operum.Model.DTOs.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Services.Authorization;
using Operum.Service.Services.Token;

namespace Operum.Service.Services.Auth
{
    public class AuthenticationService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IHttpContextAccessor httpContextAccessor, ITokenService tokenService, IAuthorizationService authorizationService) : IAuthenticationService
    {
        public async Task<ServiceResponse> Login(LoginRequestDto loginRequest)
        {
            var normalizedCredentials = loginRequest.Credentials.ToUpper();
            ApplicationUser? existingUser = await userManager.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedCredentials || x.NormalizedUserName == normalizedCredentials);

            if (existingUser == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.Forbidden, ["Invalid login attempt."]);
            }

            SignInResult? signInResult = await signInManager.CheckPasswordSignInAsync(existingUser, loginRequest.Password, true);
            if (signInResult.IsLockedOut)
            {
                return ServiceResponse.Failure(StatusCodeEnum.Forbidden);
            }
            if (!signInResult.Succeeded)
            {
                return ServiceResponse.Failure(StatusCodeEnum.Forbidden, ["Invalid login attempt."]);
            }

            await GenerateAndSetAuthCookie(existingUser);
            return ServiceResponse.Success();
        }

        public ServiceResponse Logout()
        {
            return ClearAuthCookie();
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

        public ServiceResponse<ApplicationUserDto> GetCurrentApplicationUser()
        {
            return ServiceResponse.Success(authorizationService.GetCurrentApplicationUserDto());
        }

        public ServiceResponse SetAuthCookie(string token, ApplicationUser? user = null, bool valid = true, DateTime? expires = null)
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (token == null || httpContext == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.InternalServerError);
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = valid ? DateTime.UtcNow.AddHours(6) : DateTime.UtcNow.AddDays(-1),
            };

            httpContext.Response.Cookies.Delete("AuthToken");
            httpContext.Response.Cookies.Append("AuthToken", token, cookieOptions);
            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse> GenerateAndSetAuthCookie(ApplicationUser? user = null, bool valid = true, DateTime? expires = null)
        {
            string? token = user == null ? "" : await tokenService.CreateToken(user, expires);
            return SetAuthCookie(token, user, valid, expires);
        }

        public ServiceResponse ClearAuthCookie()
        {
            return SetAuthCookie("", valid: false);
        }
    }
}
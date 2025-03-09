using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.Model.Common;
using Operum.Model.DTOs;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Services.Authorization;
using Operum.Service.Services.Token;

namespace Operum.Service.Services.Auth
{
    public class AuthenticationService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IHttpContextAccessor httpContextAccessor, ITokenService tokenService, IAuthorizationService authorizationService) : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly ITokenService _tokenService = tokenService;
        private readonly IAuthorizationService _authorizationService = authorizationService;

        public async Task<ServiceResponse> Login(LoginRequestDto loginRequest)
        {
            var normalizedCredentials = loginRequest.Credentials.ToUpper();
            ApplicationUser? existingUser = await _userManager.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedCredentials || x.NormalizedUserName == normalizedCredentials);

            if (existingUser == null) return ServiceResponse.Failure(StatusCodeEnum.Forbidden, ["Wrong credentials."]);

            SignInResult? signInResult = await _signInManager.CheckPasswordSignInAsync(existingUser, loginRequest.Password, true);
            if (signInResult.IsLockedOut) return ServiceResponse.Failure(StatusCodeEnum.Forbidden);
            if (!signInResult.Succeeded) return ServiceResponse.Failure(StatusCodeEnum.Forbidden, ["Wrong credentials."]);
            SetAuthCookie(existingUser);
            return ServiceResponse.Success();
        }

        public ServiceResponse Logout()
        {
            SetAuthCookie(valid: false);
            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse> Register(RegisterRequestDto registerRequest)
        {
            var normalizedUsernameRequest = registerRequest.Username.ToUpper();
            if (await _userManager.Users.AnyAsync(x => x.NormalizedUserName == normalizedUsernameRequest)) return ServiceResponse.Failure(StatusCodeEnum.Conflict, [$"User with username {registerRequest.Username} already exists!"]);
            var normalizedEmailRequest = registerRequest.Email.ToUpper();
            if (await _userManager.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmailRequest)) return ServiceResponse.Failure(StatusCodeEnum.Conflict, [$"User with email {registerRequest.Email} already exists!"]);

            ApplicationUser newUser = new(registerRequest.Email, registerRequest.Username);

            IdentityResult registerResult = await _userManager.CreateAsync(newUser, registerRequest.Password);
            if (!registerResult.Succeeded) return ServiceResponse.Failure(StatusCodeEnum.BadRequest, registerResult.Errors.Select(x => x.Description));

            return ServiceResponse.Success();
        }

        public ServiceResponse SetAuthCookie(ApplicationUser? user = null, bool valid = true, DateTime? expires = null)
        {
            string? token = user == null ? "" : _tokenService.CreateToken(user, expires);

            var httpContext = _httpContextAccessor.HttpContext;

            if (token == null || httpContext == null) return ServiceResponse.Failure(StatusCodeEnum.InternalServerError);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict,
                Expires = valid ? DateTime.UtcNow.AddHours(6) : DateTime.UtcNow.AddDays(-1),
            };

            httpContext.Response.Cookies.Append("AuthToken", token, cookieOptions);
            return ServiceResponse.Success();
        }

        public ServiceResponse<ApplicationUserDto> GetCurrentApplicationUser()
        {
            return ServiceResponse.Success(_authorizationService.GetCurrentApplicationUserDto());
        }

        public async Task<ServiceResponse> UpdateUserName(string newUsername)
        {
            var currentApplicationUser = _authorizationService.GetCurrentApplicationUser();
            var applicationUser = await _userManager.FindByIdAsync(currentApplicationUser.Id) ?? throw new UnauthorizedAccessException();
            applicationUser.UserName = newUsername;

            var updateResult = await _userManager.UpdateAsync(applicationUser);
            if (updateResult.Errors.Any()) return ServiceResponse.Failure(StatusCodeEnum.BadRequest, updateResult.Errors.Select(x => x.Description));

            return ServiceResponse.Success();
        }
    }
}

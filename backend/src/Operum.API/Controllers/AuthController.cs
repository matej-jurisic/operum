using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthenticationService authenticationService) : BaseController
    {
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginRequest)
        {
            return GetApiResponse(await authenticationService.Login(loginRequest));
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerRequest)
        {
            return GetApiResponse(await authenticationService.Register(registerRequest));
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            return GetApiResponse(await authenticationService.Logout());
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            return GetApiResponse(await authenticationService.RefreshToken());
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            return GetApiResponse(await authenticationService.GetCurrentApplicationUser());
        }

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto request)
        {
            return GetApiResponse(await authenticationService.ConfirmEmail(request));
        }

        [AllowAnonymous]
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
        {
            return GetApiResponse(await authenticationService.GoogleLogin(request));
        }
    }
}

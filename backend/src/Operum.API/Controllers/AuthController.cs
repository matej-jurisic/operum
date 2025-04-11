using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.Model.DTOs.Requests;
using Operum.Service.Services.Authentication;

namespace Operum.API.Controllers
{
    [ApiController, Route("api/[controller]")]
    public class AuthController(IAuthenticationService authenticationService) : BaseController
    {
        [AllowAnonymous, HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            return GetApiResponse(await authenticationService.Login(loginRequest));
        }

        [AllowAnonymous, HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerRequest)
        {
            return GetApiResponse(await authenticationService.Register(registerRequest));
        }

        [AllowAnonymous, HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            return GetApiResponse(await authenticationService.Logout());
        }

        [AllowAnonymous, HttpGet("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            return GetApiResponse(await authenticationService.RefreshToken());
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.Model.DTOs;
using Operum.Service.Services.Auth;

namespace Operum.API.Controllers
{
    [ApiController, Route("api/[controller]")]
    public class AuthController(IAuthenticationService authenticationService) : BaseController
    {
        private readonly IAuthenticationService _authenticationService = authenticationService;

        [AllowAnonymous, HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto loginRequest)
        {
            return GetApiResponse(await _authenticationService.Login(loginRequest));
        }

        [AllowAnonymous, HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto registerRequest)
        {
            return GetApiResponse(await _authenticationService.Register(registerRequest));
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return GetApiResponse(_authenticationService.Logout());
        }

        [HttpGet]
        public IActionResult GetCurrentApplicationUser()
        {
            return GetApiResponse(_authenticationService.GetCurrentApplicationUser());
        }

        [HttpPost("username")]
        public async Task<IActionResult> UpdateUserName(string newUsername)
        {
            return GetApiResponse(await _authenticationService.UpdateUserName(newUsername));
        }
    }
}

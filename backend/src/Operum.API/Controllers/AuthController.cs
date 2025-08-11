using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Operum.Model.Common;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Services.Authentication;
using System.Web;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthenticationService authenticationService, UserManager<ApplicationUser> userManager) : BaseController
    {
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            return GetApiResponse(await authenticationService.Login(loginRequest));
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerRequest)
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
        [HttpGet("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            return GetApiResponse(await authenticationService.RefreshToken());
        }

        [AllowAnonymous]
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return BadRequest("User ID and token are required");

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var decodedToken = HttpUtility.UrlDecode(token);

            var result = await userManager.ConfirmEmailAsync(user, decodedToken);
            if (result.Succeeded)
                return GetApiResponse(ServiceResponse.Success(StatusCodeEnum.Ok, "Email confirmed successfully!"));

            return GetApiResponse(ServiceResponse.Success(StatusCodeEnum.BadRequest, "Error while confirming mail address!"));
        }
    }
}

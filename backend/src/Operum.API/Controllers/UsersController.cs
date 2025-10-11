using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Users.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(IRolesService rolesService, IUsersService usersService, IAuthenticationService authenticationService) : BaseController
    {
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentApplicationUser()
        {
            return GetApiResponse(await authenticationService.GetCurrentApplicationUser());
        }

        [HttpGet("me/roles")]
        public IActionResult GetCurrentUserRoles()
        {
            return GetApiResponse(rolesService.GetCurrentUserRoles());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            return GetApiResponse(await usersService.GetAllUsers());
        }

        [HttpGet]
        public async Task<IActionResult> GetUserList([FromQuery] string search)
        {
            return GetApiResponse(await usersService.SearchUsers(search));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            return GetApiResponse(await rolesService.GetAllRoles());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{userId}/role")]
        public async Task<IActionResult> ChangeUserRole([FromRoute] string userId, [FromBody] ChangeUserRoleDto request)
        {
            return GetApiResponse(await rolesService.ChangeUserRole(userId, request));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{userId}/confirm-email")]
        public async Task<IActionResult> ConfirmUserEmail([FromRoute] string userId)
        {
            return GetApiResponse(await usersService.ConfirmUserEmail(userId));
        }
    }
}

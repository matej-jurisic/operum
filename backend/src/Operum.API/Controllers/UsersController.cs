using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.Model.DTOs.Requests;
using Operum.Service.Services.Auth;
using Operum.Service.Services.Roles;
using Operum.Service.Services.Users;

namespace Operum.API.Controllers
{
    [ApiController, Route("api/[controller]")]
    public class UsersController(IRolesService rolesService, IUsersService usersService, IAuthenticationService authenticationService) : BaseController
    {
        [HttpGet("me")]
        public IActionResult GetCurrentApplicationUser()
        {
            return GetApiResponse(authenticationService.GetCurrentApplicationUser());
        }

        [HttpGet("me/roles")]
        public IActionResult GetCurrentUserRoles()
        {
            return GetApiResponse(rolesService.GetCurrentUserRoles());
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            return GetApiResponse(await usersService.GetAllUsers());
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateApplicationUserRequestDto request)
        {
            return GetApiResponse(await usersService.UpdateApplicationUser(request));
        }

        [HttpPost("{userId}/roles")]
        public async Task<IActionResult> AddUserToRoles([FromRoute] string userId, [FromBody] ModifyUserRoleRequestDto request)
        {
            return GetApiResponse(await rolesService.AddUserToRole(userId, request));
        }

        [HttpDelete("{userId}/roles")]
        public async Task<IActionResult> RemoveUserFromRoles([FromRoute] string userId, [FromBody] ModifyUserRoleRequestDto request)
        {
            return GetApiResponse(await rolesService.RemoveUserFromRole(userId, request));
        }
    }
}

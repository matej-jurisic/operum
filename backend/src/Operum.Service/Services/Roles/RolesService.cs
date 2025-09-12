using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Roles
{
    public class RolesService(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IAuthorizationService authorizationService, OperumContext db) : IRolesService
    {
        public async Task<ServiceResponse> ChangeUserRole(string userId, ModifyUserRoleRequestDto request)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, $"User with ID: '{userId}' does not exist.");
            }

            if (userId == authorizationService.GetCurrentUserDto().Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, $"You can't change your own roles.");
            }

            var roleExists = await roleManager.RoleExistsAsync(request.RoleName);
            if (!roleExists)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, $"Role '{request.RoleName}' does not exist.");
            }

            var userInRole = await userManager.IsInRoleAsync(user, request.RoleName);
            if (userInRole)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, $"User is already in role '{request.RoleName}'");
            }

            return await HandleRoleChange(user, request.RoleName);
        }

        private async Task<ServiceResponse> HandleRoleChange(ApplicationUser user, string roleName)
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            var result = await userManager.AddToRoleAsync(user, roleName);

            if (!result.Succeeded)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, result.Errors.Select(e => e.Description));
            }
            return ServiceResponse.Success();
        }

        public ServiceResponse<List<string>> GetCurrentUserRoles()
        {
            var roles = authorizationService.GetCurrentUserRoles();
            return ServiceResponse.Success(roles);
        }

        public async Task<ServiceResponse<List<string>>> GetAllRoles()
        {
            var roles = await roleManager.Roles.Select(x => x.Name).Where(x => x != null).Cast<string>().ToListAsync() ?? [];
            return ServiceResponse.Success(roles);
        }
    }
}

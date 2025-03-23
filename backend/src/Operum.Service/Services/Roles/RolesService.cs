using Microsoft.AspNetCore.Identity;
using Operum.Model.Common;
using Operum.Model.DTOs.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Roles
{
    public class RolesService(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IAuthorizationService authorizationService) : IRolesService
    {
        public async Task<ServiceResponse> AddUserToRole(string userId, ModifyUserRoleRequestDto request)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, [$"User with ID: '{userId}' does not exist."]);
            }

            var roleExists = await roleManager.RoleExistsAsync(request.RoleName);
            if (!roleExists)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, [$"Role '{request.RoleName}' does not exist."]);
            }

            var userInRole = await userManager.IsInRoleAsync(user, request.RoleName);
            if (userInRole)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, [$"User is already in role '{request.RoleName}'"]);
            }

            return await HandleRoleAddition(user, request.RoleName);
        }

        public async Task<ServiceResponse> RemoveUserFromRole(string userId, ModifyUserRoleRequestDto request)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, [$"User with ID: '{userId}' does not exist."]);
            }

            var roleExists = await roleManager.RoleExistsAsync(request.RoleName);
            if (!roleExists)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, [$"Role '{request.RoleName}' does not exist."]);
            }

            if (request.RoleName.Equals("user", StringComparison.CurrentCultureIgnoreCase))
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, [$"Role '{request.RoleName}' cannot be removed."]);
            }

            var userInRole = await userManager.IsInRoleAsync(user, request.RoleName);
            if (!userInRole)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, [$"User is not in role '{request.RoleName}'."]);
            }
            return await HandleRoleRemoval(user, request.RoleName);
        }

        private async Task<ServiceResponse> HandleRoleAddition(ApplicationUser user, string roleName)
        {
            var result = await userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, result.Errors.Select(e => e.Description));
            }
            await userManager.UpdateSecurityStampAsync(user);
            return ServiceResponse.Success();
        }

        private async Task<ServiceResponse> HandleRoleRemoval(ApplicationUser user, string roleName)
        {
            var result = await userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, result.Errors.Select(e => e.Description));
            }
            await userManager.UpdateSecurityStampAsync(user);
            return ServiceResponse.Success();
        }

        public ServiceResponse<List<string>> GetCurrentUserRoles()
        {
            var roles = authorizationService.GetCurrentApplicationUserRoles();
            return ServiceResponse.Success(roles);
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.Model.Common;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Roles
{
    public class RolesService(RoleManager<IdentityRole> roleManager, UserManager<User> userManager, ICurrentUserService currentUserService) : IRolesService
    {
        public async Task<Result> ChangeUserRole(string userId, ChangeUserRoleDto request)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure(ResultStatus.NotFound, $"User with ID: '{userId}' does not exist.");
            }

            if (userId == currentUserService.GetCurrentUser().Id)
            {
                return Result.Failure(ResultStatus.BadRequest, $"You can't change your own roles.");
            }

            var roleExists = await roleManager.RoleExistsAsync(request.RoleName);
            if (!roleExists)
            {
                return Result.Failure(ResultStatus.NotFound, $"Role '{request.RoleName}' does not exist.");
            }

            var userInRole = await userManager.IsInRoleAsync(user, request.RoleName);
            if (userInRole)
            {
                return Result.Failure(ResultStatus.BadRequest, $"User is already in role '{request.RoleName}'");
            }

            return await HandleRoleChange(user, request.RoleName);
        }

        private async Task<Result> HandleRoleChange(User user, string roleName)
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            var result = await userManager.AddToRoleAsync(user, roleName);

            if (!result.Succeeded)
            {
                return Result.Failure(ResultStatus.BadRequest, result.Errors.Select(e => e.Description));
            }
            return Result.Success();
        }

        public Result<List<string>> GetCurrentUserRoles()
        {
            var roles = currentUserService.GetCurrentUserRoles();
            return Result.Success(roles);
        }

        public async Task<Result<List<string>>> GetAllRoles()
        {
            var roles = await roleManager.Roles.Select(x => x.Name).Where(x => x != null).Cast<string>().ToListAsync() ?? [];
            return Result.Success(roles);
        }
    }
}

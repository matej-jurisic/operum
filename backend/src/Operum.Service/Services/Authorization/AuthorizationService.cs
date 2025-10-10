using Microsoft.AspNetCore.Identity;
using Operum.Model.Models;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Authorization
{
    public class AuthorizationService(UserManager<User> userManager, ICurrentUserService currentUserService) : IAuthorizationService
    {
        public async Task<bool> HasRole(string role)
        {
            var user = currentUserService.GetCurrentUser();
            return await userManager.IsInRoleAsync(user, role);
        }
    }
}

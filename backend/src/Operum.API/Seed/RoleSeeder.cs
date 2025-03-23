using Microsoft.AspNetCore.Identity;
using Operum.Model.Models;

namespace Operum.API.Seed
{
    public class RoleSeeder
    {
        public async static Task SeedRolesAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = ["Admin", "User", "Moderator"];

            foreach (var roleName in roleNames)
            {
                var roleExists = await roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
    }
}

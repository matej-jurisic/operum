using Microsoft.AspNetCore.Identity;
using Operum.Model.Constants;
using Operum.Model.Models;

namespace Operum.API.Seed
{
    public class DataSeeder
    {
        public async static Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = ["Admin", "User", "Moderator"];

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        public async static Task SeedUsersAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            ApplicationUser adminUser = new(DefaultUsers.AdminUserData.Email, DefaultUsers.AdminUserData.UserName);
            if (!userManager.Users.Any(x => x.NormalizedUserName == adminUser.NormalizedUserName || x.NormalizedEmail == adminUser.NormalizedEmail))
            {
                var adminPassword = configuration.GetValue<string>("ASPNETCORE_ADMINUSERPASSWORD");
                await userManager.CreateAsync(adminUser, adminPassword ?? DefaultUsers.AdminUserData.Password);
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            //ApplicationUser testUser = new(DefaultUsers.TestUserData.Email, DefaultUsers.TestUserData.UserName);
            //if (!userManager.Users.Any(x => x.NormalizedUserName == testUser.NormalizedUserName || x.NormalizedEmail == testUser.NormalizedEmail))
            //{
            //    await userManager.CreateAsync(testUser, DefaultUsers.TestUserData.Password);
            //    await userManager.AddToRoleAsync(testUser, "User");
            //}
        }
    }
}

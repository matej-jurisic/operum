using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Constants;
using Operum.Model.Enums;
using Operum.Model.Models;

namespace Operum.API.Seed
{
    public class DataSeeder
    {
        public async static Task SeedTrackerTypesAsync(OperumContext db)
        {
            TrackerType templateDraft = new()
            {
                Id = (int)PublicityEnum.Draft,
                Name = "Template Draft"
            };
            TrackerType publicTemplate = new()
            {
                Id = (int)PublicityEnum.Public,
                Name = "Public Template"
            };

            List<TrackerType> trackerTypes = [templateDraft, publicTemplate];

            foreach (TrackerType type in trackerTypes)
            {
                if (!await db.TrackerTypes.AnyAsync(x => x.Id == type.Id))
                {
                    db.TrackerTypes.Add(type);
                }
            }

            await db.SaveChangesAsync();
        }

        public async static Task SeedUsersAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IConfiguration? configuration = null)
        {
            // First, ensure roles exist
            await EnsureRoleExistsAsync(roleManager, RoleNames.User);
            await EnsureRoleExistsAsync(roleManager, RoleNames.Admin);
            await EnsureRoleExistsAsync(roleManager, RoleNames.Moderator);

            // Seed Admin User
            User adminUser = new(DefaultUsers.AdminUserData.Email, DefaultUsers.AdminUserData.UserName);
            if (!userManager.Users.Any(x => x.NormalizedUserName == adminUser.NormalizedUserName || x.NormalizedEmail == adminUser.NormalizedEmail))
            {
                var adminPassword = configuration?.GetValue<string>("AdminUserPassword");
                adminUser.EmailConfirmed = true;

                var result = await userManager.CreateAsync(adminUser, adminPassword ?? DefaultUsers.AdminUserData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating admin user: {error.Description}");
                    }
                }
            }

            User testUser = new(DefaultUsers.TestUserData.Email, DefaultUsers.TestUserData.UserName);
            if (!userManager.Users.Any(x => x.NormalizedUserName == testUser.NormalizedUserName || x.NormalizedEmail == testUser.NormalizedEmail))
            {
                testUser.EmailConfirmed = true;
                var result = await userManager.CreateAsync(testUser, DefaultUsers.TestUserData.Password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, RoleNames.User);
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating test user: {error.Description}");
                    }
                }
            }
        }

        private static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}

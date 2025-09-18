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
                Id = (int)TrackerTypeEnum.TemplateDraft,
                Name = "Template Draft"
            };
            TrackerType publicTemplate = new()
            {
                Id = (int)TrackerTypeEnum.PublicTemplate,
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

        public async static Task SeedAnalyticTypesAsync(OperumContext db)
        {
            AnalyticType draftAnalytic = new()
            {
                Id = (int)AnalyticTypeEnum.DraftAnalytic,
                Name = "Draft"
            };
            AnalyticType publicAnalytic = new()
            {
                Id = (int)AnalyticTypeEnum.PublicAnalytic,
                Name = "Public"
            };

            List<AnalyticType> analyticTypes = [draftAnalytic, publicAnalytic];

            foreach (AnalyticType type in analyticTypes)
            {
                if (!await db.AnalyticTypes.AnyAsync(x => x.Id == type.Id))
                {
                    db.AnalyticTypes.Add(type);
                }
            }

            await db.SaveChangesAsync();
        }

        public async static Task SeedUsersAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration? configuration = null)
        {
            // First, ensure roles exist
            await EnsureRoleExistsAsync(roleManager, "Admin");
            await EnsureRoleExistsAsync(roleManager, "User");
            await EnsureRoleExistsAsync(roleManager, "Moderator");

            // Seed Admin User
            ApplicationUser adminUser = new(DefaultUsers.AdminUserData.Email, DefaultUsers.AdminUserData.UserName);
            if (!userManager.Users.Any(x => x.NormalizedUserName == adminUser.NormalizedUserName || x.NormalizedEmail == adminUser.NormalizedEmail))
            {
                var adminPassword = configuration?.GetValue<string>("AdminUserPassword");
                adminUser.EmailConfirmed = true;

                var result = await userManager.CreateAsync(adminUser, adminPassword ?? DefaultUsers.AdminUserData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating admin user: {error.Description}");
                    }
                }
            }

            ApplicationUser testUser = new(DefaultUsers.TestUserData.Email, DefaultUsers.TestUserData.UserName);
            if (!userManager.Users.Any(x => x.NormalizedUserName == testUser.NormalizedUserName || x.NormalizedEmail == testUser.NormalizedEmail))
            {
                testUser.EmailConfirmed = true;
                var result = await userManager.CreateAsync(testUser, DefaultUsers.TestUserData.Password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "User");
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

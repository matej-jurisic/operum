using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Users.Requests;
using Operum.Tests.Extensions;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Users
{
    public class UsersTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> AdminClient() => _factory.AuthenticatedClient(DefaultUsers.AdminUserData);

        [Fact]
        public async Task GetCurrentUser_ReturnsTheAccountBehindTheToken()
        {
            var (client, userName) = await _factory.AuthenticatedClientForNewUser("me");

            var user = await TestApi.Data(await client.GetAsync("users/me"));

            Assert.Equal(userName, user.GetProperty("userName").GetString());
        }

        [Fact]
        public async Task GetCurrentUserRoles_ReturnsTheRolesOnTheToken()
        {
            var client = await _factory.NewUserClient("roles");

            var roles = (await TestApi.Data(await client.GetAsync("users/me/roles")))
                .EnumerateArray().Select(r => r.GetString()).ToList();

            Assert.Equal([RoleNames.User], roles);
        }

        [Fact]
        public async Task SearchUsers_MatchesPartOfAUsername()
        {
            var (client, userName) = await _factory.AuthenticatedClientForNewUser("searchable");

            var found = (await TestApi.Data(await client.GetAsync($"users?search={userName[..8]}")))
                .EnumerateArray().Select(u => u.GetProperty("userName").GetString()).ToList();

            Assert.Contains(userName, found);
        }

        [Fact]
        public async Task SearchUsers_IgnoresCase()
        {
            var (client, userName) = await _factory.AuthenticatedClientForNewUser("casing");

            var found = (await TestApi.Data(await client.GetAsync($"users?search={userName.ToUpperInvariant()}")))
                .EnumerateArray().Select(u => u.GetProperty("userName").GetString()).ToList();

            Assert.Equal([userName], found);
        }

        [Fact]
        public async Task SearchUsers_NoMatch_ReturnsAnEmptyList()
        {
            var client = await _factory.NewUserClient("nomatch");

            var found = await TestApi.Data(await client.GetAsync("users?search=definitely_nobody"));

            Assert.Empty(found.EnumerateArray());
        }

        [Fact]
        public async Task SearchUsers_WithoutASearchTerm_ReturnsBadRequest()
        {
            var client = await _factory.NewUserClient("noterm");

            var response = await client.GetAsync("users");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateUsername_ChangesWhatTheAccountIsCalled()
        {
            var client = await _factory.NewUserClient("rename");
            var newName = $"renamed{Guid.NewGuid().ToString("N")[..8]}";

            var response = await client.PutAsJsonAsync("users/me/username", new UpdateUsernameDto { UserName = newName });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = await TestApi.Data(await client.GetAsync("users/me"));
            Assert.Equal(newName, user.GetProperty("userName").GetString());
        }

        [Fact]
        public async Task UpdateUsername_NameAlreadyTaken_ReturnsConflict()
        {
            var client = await _factory.NewUserClient("clash");

            var response = await client.PutAsJsonAsync("users/me/username",
                new UpdateUsernameDto { UserName = DefaultUsers.AdminUserData.UserName });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("a_very_long_username_indeed")]
        public async Task UpdateUsername_OutsideTheLengthLimits_ReturnsBadRequest(string userName)
        {
            var client = await _factory.NewUserClient("badname");

            var response = await client.PutAsJsonAsync("users/me/username", new UpdateUsernameDto { UserName = userName });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithTheRightCurrentOne_LetsTheUserSignInAgain()
        {
            var (client, userName) = await _factory.AuthenticatedClientForNewUser("password");
            var newPassword = "AnotherStrongPassword456!";

            var response = await client.PutAsJsonAsync("users/me/password", new ChangePasswordDto
            {
                CurrentPassword = "MyStrongPassword123!",
                NewPassword = newPassword
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var freshClient = _factory.CreateClientWithCookies();
            await freshClient.Authenticate(new RegisterDto
            {
                UserName = userName,
                Email = $"{userName}@example.com",
                Password = newPassword
            });
            Assert.Equal(HttpStatusCode.OK, (await freshClient.GetAsync("users/me")).StatusCode);
        }

        [Fact]
        public async Task ChangePassword_WithTheWrongCurrentOne_ReturnsBadRequest()
        {
            var client = await _factory.NewUserClient("wrongpassword");

            var response = await client.PutAsJsonAsync("users/me/password", new ChangePasswordDto
            {
                CurrentPassword = "NotThePassword123!",
                NewPassword = "AnotherStrongPassword456!"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_ToAWeakOne_ReturnsBadRequest()
        {
            var client = await _factory.NewUserClient("weakpassword");

            var response = await client.PutAsJsonAsync("users/me/password", new ChangePasswordDto
            {
                CurrentPassword = "MyStrongPassword123!",
                NewPassword = "short"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTimeZone_KnownZone_IsAccepted()
        {
            var client = await _factory.NewUserClient("timezone");

            var response = await client.PatchAsJsonAsync("users/me/timezone",
                new UpdateTimeZoneDto { TimeZone = TimeZoneInfo.Utc.Id });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTimeZone_UnknownZone_ReturnsBadRequest()
        {
            var client = await _factory.NewUserClient("badtimezone");

            var response = await client.PatchAsJsonAsync("users/me/timezone",
                new UpdateTimeZoneDto { TimeZone = "Mars/Olympus_Mons" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.Invalid("timezone"), await TestApi.Messages(response));
        }

        [Fact]
        public async Task GetProfileStats_CountsOwnedTrackersSharedOnesAndEntries()
        {
            var (client, userName) = await _factory.AuthenticatedClientForNewUser("stats");
            var trackerId = await TestApi.CreateTracker(client, "Counted");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "one" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "two" });

            var otherOwner = await _factory.NewUserClient("statsowner");
            var sharedTrackerId = await TestApi.CreateTracker(otherOwner, "Shared in");
            await otherOwner.PostAsJsonAsync($"trackers/{sharedTrackerId}/users",
                new AddUserToTrackerDto { Username = userName });

            var stats = await TestApi.Data(await client.GetAsync("users/me/stats"));

            Assert.Equal(1, stats.GetProperty("trackersOwned").GetInt32());
            Assert.Equal(1, stats.GetProperty("sharedWithMe").GetInt32());
            // Entries of trackers shared with the user are not counted as theirs.
            Assert.Equal(2, stats.GetProperty("totalEntries").GetInt32());
        }

        [Fact]
        public async Task DeleteAccount_RemovesTheUser()
        {
            var client = await _factory.NewUserClient("doomed");

            Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("users/me")).StatusCode);
            // The token still parses, but the account behind it is gone.
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("users/me")).StatusCode);
        }

        [Fact]
        public async Task AdminOnlyEndpoints_AsANormalUser_ReturnForbidden()
        {
            var client = await _factory.NewUserClient("notadmin");

            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("users/all")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("users/roles")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("admin/stats")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("admin/trackers")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("trackers/admin-templates")).StatusCode);
        }

        [Fact]
        public async Task AdminOnlyEndpoints_Unauthenticated_ReturnUnauthorized()
        {
            var client = _factory.CreateClient();
            await _factory.SeedDatabaseAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("admin/stats")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("users/all")).StatusCode);
        }

        [Fact]
        public async Task GetAllUsers_AsAnAdmin_ListsUsersWithTheirRoles()
        {
            var admin = await AdminClient();
            var (_, userName) = await _factory.AuthenticatedClientForNewUser("listed");

            var users = (await TestApi.Data(await admin.GetAsync("users/all"))).EnumerateArray().ToList();

            var listed = users.Single(u => u.GetProperty("userName").GetString() == userName);
            Assert.Equal([RoleNames.User], listed.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
        }

        [Fact]
        public async Task GetAllRoles_AsAnAdmin_ReturnsTheSeededRoles()
        {
            var admin = await AdminClient();

            var roles = (await TestApi.Data(await admin.GetAsync("users/roles")))
                .EnumerateArray().Select(r => r.GetString()).ToList();

            Assert.Contains(RoleNames.Admin, roles);
            Assert.Contains(RoleNames.User, roles);
            Assert.Contains(RoleNames.Moderator, roles);
        }

        [Fact]
        public async Task GetAdminStats_ReturnsCountsAcrossTheInstallation()
        {
            var admin = await AdminClient();

            var stats = await TestApi.Data(await admin.GetAsync("admin/stats"));

            Assert.True(stats.GetProperty("totalUsers").GetInt32() >= 2);
            Assert.True(stats.GetProperty("totalTrackers").GetInt32() >= 0);
            Assert.True(stats.GetProperty("entriesLast30Days").GetInt32() >= 0);
        }

        [Fact]
        public async Task ChangeUserRole_AsAnAdmin_ReplacesTheUsersRole()
        {
            var admin = await AdminClient();
            var (promoted, _) = await _factory.AuthenticatedClientForNewUser("promoted");
            var userId = (await TestApi.Data(await promoted.GetAsync("users/me"))).GetProperty("id").GetString();

            var response = await admin.PostAsJsonAsync($"users/{userId}/role", new ChangeUserRoleDto { RoleName = RoleNames.Moderator });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var users = (await TestApi.Data(await admin.GetAsync("users/all"))).EnumerateArray().ToList();
            var listed = users.Single(u => u.GetProperty("id").GetString() == userId);
            Assert.Equal([RoleNames.Moderator], listed.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
        }

        [Fact]
        public async Task ChangeUserRole_ToTheRoleTheUserAlreadyHas_ReturnsBadRequest()
        {
            var admin = await AdminClient();
            var (user, _) = await _factory.AuthenticatedClientForNewUser("alreadyuser");
            var userId = (await TestApi.Data(await user.GetAsync("users/me"))).GetProperty("id").GetString();

            var response = await admin.PostAsJsonAsync($"users/{userId}/role", new ChangeUserRoleDto { RoleName = RoleNames.User });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.AlreadyInRole, await TestApi.Messages(response));
        }

        [Fact]
        public async Task ChangeUserRole_ForYourOwnAccount_ReturnsBadRequest()
        {
            var admin = await AdminClient();
            var adminId = (await TestApi.Data(await admin.GetAsync("users/me"))).GetProperty("id").GetString();

            // An admin cannot demote themselves and lock the installation out.
            var response = await admin.PostAsJsonAsync($"users/{adminId}/role", new ChangeUserRoleDto { RoleName = RoleNames.User });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserRole_ToAnUnknownRole_ReturnsNotFound()
        {
            var admin = await AdminClient();
            var (user, _) = await _factory.AuthenticatedClientForNewUser("norole");
            var userId = (await TestApi.Data(await user.GetAsync("users/me"))).GetProperty("id").GetString();

            var response = await admin.PostAsJsonAsync($"users/{userId}/role", new ChangeUserRoleDto { RoleName = "Overlord" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmUserEmail_ForAnAlreadyConfirmedUser_SaysSo()
        {
            var admin = await AdminClient();
            var (user, _) = await _factory.AuthenticatedClientForNewUser("confirmed");
            var userId = (await TestApi.Data(await user.GetAsync("users/me"))).GetProperty("id").GetString();

            var response = await admin.PostAsJsonAsync($"users/{userId}/confirm-email", new { });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(Messages.EmailAlreadyConfirmed, await TestApi.Messages(response));
        }

        [Fact]
        public async Task ConfirmUserEmail_ForYourOwnAccount_ReturnsBadRequest()
        {
            var admin = await AdminClient();
            var adminId = (await TestApi.Data(await admin.GetAsync("users/me"))).GetProperty("id").GetString();

            var response = await admin.PostAsJsonAsync($"users/{adminId}/confirm-email", new { });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmUserEmail_UnknownUser_ReturnsNotFound()
        {
            var admin = await AdminClient();

            var response = await admin.PostAsJsonAsync($"users/{Guid.NewGuid()}/confirm-email", new { });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}

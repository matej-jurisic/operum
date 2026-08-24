using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Views.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Trackers
{
    /// <summary>
    /// Sharing a tracker and what each level of collaborator may then do with it. Read access
    /// comes with the invitation; editing data and editing the schema are granted separately.
    /// </summary>
    public class TrackerSharingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private sealed record Shared(HttpClient Owner, HttpClient Collaborator, string CollaboratorName, string TrackerId, string EntryId);

        /// <summary>A tracker with one field and one entry, shared with a second user.</summary>
        private async Task<Shared> ShareTracker(string name, bool canEditData = false, bool canEditSchema = false)
        {
            var owner = await _factory.NewUserClient("owner");
            var (collaborator, collaboratorName) = await _factory.AuthenticatedClientForNewUser("collab");

            var trackerId = await TestApi.CreateTracker(owner, name);
            await TestApi.CreateField(owner, trackerId, "Note", DataTypes.String);
            var entryId = await TestApi.CreateEntry(owner, trackerId, new() { ["Note"] = "owner's entry" });

            var response = await owner.PostAsJsonAsync($"trackers/{trackerId}/users",
                new AddUserToTrackerDto { Username = collaboratorName, CanEditData = canEditData, CanEditSchema = canEditSchema });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return new Shared(owner, collaborator, collaboratorName, trackerId, entryId);
        }

        [Fact]
        public async Task Collaborator_CanReadTheTrackerItsFieldsAndItsEntries()
        {
            var shared = await ShareTracker("Shared read");

            Assert.Equal(HttpStatusCode.OK, (await shared.Collaborator.GetAsync($"trackers/{shared.TrackerId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await shared.Collaborator.GetAsync($"trackers/{shared.TrackerId}/fields")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await shared.Collaborator.GetAsync($"trackers/{shared.TrackerId}/views")).StatusCode);
            Assert.Single(await TestApi.ListEntries(shared.Collaborator, shared.TrackerId));
        }

        [Fact]
        public async Task Collaborator_WithoutDataRights_CannotTouchEntries()
        {
            var shared = await ShareTracker("Shared read only");

            Assert.Equal(HttpStatusCode.Forbidden,
                (await TestApi.PostEntry(shared.Collaborator, shared.TrackerId, new() { ["Note"] = "mine" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await TestApi.PutEntry(shared.Collaborator, shared.TrackerId, shared.EntryId, new() { ["Note"] = "changed" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await shared.Collaborator.DeleteAsync($"trackers/{shared.TrackerId}/entries/{shared.EntryId}")).StatusCode);

            Assert.Equal("owner's entry", await TestApi.StringValueOf(shared.Owner, shared.TrackerId, shared.EntryId, "Note"));
        }

        [Fact]
        public async Task Collaborator_WithDataRights_MayWriteEntries()
        {
            var shared = await ShareTracker("Shared data", canEditData: true);

            var created = await TestApi.PostEntry(shared.Collaborator, shared.TrackerId, new() { ["Note"] = "theirs" });
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            Assert.Equal(HttpStatusCode.OK,
                (await TestApi.PutEntry(shared.Collaborator, shared.TrackerId, shared.EntryId, new() { ["Note"] = "changed" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK,
                (await shared.Collaborator.DeleteAsync($"trackers/{shared.TrackerId}/entries/{shared.EntryId}")).StatusCode);

            Assert.Single(await TestApi.ListEntries(shared.Owner, shared.TrackerId));
        }

        [Fact]
        public async Task Collaborator_WithDataRightsOnly_CannotChangeTheSchema()
        {
            var shared = await ShareTracker("Shared data not schema", canEditData: true);

            Assert.Equal(HttpStatusCode.NotFound,
                (await TestApi.PostField(shared.Collaborator, shared.TrackerId,
                    new CreateFieldDto { Name = "Sneak", Type = DataTypes.String })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await TestApi.PostView(shared.Collaborator, shared.TrackerId, new CreateViewDto { Name = "Sneak" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await shared.Collaborator.PostAsJsonAsync($"trackers/{shared.TrackerId}/constants",
                    new { Name = "Sneak", Type = DataTypes.Number, Value = "1" })).StatusCode);
        }

        [Fact]
        public async Task Collaborator_WithSchemaRights_MayAddFieldsViewsAndConstants()
        {
            var shared = await ShareTracker("Shared schema", canEditSchema: true);

            Assert.Equal(HttpStatusCode.OK,
                (await TestApi.PostField(shared.Collaborator, shared.TrackerId,
                    new CreateFieldDto { Name = "Added", Type = DataTypes.String })).StatusCode);
            Assert.Equal(HttpStatusCode.OK,
                (await TestApi.PostView(shared.Collaborator, shared.TrackerId, new CreateViewDto { Name = "Added" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK,
                (await shared.Collaborator.PostAsJsonAsync($"trackers/{shared.TrackerId}/constants",
                    new { Name = "Rate", Type = DataTypes.Number, Value = "2" })).StatusCode);
        }

        [Fact]
        public async Task Collaborator_WithSchemaRightsOnly_StillCannotWriteEntries()
        {
            var shared = await ShareTracker("Shared schema not data", canEditSchema: true);

            // The two permissions are independent: schema rights do not imply data rights.
            Assert.Equal(HttpStatusCode.Forbidden,
                (await TestApi.PostEntry(shared.Collaborator, shared.TrackerId, new() { ["Note"] = "mine" })).StatusCode);
        }

        [Fact]
        public async Task Collaborator_CannotDeleteOrRenameTheTracker()
        {
            var shared = await ShareTracker("Shared not owned", canEditData: true, canEditSchema: true);

            // Even a fully trusted collaborator is not the owner.
            Assert.Equal(HttpStatusCode.NotFound,
                (await shared.Collaborator.PutAsJsonAsync($"trackers/{shared.TrackerId}",
                    new UpdateTrackerDto { Name = "Renamed" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await shared.Collaborator.DeleteAsync($"trackers/{shared.TrackerId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await shared.Owner.GetAsync($"trackers/{shared.TrackerId}")).StatusCode);
        }

        [Fact]
        public async Task Collaborator_CannotShareTheTrackerFurther()
        {
            var shared = await ShareTracker("No re-sharing", canEditData: true, canEditSchema: true);
            var (_, outsiderName) = await _factory.AuthenticatedClientForNewUser("outsider");

            var response = await shared.Collaborator.PostAsJsonAsync($"trackers/{shared.TrackerId}/users",
                new AddUserToTrackerDto { Username = outsiderName, CanEditData = true });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTracker_ReportsWhatTheCallerMayDo()
        {
            var shared = await ShareTracker("Permission flags", canEditData: true);

            var asOwner = await TestApi.Data(await shared.Owner.GetAsync($"trackers/{shared.TrackerId}"));
            Assert.True(asOwner.GetProperty("currentUserCanEditData").GetBoolean());
            Assert.True(asOwner.GetProperty("currentUserCanEditSchema").GetBoolean());

            var asCollaborator = await TestApi.Data(await shared.Collaborator.GetAsync($"trackers/{shared.TrackerId}"));
            Assert.True(asCollaborator.GetProperty("currentUserCanEditData").GetBoolean());
            Assert.False(asCollaborator.GetProperty("currentUserCanEditSchema").GetBoolean());
        }

        [Fact]
        public async Task GetCollaborators_ListsThemWithTheirPermissions()
        {
            var shared = await ShareTracker("Collaborator list", canEditData: true);

            var data = await TestApi.Data(await shared.Owner.GetAsync($"trackers/{shared.TrackerId}/users"));

            var collaborator = data.EnumerateArray().Single();
            Assert.Equal(shared.CollaboratorName, collaborator.GetProperty("userName").GetString());
            Assert.True(collaborator.GetProperty("canEditData").GetBoolean());
            Assert.False(collaborator.GetProperty("canEditSchema").GetBoolean());
        }

        [Fact]
        public async Task UpdateCollaboratorPermissions_GrantsAndRevokesRightsStraightAway()
        {
            var shared = await ShareTracker("Changing rights");

            var granted = await shared.Owner.PutAsJsonAsync($"trackers/{shared.TrackerId}/users",
                new UpdateCollaboratorPermissionsDto { Username = shared.CollaboratorName, CanEditData = true });
            Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
            Assert.Equal(HttpStatusCode.OK,
                (await TestApi.PostEntry(shared.Collaborator, shared.TrackerId, new() { ["Note"] = "allowed now" })).StatusCode);

            var revoked = await shared.Owner.PutAsJsonAsync($"trackers/{shared.TrackerId}/users",
                new UpdateCollaboratorPermissionsDto { Username = shared.CollaboratorName, CanEditData = false });
            Assert.Equal(HttpStatusCode.OK, revoked.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await TestApi.PostEntry(shared.Collaborator, shared.TrackerId, new() { ["Note"] = "not any more" })).StatusCode);
        }

        [Fact]
        public async Task UpdateCollaboratorPermissions_ForSomeoneNotOnTheTracker_ReturnsNotFound()
        {
            var shared = await ShareTracker("Rights for a stranger");
            var (_, outsiderName) = await _factory.AuthenticatedClientForNewUser("outsider");

            var response = await shared.Owner.PutAsJsonAsync($"trackers/{shared.TrackerId}/users",
                new UpdateCollaboratorPermissionsDto { Username = outsiderName, CanEditData = true });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains(Messages.NotInTracker, await TestApi.Messages(response));
        }

        [Fact]
        public async Task RemoveUserFromTracker_TakesTheAccessAway()
        {
            var shared = await ShareTracker("Removing access", canEditData: true);

            var request = new HttpRequestMessage(HttpMethod.Delete, $"trackers/{shared.TrackerId}/users")
            {
                Content = JsonContent.Create(new RemoveUserFromTrackerDto { Username = shared.CollaboratorName })
            };
            var response = await shared.Owner.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await shared.Collaborator.GetAsync($"trackers/{shared.TrackerId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await shared.Collaborator.GetAsync(TestApi.EntriesUrl(shared.TrackerId))).StatusCode);
        }

        [Fact]
        public async Task RemoveUserFromTracker_SomeoneWhoWasNeverAdded_ReturnsBadRequest()
        {
            var shared = await ShareTracker("Removing a stranger");
            var (_, outsiderName) = await _factory.AuthenticatedClientForNewUser("outsider");

            var request = new HttpRequestMessage(HttpMethod.Delete, $"trackers/{shared.TrackerId}/users")
            {
                Content = JsonContent.Create(new RemoveUserFromTrackerDto { Username = outsiderName })
            };
            var response = await shared.Owner.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.NotInTracker, await TestApi.Messages(response));
        }

        [Fact]
        public async Task AddUserToTracker_TheSameUserTwice_ReturnsBadRequest()
        {
            var shared = await ShareTracker("Added twice");

            var response = await shared.Owner.PostAsJsonAsync($"trackers/{shared.TrackerId}/users",
                new AddUserToTrackerDto { Username = shared.CollaboratorName, CanEditData = true });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.AlreadyInTracker, await TestApi.Messages(response));
        }

        [Fact]
        public async Task AddUserToTracker_UnknownUsername_ReturnsNotFound()
        {
            var owner = await _factory.NewUserClient("owner");
            var trackerId = await TestApi.CreateTracker(owner, "Unknown invitee");

            var response = await owner.PostAsJsonAsync($"trackers/{trackerId}/users",
                new AddUserToTrackerDto { Username = "nobody_at_all" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains(Messages.ItemNotFound("user"), await TestApi.Messages(response));
        }

        [Fact]
        public async Task AddUserToTracker_TheOwnerThemselves_ReturnsBadRequest()
        {
            var (owner, ownerName) = await _factory.AuthenticatedClientForNewUser("owner");
            var trackerId = await TestApi.CreateTracker(owner, "Self invite");

            var response = await owner.PostAsJsonAsync($"trackers/{trackerId}/users",
                new AddUserToTrackerDto { Username = ownerName, CanEditData = true });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddUserToTracker_ByAStranger_ReturnsNotFound()
        {
            var owner = await _factory.NewUserClient("owner");
            var trackerId = await TestApi.CreateTracker(owner, "Not yours to share");
            var (stranger, strangerName) = await _factory.AuthenticatedClientForNewUser("stranger");

            var response = await stranger.PostAsJsonAsync($"trackers/{trackerId}/users",
                new AddUserToTrackerDto { Username = strangerName, CanEditData = true });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"trackers/{trackerId}")).StatusCode);
        }

        [Fact]
        public async Task GetCollaborators_ByAStranger_ReturnsForbidden()
        {
            var owner = await _factory.NewUserClient("owner");
            var trackerId = await TestApi.CreateTracker(owner, "Collaborator list guard");
            var stranger = await _factory.NewUserClient("stranger");

            var response = await stranger.GetAsync($"trackers/{trackerId}/users");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TrackerList_SeparatesOwnedFromCollaborating()
        {
            var shared = await ShareTracker("Listed as shared");
            var ownTrackerId = await TestApi.CreateTracker(shared.Collaborator, "The collaborator's own");

            var owned = await TrackerNames(shared.Collaborator, TrackerFilters.Owned);
            var collaborating = await TrackerNames(shared.Collaborator, TrackerFilters.Collaborating);
            var accessible = await TrackerNames(shared.Collaborator, TrackerFilters.Accessible);

            Assert.Equal(["The collaborator's own"], owned);
            Assert.Equal(["Listed as shared"], collaborating);
            Assert.Equal(["Listed as shared", "The collaborator's own"], accessible.Order());
            Assert.NotEmpty(ownTrackerId);
        }

        [Fact]
        public async Task TrackerList_UnknownFilter_ReturnsBadRequest()
        {
            var client = await _factory.NewUserClient("lister");

            var response = await client.GetAsync("trackers?filter=Everything");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private static async Task<List<string>> TrackerNames(HttpClient client, string filter)
        {
            var data = await TestApi.Data(await client.GetAsync($"trackers?filter={filter}"));
            return [.. data.EnumerateArray().Select(t => t.GetProperty("name").GetString()!)];
        }
    }
}

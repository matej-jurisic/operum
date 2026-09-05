using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Operum.Model;
using Operum.Model.DTOs.Notifications;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Notifications
{
    // The inbox is the persisted, per-recipient counterpart to web push: NotificationEvaluatorService
    // writes one InboxNotification per tracker member every time a notification fires. These tests
    // exercise the read/manage surface directly (rows seeded through the context, the way the toggle
    // test seeds IsTriggered) plus the fan-out helper the evaluator calls.
    public class InboxApiTests(NotificationsEnabledFactory factory) : IClassFixture<NotificationsEnabledFactory>
    {
        private readonly NotificationsEnabledFactory _factory = factory;

        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

        private static async Task<JsonElement> Data(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            if (!json.TryGetProperty("data", out var data))
                throw new Exception($"Response had no 'data' property. Status: {response.StatusCode}. Body: {body}");
            return data;
        }

        private async Task<string> UserId(string userName)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            return await db.Users.Where(u => u.UserName == userName).Select(u => u.Id).FirstAsync();
        }

        private async Task<string> SeedTracker(string ownerId, string name = "Sales")
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var tracker = new Tracker { Name = name, OwnerId = ownerId };
            db.Trackers.Add(tracker);
            await db.SaveChangesAsync();
            return tracker.Id;
        }

        private async Task<string> SeedItem(string userId, string trackerId, string title, DateTime? createdAt = null, DateTime? readAt = null)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var item = new InboxNotification
            {
                UserId = userId,
                TrackerId = trackerId,
                Title = title,
                Body = "something happened",
                Url = $"/trackers/{trackerId}",
                CreatedAt = createdAt ?? DateTime.UtcNow,
                ReadAt = readAt,
            };
            db.InboxNotifications.Add(item);
            await db.SaveChangesAsync();
            return item.Id;
        }

        [Fact]
        public async Task GetInbox_ReturnsOwnItemsNewestFirst_WithUnreadCount()
        {
            var (client, userName) = await _factory.AuthenticatedClientForNewUser("inboxlist");
            var userId = await UserId(userName);
            var trackerId = await SeedTracker(userId);

            await SeedItem(userId, trackerId, "older", createdAt: DateTime.UtcNow.AddHours(-2));
            await SeedItem(userId, trackerId, "newer", createdAt: DateTime.UtcNow.AddHours(-1));
            await SeedItem(userId, trackerId, "read one", createdAt: DateTime.UtcNow.AddHours(-3), readAt: DateTime.UtcNow);

            var page = (await Data(await client.GetAsync("inbox"))).Deserialize<InboxPageDto>(Json)!;

            Assert.Equal(3, page.Items.Count);
            Assert.Equal("newer", page.Items[0].Title);
            Assert.Equal("older", page.Items[1].Title);
            Assert.Equal("Sales", page.Items[0].TrackerName);
            Assert.Equal(2, page.UnreadCount);
            Assert.False(page.HasMore);
        }

        [Fact]
        public async Task GetInbox_Paginates_WithHasMore()
        {
            var (client, userName) = await _factory.AuthenticatedClientForNewUser("inboxpage");
            var userId = await UserId(userName);
            var trackerId = await SeedTracker(userId);

            for (var i = 0; i < 5; i++)
                await SeedItem(userId, trackerId, $"item {i}", createdAt: DateTime.UtcNow.AddMinutes(-i));

            var first = (await Data(await client.GetAsync("inbox?skip=0&take=2"))).Deserialize<InboxPageDto>(Json)!;
            Assert.Equal(2, first.Items.Count);
            Assert.True(first.HasMore);

            var last = (await Data(await client.GetAsync("inbox?skip=4&take=2"))).Deserialize<InboxPageDto>(Json)!;
            Assert.Single(last.Items);
            Assert.False(last.HasMore);
        }

        [Fact]
        public async Task UnreadCount_Endpoint_ReflectsState()
        {
            var (client, userName) = await _factory.AuthenticatedClientForNewUser("inboxcount");
            var userId = await UserId(userName);
            var trackerId = await SeedTracker(userId);
            var itemId = await SeedItem(userId, trackerId, "one");
            await SeedItem(userId, trackerId, "two");

            Assert.Equal(2, (await Data(await client.GetAsync("inbox/unread-count"))).GetInt32());

            await client.PostAsync($"inbox/{itemId}/read", null);
            Assert.Equal(1, (await Data(await client.GetAsync("inbox/unread-count"))).GetInt32());

            await client.PostAsync("inbox/read-all", null);
            Assert.Equal(0, (await Data(await client.GetAsync("inbox/unread-count"))).GetInt32());
        }

        [Fact]
        public async Task Delete_RemovesItem()
        {
            var (client, userName) = await _factory.AuthenticatedClientForNewUser("inboxdel");
            var userId = await UserId(userName);
            var trackerId = await SeedTracker(userId);
            var itemId = await SeedItem(userId, trackerId, "bye");

            var deleted = await client.DeleteAsync($"inbox/{itemId}");
            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);

            var page = (await Data(await client.GetAsync("inbox"))).Deserialize<InboxPageDto>(Json)!;
            Assert.Empty(page.Items);
        }

        [Fact]
        public async Task OtherUsersItem_IsNotVisibleOrMutable()
        {
            var (ownerClient, ownerName) = await _factory.AuthenticatedClientForNewUser("inboxownerA");
            var ownerId = await UserId(ownerName);
            var trackerId = await SeedTracker(ownerId);
            var itemId = await SeedItem(ownerId, trackerId, "private");

            var otherClient = await _factory.NewUserClient("inboxownerB");

            var page = (await Data(await otherClient.GetAsync("inbox"))).Deserialize<InboxPageDto>(Json)!;
            Assert.Empty(page.Items);

            Assert.Equal(HttpStatusCode.NotFound, (await otherClient.PostAsync($"inbox/{itemId}/read", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await otherClient.DeleteAsync($"inbox/{itemId}")).StatusCode);

            // The owner's item is untouched.
            var ownerPage = (await Data(await ownerClient.GetAsync("inbox"))).Deserialize<InboxPageDto>(Json)!;
            Assert.Single(ownerPage.Items);
            Assert.Null(ownerPage.Items[0].ReadAt);
        }

        [Fact]
        public async Task CreateForTrackerMembers_FansOutToOwnerAndCollaborators()
        {
            var (ownerClient, ownerName) = await _factory.AuthenticatedClientForNewUser("inboxfanowner");
            var ownerId = await UserId(ownerName);
            var (collabClient, collabName) = await _factory.AuthenticatedClientForNewUser("inboxfancollab");
            var collabId = await UserId(collabName);

            var trackerId = await SeedTracker(ownerId, "Shared");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
                db.UserTrackers.Add(new UserTracker { ApplicationUserId = collabId, TrackerId = trackerId, CanEditData = true });
                await db.SaveChangesAsync();
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
                var inbox = scope.ServiceProvider.GetRequiredService<IInboxService>();
                await inbox.CreateForTrackerMembersAsync(trackerId, null, "Shared - Alert", "condition met", $"/trackers/{trackerId}");
                await db.SaveChangesAsync();
            }

            var ownerPage = (await Data(await ownerClient.GetAsync("inbox"))).Deserialize<InboxPageDto>(Json)!;
            var collabPage = (await Data(await collabClient.GetAsync("inbox"))).Deserialize<InboxPageDto>(Json)!;

            Assert.Single(ownerPage.Items);
            Assert.Single(collabPage.Items);
            Assert.Equal("Shared - Alert", ownerPage.Items[0].Title);
        }

        [Fact]
        public async Task Inbox_Is404_WhenFeatureFlagOff()
        {
            await using var plainFactory = new CustomWebApplicationFactory();
            var client = await plainFactory.NewUserClient("inboxflagoff");

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("inbox")).StatusCode);
        }
    }
}

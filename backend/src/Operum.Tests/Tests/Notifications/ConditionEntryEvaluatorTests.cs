using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Operum.Model;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.Models;
using Operum.Service.Domain.Notifications;
using Operum.Tests.Util;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Notifications
{
    // ConditionEntryEvaluator is the Entry-mode counterpart to ConditionAnalyticEvaluator
    // (see ConditionAnalyticEvaluatorTests) and had no coverage of its own: it reprojects a
    // NotificationCondition's own filters into the view-filter machinery and, when a view is
    // scoped, ANDs the two filter sets together. Both are exercised directly against
    // ConditionEntryEvaluator, the same shape NotificationEvaluatorService passes it in
    // production.
    public class ConditionEntryEvaluatorTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private async Task<TrackerNotification> LoadForEvaluation(string notificationId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();

            return await db.TrackerNotifications
                .Include(n => n.Condition).ThenInclude(c => c.Filters).ThenInclude(f => f.Field)
                .FirstAsync(n => n.Id == notificationId);
        }

        private async Task<string> AddEntryModeNotification(string trackerId, string? viewId, string fieldId, string op, string? value)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();

            var notification = new TrackerNotification
            {
                Name = "Status check",
                TrackerId = trackerId,
                ViewId = viewId,
                Condition = new NotificationCondition
                {
                    ValueMode = NotificationValueMode.Entry,
                    Filters = [new NotificationConditionFilter { FieldId = fieldId, Operator = op, Value = value }]
                }
            };
            db.TrackerNotifications.Add(notification);
            await db.SaveChangesAsync();
            return notification.Id;
        }

        [Fact]
        public async Task GetMatchingEntryIdsAsync_ReturnsOnlyEntriesThatMatchTheFilter()
        {
            var client = await _factory.NewUserClient("entrynotificationmatch");

            var trackerId = await TestApi.CreateTracker(client, "Tasks");
            var statusFieldId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Status", Type = DataTypes.String }));

            var openId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Status"] = "Open" } }));
            await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Status"] = "Closed" } });

            var notificationId = await AddEntryModeNotification(trackerId, viewId: null, statusFieldId, OperatorTypes.EqualsOperator, "Open");
            var loaded = await LoadForEvaluation(notificationId);

            using var evalScope = _factory.Services.CreateScope();
            var evalDb = evalScope.ServiceProvider.GetRequiredService<OperumContext>();
            var matches = await ConditionEntryEvaluator.GetMatchingEntryIdsAsync(evalDb, loaded, TimeZoneInfo.Utc);

            Assert.Equal([openId], matches);
        }

        [Fact]
        public async Task GetMatchingEntryIdsAsync_ScopedToView_AndsViewFiltersWithConditionFilters()
        {
            var client = await _factory.NewUserClient("entrynotificationview");

            var trackerId = await TestApi.CreateTracker(client, "Tasks");
            var statusFieldId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Status", Type = DataTypes.String }));
            var priorityFieldId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Priority", Type = DataTypes.String }));

            // Only this entry satisfies both the view's Priority=High scope and the condition's Status=Open.
            var bothMatchId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Status"] = "Open", ["Priority"] = "High" } }));
            await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Status"] = "Open", ["Priority"] = "Low" } });
            await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Status"] = "Closed", ["Priority"] = "High" } });

            var viewId = await TestApi.CreateFilterView(client, trackerId, "High priority", priorityFieldId, OperatorTypes.EqualsOperator, "High");

            var notificationId = await AddEntryModeNotification(trackerId, viewId, statusFieldId, OperatorTypes.EqualsOperator, "Open");
            var loaded = await LoadForEvaluation(notificationId);

            using var evalScope = _factory.Services.CreateScope();
            var evalDb = evalScope.ServiceProvider.GetRequiredService<OperumContext>();
            var matches = await ConditionEntryEvaluator.GetMatchingEntryIdsAsync(evalDb, loaded, TimeZoneInfo.Utc);

            Assert.Equal([bothMatchId], matches);
        }

        [Fact]
        public async Task GetMatchingEntryIdsAsync_NoFilters_MatchesEveryEntryInScope()
        {
            var client = await _factory.NewUserClient("entrynotificationnofilter");

            var trackerId = await TestApi.CreateTracker(client, "Tasks");
            await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Status", Type = DataTypes.String });

            var e1 = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Status"] = "Open" } }));
            var e2 = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Status"] = "Closed" } }));

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var notification = new TrackerNotification
            {
                Name = "Any entry",
                TrackerId = trackerId,
                Condition = new NotificationCondition { ValueMode = NotificationValueMode.Entry, Filters = [] }
            };
            db.TrackerNotifications.Add(notification);
            await db.SaveChangesAsync();

            var loaded = await LoadForEvaluation(notification.Id);

            using var evalScope = _factory.Services.CreateScope();
            var evalDb = evalScope.ServiceProvider.GetRequiredService<OperumContext>();
            var matches = await ConditionEntryEvaluator.GetMatchingEntryIdsAsync(evalDb, loaded, TimeZoneInfo.Utc);

            Assert.Equal(new HashSet<string> { e1, e2 }, matches.ToHashSet());
        }
    }
}

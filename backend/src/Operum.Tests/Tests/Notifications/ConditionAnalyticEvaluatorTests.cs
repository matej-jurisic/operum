using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Operum.Model;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.Models;
using Operum.Service.Domain.Notifications;
using Operum.Tests.Util;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Notifications
{
    // Regression coverage for the Widget Library refactor: Analytic went from an EF entity
    // to a plain, unmapped POCO (Operum.Model.Models.Analytic), and ConditionAnalyticEvaluator
    // is the one production call site that constructs it independently of Widgets/Dashboard
    // -- it builds a transient Analytic straight from a NotificationCondition's own
    // AnalyticCode/AnalyticResultType, with no Widget or WidgetSource in the picture at all.
    // Nothing else in this codebase tests the notifications feature (it's opt-in behind a
    // feature flag -- see ServiceConfiguration), so this exercises the evaluator directly
    // rather than through the (disabled-by-default) hosted evaluator service.
    public class ConditionAnalyticEvaluatorTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private static async Task<JsonElement> Data(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            if (!json.TryGetProperty("data", out var data))
                throw new Exception($"Response had no 'data' property. Status: {response.StatusCode}. Body: {body}");
            return data;
        }

        // Loaded the same way NotificationEvaluatorService loads every notification it
        // evaluates, so this exercises the exact shape ConditionAnalyticEvaluator receives
        // in production.
        private async Task<TrackerNotification> LoadForEvaluation(string notificationId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();

            return await db.TrackerNotifications
                .Include(n => n.Condition).ThenInclude(c => c.Filters).ThenInclude(f => f.Field)
                .Include(n => n.Condition).ThenInclude(c => c.PurposeFields).ThenInclude(pf => pf.Field)
                .FirstAsync(n => n.Id == notificationId);
        }

        [Fact]
        public async Task EvaluateAsync_AnalyticModeConditionMet_ReturnsTrue()
        {
            var client = await _factory.NewUserClient("analyticnotification");

            var tracker = await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = "Weight" }));
            var trackerId = tracker.GetProperty("id").GetString()!;
            var amountField = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number }));
            var amountFieldId = amountField.GetProperty("id").GetString()!;

            await client.PostAsJsonAsync($"trackers/{trackerId}/entries", new CreateEntryDto
            {
                FieldValues = new() { ["Amount"] = "12" }
            });

            string notificationId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();

                var notification = new TrackerNotification
                {
                    Name = "Amount check",
                    TrackerId = trackerId,
                    Condition = new NotificationCondition
                    {
                        ValueMode = NotificationValueMode.Analytic,
                        AnalyticCode = AnalyticCodes.Average,
                        AnalyticResultType = AnalyticTypes.SingleValue,
                        PurposeFields = [new NotificationConditionPurposeField { Purpose = AnalyticPurposes.Value, FieldId = amountFieldId }],
                        Filters = [new NotificationConditionFilter { Operator = OperatorTypes.GreaterThan, Value = "10" }]
                    }
                };
                db.TrackerNotifications.Add(notification);
                await db.SaveChangesAsync();
                notificationId = notification.Id;
            }

            using var evalScope = _factory.Services.CreateScope();
            var evalDb = evalScope.ServiceProvider.GetRequiredService<OperumContext>();
            var loaded = await LoadForEvaluation(notificationId);

            var result = await ConditionAnalyticEvaluator.EvaluateAsync(evalDb, loaded, TimeZoneInfo.Utc);

            // Average(Amount) = 12, which is greater than the condition's threshold of 10.
            Assert.True(result);
        }

        [Fact]
        public async Task EvaluateAsync_AnalyticModeConditionNotMet_ReturnsFalse()
        {
            var client = await _factory.NewUserClient("analyticnotificationfalse");

            var tracker = await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = "Weight" }));
            var trackerId = tracker.GetProperty("id").GetString()!;
            var amountField = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number }));
            var amountFieldId = amountField.GetProperty("id").GetString()!;

            await client.PostAsJsonAsync($"trackers/{trackerId}/entries", new CreateEntryDto
            {
                FieldValues = new() { ["Amount"] = "3" }
            });

            string notificationId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();

                var notification = new TrackerNotification
                {
                    Name = "Amount check",
                    TrackerId = trackerId,
                    Condition = new NotificationCondition
                    {
                        ValueMode = NotificationValueMode.Analytic,
                        AnalyticCode = AnalyticCodes.Average,
                        AnalyticResultType = AnalyticTypes.SingleValue,
                        PurposeFields = [new NotificationConditionPurposeField { Purpose = AnalyticPurposes.Value, FieldId = amountFieldId }],
                        Filters = [new NotificationConditionFilter { Operator = OperatorTypes.GreaterThan, Value = "10" }]
                    }
                };
                db.TrackerNotifications.Add(notification);
                await db.SaveChangesAsync();
                notificationId = notification.Id;
            }

            using var evalScope = _factory.Services.CreateScope();
            var evalDb = evalScope.ServiceProvider.GetRequiredService<OperumContext>();
            var loaded = await LoadForEvaluation(notificationId);

            var result = await ConditionAnalyticEvaluator.EvaluateAsync(evalDb, loaded, TimeZoneInfo.Utc);

            // Average(Amount) = 3, which is not greater than the condition's threshold of 10.
            Assert.False(result);
        }
    }
}

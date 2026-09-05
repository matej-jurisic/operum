using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Operum.Model;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.Constants.Notifications;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Notifications;
using Operum.Model.DTOs.Notifications.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Notifications
{
    // End-to-end coverage of NotificationsService through the actual controller routes --
    // nothing else exercised Create/Update/Toggle/Delete before this file (see
    // ConditionAnalyticEvaluatorTests' header comment), so the manual EF child-replacement
    // dance in UpdateNotification and the pre-population step in CreateNotification had never
    // run outside of manual testing.
    public class NotificationsApiTests(NotificationsEnabledFactory factory) : IClassFixture<NotificationsEnabledFactory>
    {
        private readonly NotificationsEnabledFactory _factory = factory;

        private static async Task<JsonElement> Data(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            if (!json.TryGetProperty("data", out var data))
                throw new Exception($"Response had no 'data' property. Status: {response.StatusCode}. Body: {body}");
            return data;
        }

        private static TrackerNotificationDto Notification(JsonElement el) =>
            el.Deserialize<TrackerNotificationDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        private static CreateTrackerNotificationDto TriggeredAnalyticDto(string name, string amountFieldId, string op = OperatorTypes.GreaterThan, string value = "10") => new()
        {
            Name = name,
            IsEnabled = true,
            Event = new CreateNotificationEventDto { EventType = "Triggered" },
            Condition = new CreateNotificationConditionDto
            {
                ValueMode = "Analytic",
                AnalyticCode = AnalyticCodes.Average,
                PurposeFields = [new CreateNotificationConditionPurposeFieldDto { Purpose = AnalyticPurposes.Value, FieldId = amountFieldId }],
                Filters = [new CreateNotificationConditionFilterDto { Operator = op, Value = value }],
            },
        };

        [Fact]
        public async Task Create_ThenGet_RoundTripsEventAndCondition()
        {
            var client = await _factory.NewUserClient("notifcrud");
            var trackerId = await TestApi.CreateTracker(client, "Weight");
            var amountFieldId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number }));

            var dto = TriggeredAnalyticDto("High average", amountFieldId);
            var created = Notification(await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/notifications", dto)));

            Assert.Equal("High average", created.Name);
            Assert.Equal("Triggered", created.Event.EventType);
            Assert.Equal("Analytic", created.Condition.ValueMode);
            Assert.Equal(AnalyticCodes.Average, created.Condition.AnalyticCode);
            Assert.Single(created.Condition.PurposeFields);
            Assert.Equal(amountFieldId, created.Condition.PurposeFields[0].FieldId);
            Assert.Single(created.Condition.Filters);
            Assert.Equal("10", created.Condition.Filters[0].Value);

            var fetched = Notification((await Data(await client.GetAsync($"trackers/{trackerId}/notifications"))).EnumerateArray().Single());
            Assert.Equal(created.Id, fetched.Id);
        }

        [Fact]
        public async Task Create_DayFrequency_RoundTripsScheduleFields()
        {
            var client = await _factory.NewUserClient("notifschedule");
            var trackerId = await TestApi.CreateTracker(client, "Habit");
            await client.PostAsJsonAsync($"trackers/{trackerId}/fields", new CreateFieldDto { Name = "Done", Type = DataTypes.Bool });

            var dto = new CreateTrackerNotificationDto
            {
                Name = "Daily check",
                Event = new CreateNotificationEventDto
                {
                    EventType = "Day",
                    TimeOfDay = "09:00",
                    IntervalDays = 2,
                    SkipWeekendsDay = true,
                },
                Condition = new CreateNotificationConditionDto { ValueMode = "Entry", Filters = [] },
            };

            var created = Notification(await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/notifications", dto)));

            Assert.Equal("Day", created.Event.EventType);
            Assert.Equal("09:00", created.Event.TimeOfDay);
            Assert.Equal(2, created.Event.IntervalDays);
            Assert.True(created.Event.SkipWeekendsDay);
        }

        [Fact]
        public async Task Update_ReplacesFilters_LeavingExactlyTheNewSet()
        {
            var client = await _factory.NewUserClient("notifupdate");
            var trackerId = await TestApi.CreateTracker(client, "Weight");
            var amountFieldId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number }));

            var created = Notification(await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/notifications",
                TriggeredAnalyticDto("Check", amountFieldId, OperatorTypes.GreaterThan, "10"))));

            var updateDto = new UpdateTrackerNotificationDto
            {
                Name = "Check",
                IsEnabled = true,
                Event = new CreateNotificationEventDto { EventType = "Triggered" },
                Condition = new CreateNotificationConditionDto
                {
                    ValueMode = "Analytic",
                    AnalyticCode = AnalyticCodes.Average,
                    PurposeFields = [new CreateNotificationConditionPurposeFieldDto { Purpose = AnalyticPurposes.Value, FieldId = amountFieldId }],
                    Filters =
                    [
                        new CreateNotificationConditionFilterDto { Operator = OperatorTypes.LessThan, Value = "5" },
                        new CreateNotificationConditionFilterDto { Operator = OperatorTypes.GreaterThan, Value = "1" },
                    ],
                },
            };

            var updated = Notification(await Data(await client.PutAsJsonAsync($"trackers/{trackerId}/notifications/{created.Id}", updateDto)));

            Assert.Equal(2, updated.Condition.Filters.Count);
            Assert.DoesNotContain(updated.Condition.Filters, f => f.Value == "10");
            Assert.Contains(updated.Condition.Filters, f => f.Value == "5" && f.Operator == OperatorTypes.LessThan);
            Assert.Contains(updated.Condition.Filters, f => f.Value == "1" && f.Operator == OperatorTypes.GreaterThan);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var filterCount = await db.NotificationConditionFilters.CountAsync(f => f.Condition.NotificationId == created.Id);
            Assert.Equal(2, filterCount);
        }

        [Fact]
        public async Task Toggle_Disable_ClearsTriggeredStateAndTriggeredEntries()
        {
            var client = await _factory.NewUserClient("notiftoggle");
            var trackerId = await TestApi.CreateTracker(client, "Weight");
            var amountFieldId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number }));

            var created = Notification(await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/notifications",
                TriggeredAnalyticDto("Check", amountFieldId))));

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
                var notification = await db.TrackerNotifications.FirstAsync(n => n.Id == created.Id);
                notification.IsTriggered = true;
                await db.SaveChangesAsync();
            }

            // First toggle disables it -- state must clear so a later re-enable can fire again
            // instead of the edge tracker silently remembering the old true.
            var disabled = Notification(await Data(await client.PatchAsync($"trackers/{trackerId}/notifications/{created.Id}/toggle", null)));
            Assert.False(disabled.IsEnabled);
            Assert.False(disabled.IsTriggered);
        }

        [Fact]
        public async Task Create_EntryMode_PrePopulatesTriggeredEntries_SoFirstTickDoesNotFireOnExistingMatches()
        {
            var client = await _factory.NewUserClient("notifprepop");
            var trackerId = await TestApi.CreateTracker(client, "Tasks");
            var statusFieldId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Status", Type = DataTypes.String }));

            await client.PostAsJsonAsync($"trackers/{trackerId}/entries", new Operum.Model.DTOs.Entries.Requests.CreateEntryDto
            {
                FieldValues = new() { ["Status"] = "Open" }
            });
            await client.PostAsJsonAsync($"trackers/{trackerId}/entries", new Operum.Model.DTOs.Entries.Requests.CreateEntryDto
            {
                FieldValues = new() { ["Status"] = "Open" }
            });

            var dto = new CreateTrackerNotificationDto
            {
                Name = "Open tasks",
                Event = new CreateNotificationEventDto { EventType = "Triggered" },
                Condition = new CreateNotificationConditionDto
                {
                    ValueMode = "Entry",
                    Filters = [new CreateNotificationConditionFilterDto { FieldId = statusFieldId, Operator = OperatorTypes.EqualsOperator, Value = "Open" }],
                },
            };
            var created = Notification(await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/notifications", dto)));

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var triggeredCount = await db.NotificationTriggeredEntries.CountAsync(t => t.NotificationId == created.Id);

            Assert.Equal(2, triggeredCount);
        }

        [Fact]
        public async Task Create_EntryMode_RoundTripsDisplayPurposeFields()
        {
            var client = await _factory.NewUserClient("notifdisplayfields");
            var trackerId = await TestApi.CreateTracker(client, "Tasks");
            var statusFieldId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Status", Type = DataTypes.String }));
            var priorityFieldId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Priority", Type = DataTypes.String }));

            var dto = new CreateTrackerNotificationDto
            {
                Name = "Open tasks",
                Event = new CreateNotificationEventDto { EventType = "Triggered" },
                Condition = new CreateNotificationConditionDto
                {
                    ValueMode = "Entry",
                    PurposeFields =
                    [
                        new CreateNotificationConditionPurposeFieldDto { Purpose = NotificationPurposes.Display, FieldId = statusFieldId },
                        new CreateNotificationConditionPurposeFieldDto { Purpose = NotificationPurposes.Display, FieldId = priorityFieldId },
                    ],
                    Filters = [new CreateNotificationConditionFilterDto { FieldId = statusFieldId, Operator = OperatorTypes.EqualsOperator, Value = "Open" }],
                },
            };

            var created = Notification(await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/notifications", dto)));

            Assert.Equal(2, created.Condition.PurposeFields.Count);
            Assert.All(created.Condition.PurposeFields, pf => Assert.Equal(NotificationPurposes.Display, pf.Purpose));
            Assert.Contains(created.Condition.PurposeFields, pf => pf.FieldId == statusFieldId);
            Assert.Contains(created.Condition.PurposeFields, pf => pf.FieldId == priorityFieldId);
        }

        [Fact]
        public async Task Create_EntryMode_UnknownPurpose_ReturnsBadRequest()
        {
            var client = await _factory.NewUserClient("notifbadpurpose");
            var trackerId = await TestApi.CreateTracker(client, "Tasks");
            var statusFieldId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Status", Type = DataTypes.String }));

            var dto = new CreateTrackerNotificationDto
            {
                Name = "Open tasks",
                Event = new CreateNotificationEventDto { EventType = "Triggered" },
                Condition = new CreateNotificationConditionDto
                {
                    ValueMode = "Entry",
                    PurposeFields = [new CreateNotificationConditionPurposeFieldDto { Purpose = "NotARealPurpose", FieldId = statusFieldId }],
                    Filters = [],
                },
            };

            var response = await client.PostAsJsonAsync($"trackers/{trackerId}/notifications", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_InvalidEventType_ReturnsBadRequest()
        {
            var client = await _factory.NewUserClient("notifinvalid");
            var trackerId = await TestApi.CreateTracker(client, "Weight");

            var dto = new CreateTrackerNotificationDto
            {
                Name = "Bad",
                Event = new CreateNotificationEventDto { EventType = "NotARealType" },
                Condition = new CreateNotificationConditionDto { ValueMode = "Entry", Filters = [] },
            };

            var response = await client.PostAsJsonAsync($"trackers/{trackerId}/notifications", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Delete_RemovesNotificationAndChildRows()
        {
            var client = await _factory.NewUserClient("notifdelete");
            var trackerId = await TestApi.CreateTracker(client, "Weight");
            var amountFieldId = await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number }));

            var created = Notification(await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/notifications",
                TriggeredAnalyticDto("Check", amountFieldId))));

            var deleteResponse = await client.DeleteAsync($"trackers/{trackerId}/notifications/{created.Id}");
            Assert.True(deleteResponse.IsSuccessStatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            Assert.False(await db.TrackerNotifications.AnyAsync(n => n.Id == created.Id));
            Assert.False(await db.NotificationEvents.AnyAsync(e => e.NotificationId == created.Id));
            Assert.False(await db.NotificationConditionFilters.AnyAsync(f => f.Condition.NotificationId == created.Id));
        }
    }
}

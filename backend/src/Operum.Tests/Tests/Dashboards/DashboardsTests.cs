using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Dashboard.Requests;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Tests.Extensions;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Dashboards
{
    public class DashboardsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
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

        // A tracker with one entry and the three field types the dashboard tests draw on:
        // a date and a number (a line chart's X/Y) and a string (a bar chart's category,
        // or a line chart X of a different data type). Deliberately has no analytic of its
        // own — a dashboard item never reuses one.
        private static async Task<CapableTracker> CreateCapableTracker(HttpClient client, string name)
        {
            var tracker = await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = name }));
            var trackerId = tracker.GetProperty("id").GetString()!;

            var dayField = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Day", Type = DataTypes.Date }));
            var amountField = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number }));
            var categoryField = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Category", Type = DataTypes.String }));

            await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto
                {
                    FieldValues = new()
                    {
                        ["Day"] = "2026-01-01",
                        ["Amount"] = "5",
                        ["Category"] = "Cardio"
                    }
                });

            return new CapableTracker(
                trackerId,
                dayField.GetProperty("id").GetString()!,
                amountField.GetProperty("id").GetString()!,
                categoryField.GetProperty("id").GetString()!);
        }

        private sealed record CapableTracker(string Id, string DayFieldId, string AmountFieldId, string CategoryFieldId);

        // The "Raw Values" line chart source for a tracker, mapping the given field to the
        // x-axis (Day unless a test wants a differently typed axis) and Amount to the y-axis.
        private static DashboardItemSourceRequestDto LineSource(CapableTracker tracker, string? xFieldId = null) => new()
        {
            TrackerId = tracker.Id,
            AnalyticFields =
            [
                new CreateAnalyticFieldDto { FieldId = xFieldId ?? tracker.DayFieldId, Purpose = AnalyticPurposes.Xaxis },
                new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Yaxis }
            ]
        };

        // The "Count per Category" bar chart source for a tracker — Name is the only purpose
        // that code requires.
        private static DashboardItemSourceRequestDto BarSource(CapableTracker tracker) => new()
        {
            TrackerId = tracker.Id,
            AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.CategoryFieldId, Purpose = AnalyticPurposes.Name }]
        };

        private static async Task<string> CreateDashboard(HttpClient client)
        {
            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            return dashboard.GetProperty("id").GetString()!;
        }

        [Fact]
        public async Task AddDashboardItem_SingleSource_ReturnsNativeChartTypeUnchanged()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                ResultType = AnalyticTypes.BarChart,
                Code = AnalyticCodes.CountBarChart,
                Sources = [BarSource(tracker)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var results = await Data(await client.GetAsync($"dashboard/{dashboardId}/analytics"));

            Assert.Equal(1, results.GetArrayLength());
            Assert.Equal(AnalyticTypes.BarChart, results[0].GetProperty("resultType").GetString());
        }

        [Fact]
        public async Task AddDashboardItem_TwoSources_MergesIntoComposedChart()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var weight = await CreateCapableTracker(client, "Weight");
            var steps = await CreateCapableTracker(client, "Steps");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources = [LineSource(weight), LineSource(steps)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var results = await Data(await client.GetAsync($"dashboard/{dashboardId}/analytics"));

            Assert.Equal(1, results.GetArrayLength());
            var combined = results[0];
            Assert.Equal(AnalyticTypes.Composed, combined.GetProperty("resultType").GetString());
            Assert.Equal(2, combined.GetProperty("series").GetArrayLength());
            // One definition for both sources, and both plot a date on the x-axis, so there
            // is nothing left to warn about.
            Assert.Equal(0, combined.GetProperty("warnings").GetArrayLength());
        }

        // Sharing a definition still leaves one thing sources can disagree on: the data type
        // of the field on the x-axis, which the combined chart has to render on one axis.
        [Fact]
        public async Task AddDashboardItem_SourcesWithDifferentXAxisTypes_WarnsWithoutRejecting()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var byDate = await CreateCapableTracker(client, "Weight");
            var byCategory = await CreateCapableTracker(client, "Steps");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources = [LineSource(byDate), LineSource(byCategory, byCategory.CategoryFieldId)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var combined = (await Data(await client.GetAsync($"dashboard/{dashboardId}/analytics")))[0];
            Assert.Equal(2, combined.GetProperty("series").GetArrayLength());
            Assert.True(combined.GetProperty("warnings").GetArrayLength() > 0);
        }

        [Fact]
        public async Task AddDashboardItem_MultipleSourcesForANonCombinableType_ReturnsBadRequest()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var workouts = await CreateCapableTracker(client, "Workouts");
            var steps = await CreateCapableTracker(client, "Steps");
            var dashboardId = await CreateDashboard(client);

            DashboardItemSourceRequestDto CountSource(CapableTracker tracker) => new()
            {
                TrackerId = tracker.Id,
                AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.CategoryFieldId, Purpose = AnalyticPurposes.Value }]
            };

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                ResultType = AnalyticTypes.SingleValue,
                Code = AnalyticCodes.Count,
                Sources = [CountSource(workouts), CountSource(steps)]
            });

            Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
        }

        [Fact]
        public async Task AddDashboardItem_CodeDoesNotBelongToResultType_ReturnsBadRequest()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                ResultType = AnalyticTypes.LineChart,
                // A bar chart code, which the line chart definition knows nothing about.
                Code = AnalyticCodes.CountBarChart,
                Sources = [LineSource(tracker)]
            });

            Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
        }

        // Regression test: two separate dashboard items whose sources both point at the same
        // Tracker used to make GetUserDashboard's untracked query materialize that Tracker as
        // two distinct CLR instances (no identity resolution under the app's default
        // QueryTrackingBehavior.NoTracking). Remove() then threw when attaching the detached
        // graph and hitting the second same-key instance. See DashboardService.GetUserDashboard.
        [Fact]
        public async Task DeleteDashboard_ItemsShareATracker_Succeeds()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            for (var i = 0; i < 2; i++)
            {
                var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
                {
                    ResultType = AnalyticTypes.BarChart,
                    Code = AnalyticCodes.CountBarChart,
                    Sources = [BarSource(tracker)]
                });
                Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
            }

            var deleteResponse = await client.DeleteAsync($"dashboard/{dashboardId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        }

        // Same underlying scenario as above, but via the single-item removal endpoint rather
        // than deleting the whole dashboard.
        [Fact]
        public async Task RemoveDashboardItem_OtherItemSharesATracker_Succeeds()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            string? firstItemId = null;
            for (var i = 0; i < 2; i++)
            {
                var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
                {
                    ResultType = AnalyticTypes.BarChart,
                    Code = AnalyticCodes.CountBarChart,
                    Sources = [BarSource(tracker)]
                });
                Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
                firstItemId ??= (await Data(addResponse)).GetProperty("id").GetString();
            }

            var removeResponse = await client.DeleteAsync($"dashboard/{dashboardId}/items/{firstItemId}");
            Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        }

        [Fact]
        public async Task AddDashboardItem_Source_RendersWithoutCreatingATrackerAnalytic()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources = [LineSource(tracker)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var results = await Data(await client.GetAsync($"dashboard/{dashboardId}/analytics"));
            Assert.Equal(1, results.GetArrayLength());
            Assert.Equal(AnalyticTypes.LineChart, results[0].GetProperty("resultType").GetString());

            // The whole point: the definition stays on the dashboard and never shows up
            // among the tracker's own analytics.
            var trackerAnalytics = await Data(await client.GetAsync($"trackers/{tracker.Id}/analytics"));
            Assert.Equal(0, trackerAnalytics.GetArrayLength());
        }

        [Fact]
        public async Task AddDashboardItem_SourceMissingARequiredPurpose_ReturnsBadRequest()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources =
                [
                    new DashboardItemSourceRequestDto
                    {
                        TrackerId = tracker.Id,
                        // Y-axis left unmapped.
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.DayFieldId, Purpose = AnalyticPurposes.Xaxis }]
                    }
                ]
            });

            Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
        }

        // A dashboard spans trackers, so a source must not be able to reach a field that
        // belongs to a different tracker than the one it reads entries from.
        [Fact]
        public async Task AddDashboardItem_SourceFieldFromAnotherTracker_ReturnsNotFound()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var other = await CreateCapableTracker(client, "Steps");
            var dashboardId = await CreateDashboard(client);

            Assert.NotEqual(tracker.Id, other.Id);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources =
                [
                    new DashboardItemSourceRequestDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields =
                        [
                            new CreateAnalyticFieldDto { FieldId = tracker.DayFieldId, Purpose = AnalyticPurposes.Xaxis },
                            new CreateAnalyticFieldDto { FieldId = other.AmountFieldId, Purpose = AnalyticPurposes.Yaxis }
                        ]
                    }
                ]
            });

            Assert.Equal(HttpStatusCode.NotFound, addResponse.StatusCode);
        }

        // Removing the dashboard item is the only lifecycle a source definition has — it must
        // take its source and field mappings with it rather than leaving orphans.
        [Fact]
        public async Task RemoveDashboardItem_RemovesTheDefinitionToo()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources = [LineSource(tracker)]
            });
            var itemId = (await Data(addResponse)).GetProperty("id").GetString()!;

            var removeResponse = await client.DeleteAsync($"dashboard/{dashboardId}/items/{itemId}");
            Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);

            var fetched = await Data(await client.GetAsync($"dashboard/{dashboardId}"));
            Assert.Equal(0, fetched.GetProperty("items").GetArrayLength());
        }

        // Regression test: GetUserDashboard's query must be tracked, otherwise mutating the
        // fetched entity and calling SaveChanges silently persists nothing.
        [Fact]
        public async Task UpdateDashboard_PersistsNewName()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var dashboardId = await CreateDashboard(client);

            var updateResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}", new UpdateDashboardDto { Name = "Renamed board" });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var fetched = await Data(await client.GetAsync($"dashboard/{dashboardId}"));
            Assert.Equal("Renamed board", fetched.GetProperty("name").GetString());
        }
    }
}

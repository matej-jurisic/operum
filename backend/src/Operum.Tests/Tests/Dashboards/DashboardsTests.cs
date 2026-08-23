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

        // Creates a tracker with a single string field, one entry, and a "Count per
        // Category" bar chart analytic over that field (the minimal setup for a valid
        // BarChart analytic — no Value field required).
        private static async Task<(string TrackerId, string AnalyticId)> CreateBarAnalyticTracker(HttpClient client, string name)
        {
            var tracker = await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = name }));
            var trackerId = tracker.GetProperty("id").GetString()!;

            var field = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Category", Type = DataTypes.String }));
            var fieldId = field.GetProperty("id").GetString()!;

            await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Category"] = "Cardio" } });

            // AddAnalytic returns Result (no payload) by design — the caller is expected
            // to refetch, same as the frontend does after creating one.
            await client.PostAsJsonAsync($"trackers/{trackerId}/analytics",
                new CreateAnalyticDto
                {
                    Type = AnalyticTypes.BarChart,
                    Code = AnalyticCodes.CountBarChart,
                    AnalyticFields = [new CreateAnalyticFieldDto { FieldId = fieldId, Purpose = AnalyticPurposes.Name }]
                });
            var analyticId = await GetSoleAnalyticId(client, trackerId);

            return (trackerId, analyticId);
        }

        // Creates a tracker with date/number fields, one entry, and a raw "Line Chart"
        // line analytic over them (the minimal setup for a valid LineChart analytic).
        private static async Task<(string TrackerId, string AnalyticId)> CreateLineAnalyticTracker(HttpClient client, string name)
        {
            var tracker = await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = name }));
            var trackerId = tracker.GetProperty("id").GetString()!;

            var dayField = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Day", Type = DataTypes.Date }));
            var amountField = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number }));

            await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Day"] = "2026-01-01", ["Amount"] = "5" } });

            await client.PostAsJsonAsync($"trackers/{trackerId}/analytics",
                new CreateAnalyticDto
                {
                    Type = AnalyticTypes.LineChart,
                    Code = AnalyticCodes.LineChart,
                    AnalyticFields =
                    [
                        new CreateAnalyticFieldDto { FieldId = dayField.GetProperty("id").GetString()!, Purpose = AnalyticPurposes.Xaxis },
                        new CreateAnalyticFieldDto { FieldId = amountField.GetProperty("id").GetString()!, Purpose = AnalyticPurposes.Yaxis }
                    ]
                });
            var analyticId = await GetSoleAnalyticId(client, trackerId);

            return (trackerId, analyticId);
        }

        // Creates a tracker with a single-value ("Count") analytic — one of the result
        // types that can never be combined with another tracker's chart.
        private static async Task<(string TrackerId, string AnalyticId)> CreateSingleValueAnalyticTracker(HttpClient client, string name)
        {
            var tracker = await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = name }));
            var trackerId = tracker.GetProperty("id").GetString()!;

            var field = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Category", Type = DataTypes.String }));

            await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Category"] = "Cardio" } });

            await client.PostAsJsonAsync($"trackers/{trackerId}/analytics",
                new CreateAnalyticDto
                {
                    Type = AnalyticTypes.SingleValue,
                    Code = AnalyticCodes.Count,
                    AnalyticFields = [new CreateAnalyticFieldDto { FieldId = field.GetProperty("id").GetString()!, Purpose = AnalyticPurposes.Value }]
                });
            var analyticId = await GetSoleAnalyticId(client, trackerId);

            return (trackerId, analyticId);
        }

        private static async Task<string> GetSoleAnalyticId(HttpClient client, string trackerId)
        {
            var summary = await Data(await client.GetAsync($"trackers/{trackerId}/analytics/summary"));
            return summary[0].GetProperty("id").GetString()!;
        }

        [Fact]
        public async Task AddDashboardItem_SingleSource_ReturnsNativeChartTypeUnchanged()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var (trackerId, analyticId) = await CreateBarAnalyticTracker(client, "Workouts");

            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            var dashboardId = dashboard.GetProperty("id").GetString()!;

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                Sources = [new DashboardItemSourceRequestDto { TrackerId = trackerId, AnalyticId = analyticId }]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var analyticsResponse = await client.GetAsync($"dashboard/{dashboardId}/analytics");
            var results = await Data(analyticsResponse);

            Assert.Equal(1, results.GetArrayLength());
            Assert.Equal(AnalyticTypes.BarChart, results[0].GetProperty("resultType").GetString());
        }

        [Fact]
        public async Task AddDashboardItem_TwoCombinableSources_MergesIntoComposedChartWithWarning()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var (barTrackerId, barAnalyticId) = await CreateBarAnalyticTracker(client, "Workouts");
            var (lineTrackerId, lineAnalyticId) = await CreateLineAnalyticTracker(client, "Weight");

            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            var dashboardId = dashboard.GetProperty("id").GetString()!;

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                Sources =
                [
                    new DashboardItemSourceRequestDto { TrackerId = barTrackerId, AnalyticId = barAnalyticId },
                    new DashboardItemSourceRequestDto { TrackerId = lineTrackerId, AnalyticId = lineAnalyticId }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var results = await Data(await client.GetAsync($"dashboard/{dashboardId}/analytics"));

            Assert.Equal(1, results.GetArrayLength());
            var combined = results[0];
            Assert.Equal(AnalyticTypes.Composed, combined.GetProperty("resultType").GetString());
            Assert.Equal(2, combined.GetProperty("series").GetArrayLength());
            // Mixing a bar and a line source should surface a warning, not be rejected.
            Assert.True(combined.GetProperty("warnings").GetArrayLength() > 0);
        }

        [Fact]
        public async Task AddDashboardItem_SecondSourceNotCombinable_ReturnsBadRequest()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var (barTrackerId, barAnalyticId) = await CreateBarAnalyticTracker(client, "Workouts");
            var (singleTrackerId, singleAnalyticId) = await CreateSingleValueAnalyticTracker(client, "Steps");

            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            var dashboardId = dashboard.GetProperty("id").GetString()!;

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                Sources =
                [
                    new DashboardItemSourceRequestDto { TrackerId = barTrackerId, AnalyticId = barAnalyticId },
                    new DashboardItemSourceRequestDto { TrackerId = singleTrackerId, AnalyticId = singleAnalyticId }
                ]
            });

            Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
        }

        // Regression test: two separate dashboard items whose sources both point at the same
        // Tracker/Analytic used to make GetUserDashboard's untracked query materialize that
        // Tracker as two distinct CLR instances (no identity resolution under the app's default
        // QueryTrackingBehavior.NoTracking). Remove() then threw when attaching the detached
        // graph and hitting the second same-key instance. See DashboardService.GetUserDashboard.
        [Fact]
        public async Task DeleteDashboard_ItemsShareATracker_Succeeds()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var (trackerId, analyticId) = await CreateBarAnalyticTracker(client, "Workouts");

            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            var dashboardId = dashboard.GetProperty("id").GetString()!;

            for (var i = 0; i < 2; i++)
            {
                var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
                {
                    Sources = [new DashboardItemSourceRequestDto { TrackerId = trackerId, AnalyticId = analyticId }]
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

            var (trackerId, analyticId) = await CreateBarAnalyticTracker(client, "Workouts");

            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            var dashboardId = dashboard.GetProperty("id").GetString()!;

            string? firstItemId = null;
            for (var i = 0; i < 2; i++)
            {
                var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
                {
                    Sources = [new DashboardItemSourceRequestDto { TrackerId = trackerId, AnalyticId = analyticId }]
                });
                Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
                firstItemId ??= (await Data(addResponse)).GetProperty("id").GetString();
            }

            var removeResponse = await client.DeleteAsync($"dashboard/{dashboardId}/items/{firstItemId}");
            Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        }

        // Creates a tracker with date/number fields and one entry, but deliberately no
        // analytic — the starting point for an ad hoc dashboard source.
        private static async Task<(string TrackerId, string DayFieldId, string AmountFieldId)> CreateLineCapableTracker(HttpClient client, string name)
        {
            var tracker = await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = name }));
            var trackerId = tracker.GetProperty("id").GetString()!;

            var dayField = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Day", Type = DataTypes.Date }));
            var amountField = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number }));

            await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Day"] = "2026-01-01", ["Amount"] = "5" } });

            return (trackerId, dayField.GetProperty("id").GetString()!, amountField.GetProperty("id").GetString()!);
        }

        [Fact]
        public async Task AddDashboardItem_AdHocSource_RendersWithoutCreatingATrackerAnalytic()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var (trackerId, dayFieldId, amountFieldId) = await CreateLineCapableTracker(client, "Weight");

            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            var dashboardId = dashboard.GetProperty("id").GetString()!;

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                Sources =
                [
                    new DashboardItemSourceRequestDto
                    {
                        TrackerId = trackerId,
                        ResultType = AnalyticTypes.LineChart,
                        Code = AnalyticCodes.LineChart,
                        AnalyticFields =
                        [
                            new CreateAnalyticFieldDto { FieldId = dayFieldId, Purpose = AnalyticPurposes.Xaxis },
                            new CreateAnalyticFieldDto { FieldId = amountFieldId, Purpose = AnalyticPurposes.Yaxis }
                        ]
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var results = await Data(await client.GetAsync($"dashboard/{dashboardId}/analytics"));
            Assert.Equal(1, results.GetArrayLength());
            Assert.Equal(AnalyticTypes.LineChart, results[0].GetProperty("resultType").GetString());

            // The whole point: the definition stays on the dashboard and never shows up
            // among the tracker's own analytics.
            var summary = await Data(await client.GetAsync($"trackers/{trackerId}/analytics/summary"));
            Assert.Equal(0, summary.GetArrayLength());
        }

        [Fact]
        public async Task AddDashboardItem_AdHocSourceCombinedWithSavedAnalytic_MergesIntoComposedChart()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var (savedTrackerId, savedAnalyticId) = await CreateLineAnalyticTracker(client, "Weight");
            var (adHocTrackerId, dayFieldId, amountFieldId) = await CreateLineCapableTracker(client, "Steps");

            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            var dashboardId = dashboard.GetProperty("id").GetString()!;

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                Sources =
                [
                    new DashboardItemSourceRequestDto { TrackerId = savedTrackerId, AnalyticId = savedAnalyticId },
                    new DashboardItemSourceRequestDto
                    {
                        TrackerId = adHocTrackerId,
                        ResultType = AnalyticTypes.LineChart,
                        Code = AnalyticCodes.LineChart,
                        AnalyticFields =
                        [
                            new CreateAnalyticFieldDto { FieldId = dayFieldId, Purpose = AnalyticPurposes.Xaxis },
                            new CreateAnalyticFieldDto { FieldId = amountFieldId, Purpose = AnalyticPurposes.Yaxis }
                        ]
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var results = await Data(await client.GetAsync($"dashboard/{dashboardId}/analytics"));
            var combined = results[0];
            Assert.Equal(AnalyticTypes.Composed, combined.GetProperty("resultType").GetString());
            Assert.Equal(2, combined.GetProperty("series").GetArrayLength());
            // Same result type and code on both sides, so nothing to warn about.
            Assert.Equal(0, combined.GetProperty("warnings").GetArrayLength());
        }

        [Fact]
        public async Task AddDashboardItem_AdHocSourceMissingARequiredPurpose_ReturnsBadRequest()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var (trackerId, dayFieldId, _) = await CreateLineCapableTracker(client, "Weight");

            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            var dashboardId = dashboard.GetProperty("id").GetString()!;

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                Sources =
                [
                    new DashboardItemSourceRequestDto
                    {
                        TrackerId = trackerId,
                        ResultType = AnalyticTypes.LineChart,
                        Code = AnalyticCodes.LineChart,
                        // Y-axis left unmapped.
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = dayFieldId, Purpose = AnalyticPurposes.Xaxis }]
                    }
                ]
            });

            Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
        }

        // A dashboard spans trackers, so an ad hoc source must not be able to reach a
        // field that belongs to a different tracker than the one it reads entries from.
        [Fact]
        public async Task AddDashboardItem_AdHocSourceFieldFromAnotherTracker_ReturnsNotFound()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var (trackerId, dayFieldId, _) = await CreateLineCapableTracker(client, "Weight");
            var (otherTrackerId, _, otherAmountFieldId) = await CreateLineCapableTracker(client, "Steps");

            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            var dashboardId = dashboard.GetProperty("id").GetString()!;

            Assert.NotEqual(trackerId, otherTrackerId);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                Sources =
                [
                    new DashboardItemSourceRequestDto
                    {
                        TrackerId = trackerId,
                        ResultType = AnalyticTypes.LineChart,
                        Code = AnalyticCodes.LineChart,
                        AnalyticFields =
                        [
                            new CreateAnalyticFieldDto { FieldId = dayFieldId, Purpose = AnalyticPurposes.Xaxis },
                            new CreateAnalyticFieldDto { FieldId = otherAmountFieldId, Purpose = AnalyticPurposes.Yaxis }
                        ]
                    }
                ]
            });

            Assert.Equal(HttpStatusCode.NotFound, addResponse.StatusCode);
        }

        // Removing the dashboard item is the only lifecycle an ad hoc definition has —
        // it must take its source and field mappings with it rather than leaving orphans.
        [Fact]
        public async Task RemoveDashboardItem_AdHocSource_RemovesTheDefinitionToo()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var (trackerId, dayFieldId, amountFieldId) = await CreateLineCapableTracker(client, "Weight");

            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            var dashboardId = dashboard.GetProperty("id").GetString()!;

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                Sources =
                [
                    new DashboardItemSourceRequestDto
                    {
                        TrackerId = trackerId,
                        ResultType = AnalyticTypes.LineChart,
                        Code = AnalyticCodes.LineChart,
                        AnalyticFields =
                        [
                            new CreateAnalyticFieldDto { FieldId = dayFieldId, Purpose = AnalyticPurposes.Xaxis },
                            new CreateAnalyticFieldDto { FieldId = amountFieldId, Purpose = AnalyticPurposes.Yaxis }
                        ]
                    }
                ]
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

            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            var dashboardId = dashboard.GetProperty("id").GetString()!;

            var updateResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}", new UpdateDashboardDto { Name = "Renamed board" });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var fetched = await Data(await client.GetAsync($"dashboard/{dashboardId}"));
            Assert.Equal("Renamed board", fetched.GetProperty("name").GetString());
        }
    }
}

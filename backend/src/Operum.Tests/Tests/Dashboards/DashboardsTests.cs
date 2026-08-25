using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Dashboard.Requests;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Views.Requests;
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

        // The board as the client renders it: a placement per widget wrapped around the
        // chart calculated for it, which is what these tests are usually after.
        private static async Task<JsonElement> Widgets(HttpClient client, string dashboardId)
            => await Data(await client.GetAsync($"dashboard/{dashboardId}/widgets"));

        private static JsonElement Analytic(JsonElement widget) => widget.GetProperty("analytic");

        private static JsonElement Layout(JsonElement widget) => widget.GetProperty("layout");

        private static JsonElement MobileLayout(JsonElement widget) => widget.GetProperty("mobileLayout");

        // Adds the "Raw Values" line chart of a tracker and hands back the item's id.
        private static async Task<string> AddLineItem(HttpClient client, string dashboardId, CapableTracker tracker)
        {
            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new AddDashboardItemDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources = [LineSource(tracker)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
            return (await Data(addResponse)).GetProperty("id").GetString()!;
        }

        // Creates a "Raw Values" line chart on the tracker itself and hands back its id, so
        // a test can add it to a board the way the widget picker does.
        private static async Task<string> CreateTrackerAnalytic(HttpClient client, CapableTracker tracker)
        {
            var createResponse = await client.PostAsJsonAsync($"trackers/{tracker.Id}/analytics", new CreateAnalyticDto
            {
                Type = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                AnalyticFields =
                [
                    new CreateAnalyticFieldDto { FieldId = tracker.DayFieldId, Purpose = AnalyticPurposes.Xaxis },
                    new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Yaxis }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            var analytics = await Data(await client.GetAsync($"trackers/{tracker.Id}/analytics"));
            return analytics[analytics.GetArrayLength() - 1].GetProperty("id").GetString()!;
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

            var results = await Widgets(client, dashboardId);

            Assert.Equal(1, results.GetArrayLength());
            Assert.Equal(AnalyticTypes.BarChart, Analytic(results[0]).GetProperty("resultType").GetString());
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

            var results = await Widgets(client, dashboardId);

            Assert.Equal(1, results.GetArrayLength());
            var combined = Analytic(results[0]);
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

            var combined = Analytic((await Widgets(client, dashboardId))[0]);
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

            var results = await Widgets(client, dashboardId);
            Assert.Equal(1, results.GetArrayLength());
            Assert.Equal(AnalyticTypes.LineChart, Analytic(results[0]).GetProperty("resultType").GetString());

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


        // Adding an existing analytic copies its definition onto the board: the widget keeps
        // rendering after the tracker's own analytic is deleted, which is what makes the item
        // a copy rather than a reference.
        [Fact]
        public async Task AddDashboardItemFromAnalytic_CopiesTheDefinitionInsteadOfReferencingIt()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("analyticcopy");

            var tracker = await CreateCapableTracker(client, "Weight");
            var analyticId = await CreateTrackerAnalytic(client, tracker);
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/from-analytic",
                new AddDashboardItemFromAnalyticDto { AnalyticId = analyticId });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var item = await Data(addResponse);
            Assert.Equal(AnalyticTypes.LineChart, item.GetProperty("resultType").GetString());
            Assert.Equal(AnalyticCodes.LineChart, item.GetProperty("code").GetString());
            Assert.Equal(1, item.GetProperty("sources").GetArrayLength());
            Assert.Equal(2, item.GetProperty("sources")[0].GetProperty("fields").GetArrayLength());

            var removeResponse = await client.DeleteAsync($"trackers/{tracker.Id}/analytics/{analyticId}");
            Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(1, widgets.GetArrayLength());
            Assert.Equal(AnalyticTypes.LineChart, Analytic(widgets[0]).GetProperty("resultType").GetString());
        }

        [Fact]
        public async Task AddDashboardItemFromAnalytic_ViewIdsNarrowTheWidget()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("analyticview");

            var tracker = await CreateCapableTracker(client, "Weight");
            var analyticId = await CreateTrackerAnalytic(client, tracker);
            var dashboardId = await CreateDashboard(client);

            // Nothing has a category of "Strength", so the view filters the single entry out.
            var view = await Data(await client.PostAsJsonAsync($"trackers/{tracker.Id}/views", new CreateViewDto
            {
                Name = "Strength only",
                Filters =
                [
                    new CreateViewFilterDto
                    {
                        FieldId = tracker.CategoryFieldId,
                        Operator = OperatorTypes.EqualsOperator,
                        Value = "Strength"
                    }
                ]
            }));

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/from-analytic",
                new AddDashboardItemFromAnalyticDto
                {
                    AnalyticId = analyticId,
                    ViewIds = [view.GetProperty("id").GetString()!]
                });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(0, Analytic(widgets[0]).GetProperty("points").GetArrayLength());
        }

        [Fact]
        public async Task AddDashboardItemFromAnalytic_UnknownAnalytic_ReturnsNotFound()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("analyticmissing");

            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/from-analytic",
                new AddDashboardItemFromAnalyticDto { AnalyticId = Guid.NewGuid().ToString() });

            Assert.Equal(HttpStatusCode.NotFound, addResponse.StatusCode);
        }

        // The shortcut must not become a way around tracker access: an analytic id is enough
        // to name a tracker, so the normal add path still has to answer for it.
        [Fact]
        public async Task AddDashboardItemFromAnalytic_AnalyticOfAnotherUsersTracker_ReturnsForbidden()
        {
            await _factory.SeedDatabaseAsync();

            var owner = await _factory.NewUserClient("analyticowner");
            var tracker = await CreateCapableTracker(owner, "Weight");
            var analyticId = await CreateTrackerAnalytic(owner, tracker);

            var stranger = await _factory.NewUserClient("analyticstranger");
            var dashboardId = await CreateDashboard(stranger);

            var addResponse = await stranger.PostAsJsonAsync($"dashboard/{dashboardId}/items/from-analytic",
                new AddDashboardItemFromAnalyticDto { AnalyticId = analyticId });

            Assert.Equal(HttpStatusCode.Forbidden, addResponse.StatusCode);
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

        // A widget the user has not placed yet still has to land somewhere sensible, which
        // means below what is already on the board rather than on top of it.
        [Fact]
        public async Task AddDashboardItem_PlacesTheWidgetUnderTheOnesAlreadyOnTheBoard()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            await AddLineItem(client, dashboardId, tracker);
            await AddLineItem(client, dashboardId, tracker);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(2, widgets.GetArrayLength());

            var first = Layout(widgets[0]);
            var second = Layout(widgets[1]);

            Assert.Equal(0, first.GetProperty("y").GetInt32());
            Assert.True(first.GetProperty("w").GetInt32() > 0);
            Assert.True(first.GetProperty("h").GetInt32() > 0);
            Assert.Equal(first.GetProperty("y").GetInt32() + first.GetProperty("h").GetInt32(), second.GetProperty("y").GetInt32());
        }

        [Fact]
        public async Task UpdateDashboardLayout_PersistsThePlacement()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var layoutResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Items = [new DashboardLayoutItemDto { ItemId = itemId, X = 4, Y = 2, W = 5, H = 7 }]
            });
            Assert.Equal(HttpStatusCode.OK, layoutResponse.StatusCode);

            var layout = Layout((await Widgets(client, dashboardId))[0]);
            Assert.Equal(4, layout.GetProperty("x").GetInt32());
            Assert.Equal(2, layout.GetProperty("y").GetInt32());
            Assert.Equal(5, layout.GetProperty("w").GetInt32());
            Assert.Equal(7, layout.GetProperty("h").GetInt32());
        }

        // A client that lays out more columns than the grid has would otherwise push widgets
        // off the right edge, where they cannot be dragged back.
        [Fact]
        public async Task UpdateDashboardLayout_PlacementOutsideTheGrid_IsClampedNotRejected()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var layoutResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Items = [new DashboardLayoutItemDto { ItemId = itemId, X = DashboardGrid.Columns - 1, Y = 0, W = 8, H = DashboardGrid.MaxHeight + 10 }]
            });
            Assert.Equal(HttpStatusCode.OK, layoutResponse.StatusCode);

            var layout = Layout((await Widgets(client, dashboardId))[0]);
            Assert.Equal(8, layout.GetProperty("w").GetInt32());
            Assert.Equal(DashboardGrid.Columns - 8, layout.GetProperty("x").GetInt32());
            Assert.Equal(DashboardGrid.MaxHeight, layout.GetProperty("h").GetInt32());
        }

        // The grid sends the whole board at once, so an item that was removed in another tab
        // must not fail the save for everything else.
        [Fact]
        public async Task UpdateDashboardLayout_UnknownItem_IsIgnored()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var layoutResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Items =
                [
                    new DashboardLayoutItemDto { ItemId = itemId, X = 0, Y = 0, W = 4, H = 4 },
                    new DashboardLayoutItemDto { ItemId = Guid.NewGuid().ToString(), X = 4, Y = 0, W = 4, H = 4 }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, layoutResponse.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(1, widgets.GetArrayLength());
            Assert.Equal(4, Layout(widgets[0]).GetProperty("w").GetInt32());
        }

        // A board is arranged twice, so a widget added on one screen still has to be
        // somewhere sensible on the other. The narrow grid has no room beside anything, so
        // a new widget takes the full width of it and stacks under what is already there.
        [Fact]
        public async Task AddDashboardItem_PlacesTheWidgetOnBothGrids()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("bothgrids");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            await AddLineItem(client, dashboardId, tracker);
            await AddLineItem(client, dashboardId, tracker);

            var widgets = await Widgets(client, dashboardId);
            var first = MobileLayout(widgets[0]);
            var second = MobileLayout(widgets[1]);

            Assert.Equal(0, first.GetProperty("x").GetInt32());
            Assert.Equal(0, first.GetProperty("y").GetInt32());
            Assert.Equal(DashboardGrid.MobileColumns, first.GetProperty("w").GetInt32());
            Assert.True(first.GetProperty("h").GetInt32() > 0);

            Assert.Equal(0, second.GetProperty("x").GetInt32());
            Assert.Equal(DashboardGrid.MobileColumns, second.GetProperty("w").GetInt32());
            Assert.Equal(
                first.GetProperty("y").GetInt32() + first.GetProperty("h").GetInt32(),
                second.GetProperty("y").GetInt32());
        }

        // The whole point of storing two arrangements: dragging a widget on a phone must
        // not move it on the desktop board, and arranging the desktop board afterwards must
        // not undo what was done on the phone.
        [Fact]
        public async Task UpdateDashboardLayout_WritesOnlyTheGridItWasMadeOn()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("gridsapart");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var desktopResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Variant = DashboardLayoutVariants.Desktop,
                Items = [new DashboardLayoutItemDto { ItemId = itemId, X = 4, Y = 2, W = 5, H = 7 }]
            });
            Assert.Equal(HttpStatusCode.OK, desktopResponse.StatusCode);

            var mobileResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Variant = DashboardLayoutVariants.Mobile,
                Items = [new DashboardLayoutItemDto { ItemId = itemId, X = 0, Y = 3, W = 4, H = 9 }]
            });
            Assert.Equal(HttpStatusCode.OK, mobileResponse.StatusCode);

            var widget = (await Widgets(client, dashboardId))[0];

            var desktop = Layout(widget);
            Assert.Equal(4, desktop.GetProperty("x").GetInt32());
            Assert.Equal(2, desktop.GetProperty("y").GetInt32());
            Assert.Equal(5, desktop.GetProperty("w").GetInt32());
            Assert.Equal(7, desktop.GetProperty("h").GetInt32());

            var mobile = MobileLayout(widget);
            Assert.Equal(0, mobile.GetProperty("x").GetInt32());
            Assert.Equal(3, mobile.GetProperty("y").GetInt32());
            Assert.Equal(4, mobile.GetProperty("w").GetInt32());
            Assert.Equal(9, mobile.GetProperty("h").GetInt32());

            // Arranging the desktop board again leaves the phone's arrangement where it is.
            var reDesktopResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Variant = DashboardLayoutVariants.Desktop,
                Items = [new DashboardLayoutItemDto { ItemId = itemId, X = 0, Y = 0, W = 3, H = 3 }]
            });
            Assert.Equal(HttpStatusCode.OK, reDesktopResponse.StatusCode);

            var afterMobile = MobileLayout((await Widgets(client, dashboardId))[0]);
            Assert.Equal(3, afterMobile.GetProperty("y").GetInt32());
            Assert.Equal(4, afterMobile.GetProperty("w").GetInt32());
            Assert.Equal(9, afterMobile.GetProperty("h").GetInt32());
        }

        // A placement is clamped to the grid it was made on, not to the widest one there is,
        // or a phone could push a widget three columns off its own right edge.
        [Fact]
        public async Task UpdateDashboardLayout_MobilePlacementOutsideTheNarrowGrid_IsClamped()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("mobileclamp");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var layoutResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Variant = DashboardLayoutVariants.Mobile,
                Items = [new DashboardLayoutItemDto { ItemId = itemId, X = DashboardGrid.MobileColumns - 1, Y = 0, W = DashboardGrid.Columns, H = 4 }]
            });
            Assert.Equal(HttpStatusCode.OK, layoutResponse.StatusCode);

            var mobile = MobileLayout((await Widgets(client, dashboardId))[0]);
            Assert.Equal(DashboardGrid.MobileColumns, mobile.GetProperty("w").GetInt32());
            Assert.Equal(0, mobile.GetProperty("x").GetInt32());
        }

        // Unlike a placement, an unknown variant cannot be clamped into something sensible:
        // there is no telling which grid the numbers beside it belong to.
        [Fact]
        public async Task UpdateDashboardLayout_UnknownVariant_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("unknownvariant");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var layoutResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Variant = "watch",
                Items = [new DashboardLayoutItemDto { ItemId = itemId, X = 0, Y = 0, W = 4, H = 4 }]
            });

            Assert.Equal(HttpStatusCode.BadRequest, layoutResponse.StatusCode);
        }

        // The two arrangements can disagree about what comes first, so only the wide grid
        // gets a say in the board's reading order. Otherwise it would flip back and forth
        // with whichever screen was used last.
        [Fact]
        public async Task UpdateDashboardLayout_MobileVariant_DoesNotRewriteTheReadingOrder()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("mobileorder");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var firstId = await AddLineItem(client, dashboardId, tracker);
            var secondId = await AddLineItem(client, dashboardId, tracker);

            // Stack them the other way round on the phone: second on top, first below.
            var layoutResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Variant = DashboardLayoutVariants.Mobile,
                Items =
                [
                    new DashboardLayoutItemDto { ItemId = secondId, X = 0, Y = 0, W = 4, H = 4 },
                    new DashboardLayoutItemDto { ItemId = firstId, X = 0, Y = 4, W = 4, H = 4 }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, layoutResponse.StatusCode);

            var items = (await Data(await client.GetAsync($"dashboard/{dashboardId}"))).GetProperty("items");
            var orderById = items.EnumerateArray()
                .ToDictionary(i => i.GetProperty("id").GetString()!, i => i.GetProperty("order").GetInt32());

            Assert.True(orderById[firstId] < orderById[secondId]);
        }

        [Fact]
        public async Task UpdateDashboardLayout_DashboardOfAnotherUser_ReturnsNotFound()
        {
            await _factory.SeedDatabaseAsync();

            var owner = await _factory.NewUserClient("layoutowner");
            var tracker = await CreateCapableTracker(owner, "Weight");
            var dashboardId = await CreateDashboard(owner);
            var itemId = await AddLineItem(owner, dashboardId, tracker);

            var stranger = await _factory.NewUserClient("layoutstranger");
            var layoutResponse = await stranger.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Items = [new DashboardLayoutItemDto { ItemId = itemId, X = 0, Y = 0, W = 4, H = 4 }]
            });

            Assert.Equal(HttpStatusCode.NotFound, layoutResponse.StatusCode);
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

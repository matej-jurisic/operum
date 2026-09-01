using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Dashboard;
using Operum.Model.DTOs.Dashboard.Requests;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Queries;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Views.Requests;
using Operum.Model.DTOs.Widgets.Requests;
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
        private static CreateAndPlaceWidgetSourceDto LineSource(CapableTracker tracker, string? xFieldId = null) => new()
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
        private static CreateAndPlaceWidgetSourceDto BarSource(CapableTracker tracker) => new()
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

        // Defines a new "Raw Values" line chart Widget and places it on the board in one
        // call, and hands back the placement's item id.
        private static async Task<string> AddLineItem(HttpClient client, string dashboardId, CapableTracker tracker)
        {
            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources = [LineSource(tracker)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
            return (await Data(addResponse)).GetProperty("id").GetString()!;
        }

        // The sources of an item as the board reports them: an edit has to name every source
        // id, so this is where a test gets them from.
        private static async Task<JsonElement> ItemSources(HttpClient client, string dashboardId, string itemId)
        {
            var dashboard = await Data(await client.GetAsync($"dashboard/{dashboardId}"));
            var item = dashboard.GetProperty("items").EnumerateArray()
                .Single(i => i.GetProperty("id").GetString() == itemId);
            return item.GetProperty("sources");
        }

        private static async Task<string> SingleSourceId(HttpClient client, string dashboardId, string itemId)
            => (await ItemSources(client, dashboardId, itemId))[0].GetProperty("id").GetString()!;

        // Creates a "Raw Values" line chart Widget in the Library and hands back its full
        // definition (including WidgetSource ids), so a test can place it on a board the
        // way the widget picker does.
        private static async Task<JsonElement> CreateWidget(HttpClient client, CapableTracker tracker, string? name = null)
        {
            var response = await client.PostAsJsonAsync("widgets", new CreateWidgetDto
            {
                Name = name,
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources =
                [
                    new CreateWidgetSourceRequestDto
                    {
                        TrackerId = tracker.Id,
                        Fields =
                        [
                            new CreateAnalyticFieldDto { FieldId = tracker.DayFieldId, Purpose = AnalyticPurposes.Xaxis },
                            new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Yaxis }
                        ]
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await Data(response);
        }

        [Fact]
        public async Task CreateAndPlaceWidget_SingleSource_ReturnsNativeChartTypeUnchanged()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
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
        public async Task CreateAndPlaceWidget_TwoSources_MergesIntoComposedChart()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var weight = await CreateCapableTracker(client, "Weight");
            var steps = await CreateCapableTracker(client, "Steps");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
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
        public async Task CreateAndPlaceWidget_SourcesWithDifferentXAxisTypes_WarnsWithoutRejecting()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var byDate = await CreateCapableTracker(client, "Weight");
            var byCategory = await CreateCapableTracker(client, "Steps");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
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
        public async Task CreateAndPlaceWidget_MultipleSourcesForANonCombinableType_ReturnsBadRequest()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var workouts = await CreateCapableTracker(client, "Workouts");
            var steps = await CreateCapableTracker(client, "Steps");
            var dashboardId = await CreateDashboard(client);

            CreateAndPlaceWidgetSourceDto CountSource(CapableTracker tracker) => new()
            {
                TrackerId = tracker.Id,
                AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.CategoryFieldId, Purpose = AnalyticPurposes.Value }]
            };

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.SingleValue,
                Code = AnalyticCodes.Count,
                Sources = [CountSource(workouts), CountSource(steps)]
            });

            Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
        }

        [Fact]
        public async Task CreateAndPlaceWidget_CodeDoesNotBelongToResultType_ReturnsBadRequest()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
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
                var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
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
                var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
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

        // Building a chart inline from a dashboard still creates a first-class Widget Library
        // entry behind the scenes -- there's no such thing as a dashboard-only chart
        // definition any more, the way there was no way to reach a tracker's own analytics
        // page in the old model.
        [Fact]
        public async Task CreateAndPlaceWidget_Source_CreatesAReusableLibraryWidgetInsteadOfATrackerAnalytic()
        {
            await _factory.SeedDatabaseAsync();
            // A fresh user, not the shared DefaultUsers.TestUserData: the widget-count
            // assertion below reads every widget this user owns, and the class shares one
            // database across every test authenticated as the default user.
            var client = await _factory.NewUserClient("inlinewidgetreuse");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources = [LineSource(tracker)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var results = await Widgets(client, dashboardId);
            Assert.Equal(1, results.GetArrayLength());
            Assert.Equal(AnalyticTypes.LineChart, Analytic(results[0]).GetProperty("resultType").GetString());

            // It shows up in the Widget Library, ready to be placed elsewhere -- there's no
            // separate tracker-owned analytics list any more for it to *not* show up in.
            var libraryWidgets = await Data(await client.GetAsync("widgets"));
            Assert.Equal(1, libraryWidgets.GetArrayLength());
        }

        [Fact]
        public async Task CreateAndPlaceWidget_SourceMissingARequiredPurpose_ReturnsBadRequest()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
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
        public async Task CreateAndPlaceWidget_SourceFieldFromAnotherTracker_ReturnsNotFound()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var tracker = await CreateCapableTracker(client, "Weight");
            var other = await CreateCapableTracker(client, "Steps");
            var dashboardId = await CreateDashboard(client);

            Assert.NotEqual(tracker.Id, other.Id);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
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

        // Placing a widget is a reference, never a copy: there is nothing left on the
        // placement itself once the definition it points at is gone.
        [Fact]
        public async Task PlaceWidget_ReferencesTheWidgetInsteadOfCopyingIt()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("widgetreference");

            var tracker = await CreateCapableTracker(client, "Weight");
            var widget = await CreateWidget(client, tracker);
            var widgetId = widget.GetProperty("id").GetString()!;
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/place-widget",
                new PlaceWidgetDto { WidgetId = widgetId });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var item = await Data(addResponse);
            Assert.Equal(AnalyticTypes.LineChart, item.GetProperty("resultType").GetString());
            Assert.Equal(AnalyticCodes.LineChart, item.GetProperty("code").GetString());
            Assert.Equal(1, item.GetProperty("sources").GetArrayLength());
            Assert.Equal(2, item.GetProperty("sources")[0].GetProperty("fields").GetArrayLength());

            // The whole point: deleting the widget from the Library takes the placement
            // down with it, because there was never a copy to fall back to.
            var deleteResponse = await client.DeleteAsync($"widgets/{widgetId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(0, widgets.GetArrayLength());
        }

        // The core promise of the new model: a placement without its own label reads
        // whatever the shared widget is named right now, everywhere it's placed.
        [Fact]
        public async Task PlaceWidget_RenamingTheWidgetInTheLibrary_UpdatesEveryPlacement()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("widgetrename");

            var tracker = await CreateCapableTracker(client, "Weight");
            var widget = await CreateWidget(client, tracker);
            var widgetId = widget.GetProperty("id").GetString()!;

            var dashboardA = await CreateDashboard(client);
            var dashboardB = await CreateDashboard(client);

            await client.PostAsJsonAsync($"dashboard/{dashboardA}/items/place-widget", new PlaceWidgetDto { WidgetId = widgetId });
            await client.PostAsJsonAsync($"dashboard/{dashboardB}/items/place-widget", new PlaceWidgetDto { WidgetId = widgetId });

            var renameResponse = await client.PutAsJsonAsync($"widgets/{widgetId}", new UpdateWidgetDto { Name = "Renamed everywhere" });
            Assert.Equal(HttpStatusCode.OK, renameResponse.StatusCode);

            var widgetsA = await Widgets(client, dashboardA);
            var widgetsB = await Widgets(client, dashboardB);
            Assert.Equal("Renamed everywhere", Analytic(widgetsA[0]).GetProperty("name").GetString());
            Assert.Equal("Renamed everywhere", Analytic(widgetsB[0]).GetProperty("name").GetString());
        }

        [Fact]
        public async Task PlaceWidget_ViewIdsNarrowTheWidget()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("widgetview");

            var tracker = await CreateCapableTracker(client, "Weight");
            var widget = await CreateWidget(client, tracker);
            var widgetId = widget.GetProperty("id").GetString()!;
            var sourceId = widget.GetProperty("sources")[0].GetProperty("id").GetString()!;
            var dashboardId = await CreateDashboard(client);

            // Nothing has a category of "Strength", so the view filters the single entry out.
            var view = await Data(await client.PostAsJsonAsync($"trackers/{tracker.Id}/views", new CreateViewDto
            {
                Name = "Strength only",
                Queries = [TestApi.FilterClause(tracker.CategoryFieldId, OperatorTypes.EqualsOperator, "Strength")]
            }));

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/place-widget",
                new PlaceWidgetDto
                {
                    WidgetId = widgetId,
                    SourceOverrides = [new PlaceWidgetSourceOverrideDto { WidgetSourceId = sourceId, ViewId = view.GetProperty("id").GetString()! }]
                });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(0, Analytic(widgets[0]).GetProperty("points").GetArrayLength());
        }

        [Fact]
        public async Task PlaceWidget_UnknownWidget_ReturnsNotFound()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("widgetmissing");

            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/place-widget",
                new PlaceWidgetDto { WidgetId = Guid.NewGuid().ToString() });

            Assert.Equal(HttpStatusCode.NotFound, addResponse.StatusCode);
        }

        // A widget is only ever placeable by its own owner today -- there is no sharing
        // model yet -- so a stranger's widget id simply doesn't resolve, the same as any
        // other id that names nothing of theirs.
        [Fact]
        public async Task PlaceWidget_WidgetOwnedByAnotherUser_ReturnsNotFound()
        {
            await _factory.SeedDatabaseAsync();

            var owner = await _factory.NewUserClient("widgetowner");
            var tracker = await CreateCapableTracker(owner, "Weight");
            var widget = await CreateWidget(owner, tracker);
            var widgetId = widget.GetProperty("id").GetString()!;

            var stranger = await _factory.NewUserClient("widgetstranger");
            var dashboardId = await CreateDashboard(stranger);

            var addResponse = await stranger.PostAsJsonAsync($"dashboard/{dashboardId}/items/place-widget",
                new PlaceWidgetDto { WidgetId = widgetId });

            Assert.Equal(HttpStatusCode.NotFound, addResponse.StatusCode);
        }

        // Removing a placement is not the same as deleting the widget: the shared
        // definition created alongside it survives in the Library, unlike the old model
        // where a dashboard item's definition only ever existed on that one item.
        [Fact]
        public async Task RemoveDashboardItem_LeavesTheSharedWidgetInPlace()
        {
            await _factory.SeedDatabaseAsync();
            // A fresh user, not the shared DefaultUsers.TestUserData: the widget-count
            // assertion below reads every widget this user owns, and the class shares one
            // database across every test authenticated as the default user.
            var client = await _factory.NewUserClient("removeitemwidget");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
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

            // The widget itself is untouched -- it just isn't placed anywhere right now.
            var libraryWidgets = await Data(await client.GetAsync("widgets"));
            Assert.Equal(1, libraryWidgets.GetArrayLength());
        }

        [Fact]
        public async Task CreateAndPlaceEntriesWidget_AddsAnEntriesWidget()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("entriescreate");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/entries",
                new CreateAndPlaceEntriesWidgetDto { TrackerId = tracker.Id });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(1, widgets.GetArrayLength());
            Assert.Equal(DashboardWidgetTypes.Entries, widgets[0].GetProperty("type").GetString());
            Assert.Equal(tracker.Id, widgets[0].GetProperty("entriesWidget").GetProperty("trackerId").GetString());
        }

        // The Entries equivalent of PlaceWidget_ReferencesTheWidgetInsteadOfCopyingIt.
        [Fact]
        public async Task PlaceEntriesWidget_ReferencesTheEntriesWidgetInsteadOfCopyingIt()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("entriesreference");

            var tracker = await CreateCapableTracker(client, "Weight");
            var entriesWidgetId = (await Data(await client.PostAsJsonAsync("widgets/entries",
                new CreateEntriesWidgetDto { TrackerId = tracker.Id }))).GetProperty("id").GetString()!;

            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/place-entries-widget",
                new PlaceEntriesWidgetDto { EntriesWidgetId = entriesWidgetId });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var deleteResponse = await client.DeleteAsync($"widgets/entries/{entriesWidgetId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(0, widgets.GetArrayLength());
        }

        // A widget the user has not placed yet still has to land somewhere sensible, which
        // means below what is already on the board rather than on top of it.
        [Fact]
        public async Task CreateAndPlaceWidget_PlacesTheWidgetUnderTheOnesAlreadyOnTheBoard()
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
        public async Task CreateAndPlaceWidget_PlacesTheWidgetOnBothGrids()
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
        public async Task AddQuickAddItem_ValidTracker_AddsAQuickAddWidget()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("quickadd");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/quick-add",
                new AddDashboardQuickAddItemDto { TrackerId = tracker.Id });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(1, widgets.GetArrayLength());
            Assert.Equal(DashboardWidgetTypes.QuickAdd, widgets[0].GetProperty("type").GetString());

            var config = JsonDocument.Parse(widgets[0].GetProperty("config").GetString()!).RootElement;
            Assert.Equal(tracker.Id, config.GetProperty("trackerId").GetString());

            // The tracker is resolved server-side so the card can render its button
            // without fetching the tracker itself once it mounts.
            var quickAddTracker = widgets[0].GetProperty("quickAddTracker");
            Assert.Equal(tracker.Id, quickAddTracker.GetProperty("id").GetString());
            Assert.Equal("Weight", quickAddTracker.GetProperty("name").GetString());
        }

        // A quick-add button is still a way to reach a tracker's entries, so it must not
        // become a way around tracker access.
        [Fact]
        public async Task AddQuickAddItem_TrackerNotAccessibleToUser_ReturnsForbidden()
        {
            await _factory.SeedDatabaseAsync();

            var owner = await _factory.NewUserClient("quickaddowner");
            var tracker = await CreateCapableTracker(owner, "Weight");

            var stranger = await _factory.NewUserClient("quickaddstranger");
            var dashboardId = await CreateDashboard(stranger);

            var addResponse = await stranger.PostAsJsonAsync($"dashboard/{dashboardId}/items/quick-add",
                new AddDashboardQuickAddItemDto { TrackerId = tracker.Id });

            Assert.Equal(HttpStatusCode.Forbidden, addResponse.StatusCode);
        }

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

        // Nothing has a category of "Strength", so a source (or a View widget) filtered by
        // this view keeps zero of the tracker's one seeded entry — the same shape
        // PlaceWidget_ViewIdsNarrowTheWidget uses, factored out for the View widget tests
        // below.
        private static async Task<string> CreateStrengthOnlyView(HttpClient client, CapableTracker tracker)
        {
            var view = await Data(await client.PostAsJsonAsync($"trackers/{tracker.Id}/views", new CreateViewDto
            {
                Name = "Strength only",
                Queries = [TestApi.FilterClause(tracker.CategoryFieldId, OperatorTypes.EqualsOperator, "Strength")]
            }));
            return view.GetProperty("id").GetString()!;
        }

        // Places a plain, unfiltered line chart on the board and returns its item id — the
        // "before" a View widget's own link picker acts on.
        private static async Task<string> PlaceLineChart(HttpClient client, string dashboardId, CapableTracker tracker, string? viewId = null)
        {
            var source = LineSource(tracker);
            source.ViewId = viewId;

            var response = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources = [source]
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await Data(response)).GetProperty("id").GetString()!;
        }

        private static JsonElement ChartFor(JsonElement widgets, string itemId)
            => widgets.EnumerateArray().Single(w => w.GetProperty("id").GetString() == itemId);

        private static int PointsOf(JsonElement widgets, string itemId)
            => Analytic(ChartFor(widgets, itemId)).GetProperty("points").GetArrayLength();

        // What an edit is for: the widget's own name, changed without disturbing the chart
        // it was built to draw.
        [Fact]
        public async Task UpdateDashboardItem_RenamesTheWidgetWithoutTouchingItsDefinition()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("itemrename");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var source = LineSource(tracker);
            source.Label = "Weight over time";
            var itemId = (await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources = [source]
            }))).GetProperty("id").GetString()!;

            var sourceId = await SingleSourceId(client, dashboardId, itemId);

            var updateResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{itemId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId, Label = "Trend" }]
            });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            // Renamed in the response the edit hands back, and still drawing the same chart.
            var updated = (await Data(updateResponse))[0];
            Assert.Equal("Trend", Analytic(updated).GetProperty("name").GetString());
            Assert.Equal(AnalyticTypes.LineChart, Analytic(updated).GetProperty("resultType").GetString());
            Assert.Equal(1, Analytic(updated).GetProperty("points").GetArrayLength());

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal("Trend", Analytic(widgets[0]).GetProperty("name").GetString());
        }

        // A name of nothing at all is not a blank title: the widget goes back to reading as
        // the definition it was built from, the same as one that was never named.
        [Fact]
        public async Task UpdateDashboardItem_BlankLabel_FallsBackToTheDefinitionsLabel()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("itemblanklabel");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var labelled = LineSource(tracker);
            labelled.Label = "Weight over time";
            var itemId = (await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources = [labelled]
            }))).GetProperty("id").GetString()!;

            var sourceId = await SingleSourceId(client, dashboardId, itemId);

            // The same chart with no label of its own, to read the fallback name off.
            var unnamedId = await AddLineItem(client, dashboardId, tracker);
            var defaultName = Analytic((await Widgets(client, dashboardId)).EnumerateArray()
                .Single(w => w.GetProperty("id").GetString() == unnamedId)).GetProperty("name").GetString();

            var updateResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{itemId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId, Label = "   " }]
            });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var renamed = (await Data(updateResponse)).EnumerateArray()
                .Single(w => w.GetProperty("id").GetString() == itemId);
            Assert.Equal(defaultName, Analytic(renamed).GetProperty("name").GetString());

            var storedSource = (await ItemSources(client, dashboardId, itemId))[0];
            Assert.Equal(JsonValueKind.Null, storedSource.GetProperty("label").ValueKind);
        }

        // The payload stands for the whole widget, so a combined chart that names only one of
        // its two sources is refused rather than half applied.
        [Fact]
        public async Task UpdateDashboardItem_SourcesNotNamedInFull_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("itempartial");

            var first = await CreateCapableTracker(client, "Weight");
            var second = await CreateCapableTracker(client, "Steps");
            var dashboardId = await CreateDashboard(client);

            var itemId = (await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources = [LineSource(first), LineSource(second)]
            }))).GetProperty("id").GetString()!;

            var sources = await ItemSources(client, dashboardId, itemId);
            Assert.Equal(2, sources.GetArrayLength());

            var updateResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{itemId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sources[0].GetProperty("id").GetString()!, Label = "Only one" }]
            });

            Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        }

        [Fact]
        public async Task UpdateDashboardItem_ViewOfAnotherTracker_ReturnsNotFound()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("itemviewmismatch");

            var tracker = await CreateCapableTracker(client, "Weight");
            var other = await CreateCapableTracker(client, "Steps");
            var otherViewId = await CreateStrengthOnlyView(client, other);
            var dashboardId = await CreateDashboard(client);

            var itemId = await AddLineItem(client, dashboardId, tracker);
            var sourceId = await SingleSourceId(client, dashboardId, itemId);

            var updateResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{itemId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId, ViewId = otherViewId }]
            });

            Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        }

        // A widget with no sources has neither a name nor a filter of its own to edit, so it
        // is not something this endpoint knows about at all.
        [Fact]
        public async Task UpdateDashboardItem_WidgetThatIsNotAnAnalytic_ReturnsNotFound()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("itemnotanalytic");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var quickAddId = (await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/quick-add",
                new AddDashboardQuickAddItemDto { TrackerId = tracker.Id }))).GetProperty("id").GetString()!;

            var updateResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{quickAddId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = Guid.NewGuid().ToString(), Label = "Nope" }]
            });

            Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        }

        [Fact]
        public async Task UpdateDashboardItem_DashboardOfAnotherUser_ReturnsNotFound()
        {
            await _factory.SeedDatabaseAsync();

            var owner = await _factory.NewUserClient("itemeditowner");
            var tracker = await CreateCapableTracker(owner, "Weight");
            var dashboardId = await CreateDashboard(owner);
            var itemId = await AddLineItem(owner, dashboardId, tracker);
            var sourceId = await SingleSourceId(owner, dashboardId, itemId);

            var stranger = await _factory.NewUserClient("itemeditstranger");

            var updateResponse = await stranger.PutAsJsonAsync($"dashboard/{dashboardId}/items/{itemId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId, Label = "Not yours" }]
            });

            Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        }

        // ----- View selector widget -----

        // The seeded entry is dated 2026-01-01, so a "later than mid-year" date clause keeps
        // nothing once it is selected, and everything again once it is cleared.
        private static SaveDashboardViewDto LateInYearSet() => new()
        {
            Name = "Late in year",
            Clauses =
            [
                new ClauseDto
                {
                    Kind = QueryKinds.Filter,
                    DataType = DataTypes.Date,
                    Operator = OperatorTypes.GreaterThan,
                    Value = "2026-06-01"
                }
            ]
        };

        [Fact]
        public async Task ViewSelector_SelectedFilterSet_NarrowsLinkedChartAndClearsWhenDeselected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("selectornarrows");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);

            var view = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/views", LateInYearSet()));
            var viewId = view.GetProperty("id").GetString()!;
            var queryId = view.GetProperty("clauses")[0].GetProperty("queryId").GetString()!;

            var selectorItem = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/view-selector",
                new SaveViewSelectorItemDto
                {
                    OptionIds = [viewId],
                    SelectedId = viewId,
                    Links =
                    [
                        new ViewSelectorLinkDto
                        {
                            ItemId = chartId,
                            TrackerId = tracker.Id,
                            FieldByQuery = new() { [queryId] = tracker.DayFieldId }
                        }
                    ]
                }));
            var selectorId = selectorItem.GetProperty("id").GetString()!;

            Assert.Equal(0, PointsOf(await Widgets(client, dashboardId), chartId));

            var cleared = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{selectorId}/view-selector-selection",
                new SetViewSelectorSelectionDto { SelectedId = null });
            Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
            Assert.Equal(1, PointsOf(await Data(cleared), chartId));
        }

        [Fact]
        public async Task AddViewSelector_ClauseMappedToFieldOfWrongType_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("selectorwrongtype");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);

            var view = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/views", LateInYearSet()));
            var viewId = view.GetProperty("id").GetString()!;
            var queryId = view.GetProperty("clauses")[0].GetProperty("queryId").GetString()!;

            var response = await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/view-selector",
                new SaveViewSelectorItemDto
                {
                    OptionIds = [viewId],
                    Links =
                    [
                        new ViewSelectorLinkDto
                        {
                            ItemId = chartId,
                            TrackerId = tracker.Id,
                            // Amount is a number field, but the clause is a date clause.
                            FieldByQuery = new() { [queryId] = tracker.AmountFieldId }
                        }
                    ]
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddDashboardView_MoreThanTheCap_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("viewcap");
            var dashboardId = await CreateDashboard(client);

            for (var i = 0; i < DataLimits.MaxDashboardViewCount; i++)
            {
                var ok = await client.PostAsJsonAsync($"dashboard/{dashboardId}/views",
                    new SaveDashboardViewDto
                    {
                        Name = $"Set {i}",
                        Clauses = [new ClauseDto { Kind = QueryKinds.Sort, DataType = DataTypes.Date }]
                    });
                Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            }

            var overflow = await client.PostAsJsonAsync($"dashboard/{dashboardId}/views",
                new SaveDashboardViewDto
                {
                    Name = "One too many",
                    Clauses = [new ClauseDto { Kind = QueryKinds.Sort, DataType = DataTypes.Date }]
                });
            Assert.Equal(HttpStatusCode.BadRequest, overflow.StatusCode);
        }
    }
}

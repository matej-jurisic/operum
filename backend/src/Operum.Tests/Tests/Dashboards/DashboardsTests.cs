using Microsoft.Extensions.DependencyInjection;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Dashboard;
using Operum.Model.Enums;
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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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

        // Deliberately has no analytic of its own: a dashboard item never reuses one.
        private static async Task<CapableTracker> CreateCapableTracker(HttpClient client, string name, string? color = null)
        {
            var tracker = await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = name, Color = color }));
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

        private static CreateAndPlaceWidgetSourceDto LineSource(CapableTracker tracker, string? xFieldId = null) => new()
        {
            TrackerId = tracker.Id,
            AnalyticFields =
            [
                new CreateAnalyticFieldDto { FieldId = xFieldId ?? tracker.DayFieldId, Purpose = AnalyticPurposes.Xaxis },
                new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Yaxis }
            ]
        };

        // Name is the only purpose this code requires.
        private static CreateAndPlaceWidgetSourceDto BarSource(CapableTracker tracker) => new()
        {
            TrackerId = tracker.Id,
            AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.CategoryFieldId, Purpose = AnalyticPurposes.Name }]
        };

        // The calendar source for a tracker: Day is the event date, Category its label.
        private static CreateAndPlaceWidgetSourceDto CalendarSource(CapableTracker tracker) => new()
        {
            TrackerId = tracker.Id,
            AnalyticFields =
            [
                new CreateAnalyticFieldDto { FieldId = tracker.DayFieldId, Purpose = AnalyticPurposes.When },
                new CreateAnalyticFieldDto { FieldId = tracker.CategoryFieldId, Purpose = AnalyticPurposes.What }
            ]
        };

        // Day is the shared match key, Amount the value that becomes this source's axis.
        private static CreateAndPlaceWidgetSourceDto CorrelationSource(CapableTracker tracker) => new()
        {
            TrackerId = tracker.Id,
            AnalyticFields =
            [
                new CreateAnalyticFieldDto { FieldId = tracker.DayFieldId, Purpose = AnalyticPurposes.Match },
                new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }
            ]
        };

        private static Task AddEntry(HttpClient client, string trackerId, string day, string amount) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/entries", new CreateEntryDto
            {
                FieldValues = new() { ["Day"] = day, ["Amount"] = amount, ["Category"] = "x" }
            });

        private static async Task<string> CreateDashboard(HttpClient client)
        {
            var dashboard = await Data(await client.PostAsJsonAsync("dashboard", new CreateDashboardDto { Name = "My board" }));
            return dashboard.GetProperty("id").GetString()!;
        }

        private static async Task<JsonElement> Widgets(HttpClient client, string dashboardId)
            => await Data(await client.GetAsync($"dashboard/{dashboardId}/widgets"));

        private static JsonElement Analytic(JsonElement widget) => widget.GetProperty("analytic");

        private static JsonElement Layout(JsonElement widget) => widget.GetProperty("layout");

        private static JsonElement MobileLayout(JsonElement widget) => widget.GetProperty("mobileLayout");

        private static async Task<string> AddLineItem(HttpClient client, string dashboardId, CapableTracker tracker)
        {
            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
                Sources = [LineSource(tracker)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
            return (await Data(addResponse)).GetProperty("id").GetString()!;
        }

        // An edit has to name every source id, so this is where a test reads them from.
        private static async Task<JsonElement> ItemSources(HttpClient client, string dashboardId, string itemId)
        {
            var dashboard = await Data(await client.GetAsync($"dashboard/{dashboardId}"));
            var item = dashboard.GetProperty("items").EnumerateArray()
                .Single(i => i.GetProperty("id").GetString() == itemId);
            return item.GetProperty("sources");
        }

        private static async Task<string> SingleSourceId(HttpClient client, string dashboardId, string itemId)
            => (await ItemSources(client, dashboardId, itemId))[0].GetProperty("id").GetString()!;

        private static async Task<JsonElement> CreateWidget(HttpClient client, CapableTracker tracker, string? name = null)
        {
            var response = await client.PostAsJsonAsync("widgets", new CreateWidgetDto
            {
                Name = name,
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
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
                Code = AnalyticCodes.Count,
                Grouping = AnalyticGroupings.Exact,
                Sources = [BarSource(tracker)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var results = await Widgets(client, dashboardId);

            Assert.Equal(1, results.GetArrayLength());
            Assert.Equal(AnalyticTypes.BarChart, Analytic(results[0]).GetProperty("resultType").GetString());
        }

        [Fact]
        public async Task CreateAndPlaceWidget_Goal_RendersProgressTowardTheTarget()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("goalprogress");

            var tracker = await CreateCapableTracker(client, "Savings");
            // CreateCapableTracker already logged Amount 5; another 25 brings the sum to 30.
            await AddEntry(client, tracker.Id, "2026-01-02", "25");

            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.Goal,
                Code = AnalyticCodes.Sum,
                GoalTarget = "60",
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }]
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var analytic = Analytic((await Widgets(client, dashboardId))[0]);
            Assert.Equal(AnalyticTypes.Goal, analytic.GetProperty("resultType").GetString());
            Assert.Equal("60", analytic.GetProperty("target").GetString());
            Assert.Equal(0.5, analytic.GetProperty("progress").GetDouble(), 3);
            Assert.Equal(GoalDirections.HigherIsBetter, analytic.GetProperty("direction").GetString());
        }

        [Fact]
        public async Task CreateAndPlaceWidget_GoalLowerIsBetter_InvertsProgressTowardTheCap()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("goallowerisbetter");

            var tracker = await CreateCapableTracker(client, "Budget");
            // CreateCapableTracker already logged Amount 5; another 25 brings the sum to 30.
            await AddEntry(client, tracker.Id, "2026-01-02", "25");

            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.Goal,
                Code = AnalyticCodes.Sum,
                GoalTarget = "60",
                GoalDirection = GoalDirections.LowerIsBetter,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }]
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            // LowerIsBetter's inverted ratio (target / value) reads well past 1 for the same
            // numbers a HigherIsBetter goal would call "half done".
            var analytic = Analytic((await Widgets(client, dashboardId))[0]);
            Assert.Equal(GoalDirections.LowerIsBetter, analytic.GetProperty("direction").GetString());
            Assert.Equal(2.0, analytic.GetProperty("progress").GetDouble(), 3);
        }

        [Fact]
        public async Task CreateAndPlaceWidget_GoalLowerIsBetter_OverTheCapReadsAsUnderOne()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("goallowerisbetterover");

            var tracker = await CreateCapableTracker(client, "Budget");
            // CreateCapableTracker already logged Amount 5; another 25 brings the sum to 30.
            await AddEntry(client, tracker.Id, "2026-01-02", "25");

            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.Goal,
                Code = AnalyticCodes.Sum,
                GoalTarget = "10",
                GoalDirection = GoalDirections.LowerIsBetter,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }]
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            // Over budget: the ratio drops under 1, the same as HigherIsBetter falling under 1
            // for not having reached its target yet.
            var analytic = Analytic((await Widgets(client, dashboardId))[0]);
            var progress = analytic.GetProperty("progress").GetDouble();
            Assert.True(progress < 1, $"Expected progress under 1 once over the cap, got {progress}");
        }

        [Fact]
        public async Task Goal_ConditionalTarget_ReplacesTheDefaultWhenAFollowedFilterMatches()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("goalconditional");

            var tracker = await CreateCapableTracker(client, "Focus");
            var dashboardId = await CreateDashboard(client);

            var goalItem = await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.Goal,
                Code = AnalyticCodes.Sum,
                GoalTarget = "60",
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }]
                    }
                ]
            }));
            var goalId = goalItem.GetProperty("id").GetString()!;
            var sourceId = goalItem.GetProperty("sources")[0].GetProperty("id").GetString()!;

            var filter = await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/filter", new SaveFilterItemDto
            {
                Clauses = AmountOverClauses(),
                Links =
                [
                    new WidgetLinkDto
                    {
                        ItemId = goalId,
                        TrackerId = tracker.Id,
                        FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
                    }
                ]
            }));
            var filterId = filter.GetProperty("id").GetString()!;
            var slotId = await FilterSlotId(client, dashboardId, filterId);

            // When that filter is set to "1", aim for 999 instead of the default 60.
            var update = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{goalId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId }],
                GoalConditionalTargets =
                [
                    new GoalConditionalTargetDto { Conditions = new() { [slotId] = "1" }, Target = "999" }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            // Filter unset: the default target.
            Assert.Equal("60", Analytic(ChartFor(await Widgets(client, dashboardId), goalId)).GetProperty("target").GetString());

            // Filter set to the matching value: the conditional target wins.
            var narrowed = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = "1" } });
            Assert.Equal("999", Analytic(ChartFor(await Data(narrowed), goalId)).GetProperty("target").GetString());

            // A different value: no row matches, back to the default.
            var other = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = "2" } });
            Assert.Equal("60", Analytic(ChartFor(await Data(other), goalId)).GetProperty("target").GetString());
        }

        [Fact]
        public async Task Goal_ConditionalTarget_OnADateClause_MatchesATokenAgainstTheConcreteDate()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("goalconditionaldate");

            var tracker = await CreateCapableTracker(client, "Focus");
            var dashboardId = await CreateDashboard(client);

            var goalItem = await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.Goal,
                Code = AnalyticCodes.Sum,
                GoalTarget = "60",
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }]
                    }
                ]
            }));
            var goalId = goalItem.GetProperty("id").GetString()!;
            var sourceId = goalItem.GetProperty("sources")[0].GetProperty("id").GetString()!;

            // "on or before" keeps the seeded 2026-01-01 entry in scope for every value the
            // test sets, so the goal always calculates and only its target changes.
            var filter = await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/filter", new SaveFilterItemDto
            {
                Clauses =
                [
                    new ClauseDto
                    {
                        Kind = QueryKinds.Filter,
                        DataType = DataTypes.Date,
                        Operator = OperatorTypes.LessThanOrEqual
                    }
                ],
                Links =
                [
                    new WidgetLinkDto
                    {
                        ItemId = goalId,
                        TrackerId = tracker.Id,
                        FieldByQuery = new() { ["0"] = tracker.DayFieldId }
                    }
                ]
            }));
            var filterId = filter.GetProperty("id").GetString()!;
            var slotId = await FilterSlotId(client, dashboardId, filterId);

            // The row is keyed on the "start of month" token.
            var update = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{goalId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId }],
                GoalConditionalTargets =
                [
                    new GoalConditionalTargetDto
                    {
                        Conditions = new() { [slotId] = DynamicDateTokens.StartOfMonth },
                        Target = "999"
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            // Filter set to the concrete first-of-month date: the token still matches it.
            var now = DateTime.UtcNow;
            var firstOfMonth = new DateTime(now.Year, now.Month, 1).ToString("yyyy-MM-dd");
            var onFirst = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = firstOfMonth } });
            Assert.Equal("999", Analytic(ChartFor(await Data(onFirst), goalId)).GetProperty("target").GetString());

            // A different day: no row matches, back to the default.
            var secondOfMonth = new DateTime(now.Year, now.Month, 2).ToString("yyyy-MM-dd");
            var onSecond = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = secondOfMonth } });
            Assert.Equal("60", Analytic(ChartFor(await Data(onSecond), goalId)).GetProperty("target").GetString());
        }

        [Fact]
        public async Task SingleValue_FollowingADateRangeFilter_ComputesTheTrend()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("trendfollow");

            // Seeded Day 2026-01-01, Amount 5 lands in the previous window below; Jan 10/12
            // land in the current window (10 + 20 = 30).
            var tracker = await CreateCapableTracker(client, "Spending");
            await AddEntry(client, tracker.Id, "2026-01-10", "10");
            await AddEntry(client, tracker.Id, "2026-01-12", "20");

            var dashboardId = await CreateDashboard(client);

            var sumItem = await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.SingleValue,
                Code = AnalyticCodes.Sum,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }]
                    }
                ]
            }));
            var sumId = sumItem.GetProperty("id").GetString()!;

            var filter = await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/filter", new SaveFilterItemDto
            {
                Clauses =
                [
                    new ClauseDto { Kind = QueryKinds.Filter, DataType = DataTypes.Date, Operator = OperatorTypes.GreaterThanOrEqual },
                    new ClauseDto { Kind = QueryKinds.Filter, DataType = DataTypes.Date, Operator = OperatorTypes.LessThanOrEqual }
                ],
                Links =
                [
                    new WidgetLinkDto
                    {
                        ItemId = sumId,
                        TrackerId = tracker.Id,
                        FieldByQuery = new() { ["0"] = tracker.DayFieldId, ["1"] = tracker.DayFieldId }
                    }
                ]
            }));
            var filterId = filter.GetProperty("id").GetString()!;
            var lowerSlotId = await FilterSlotId(client, dashboardId, filterId, 0);
            var upperSlotId = await FilterSlotId(client, dashboardId, filterId, 1);

            // Current window Jan 8-15 (10 + 20 = 30); the equal-length previous window is
            // Jan 1 up to (not including) Jan 8, which only the seeded Jan 1 entry (5) falls in.
            var setValues = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [lowerSlotId] = "2026-01-08", [upperSlotId] = "2026-01-15" } });
            Assert.Equal(HttpStatusCode.OK, setValues.StatusCode);

            var analytic = Analytic(ChartFor(await Data(setValues), sumId));
            Assert.Equal("30.00", analytic.GetProperty("value").GetString());
            Assert.Equal("5.00", analytic.GetProperty("trend").GetProperty("previousValue").GetString());

            var points = analytic.GetProperty("trend").GetProperty("points").EnumerateArray().ToList();
            Assert.Equal(30, points.Sum(p => p.GetProperty("y").GetDouble()));
        }

        [Fact]
        public async Task SingleValue_NotFollowingADateFilter_HasNoTrend()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("trendnofilter");

            var tracker = await CreateCapableTracker(client, "Spending");
            var dashboardId = await CreateDashboard(client);

            var sumItem = await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.SingleValue,
                Code = AnalyticCodes.Sum,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }]
                    }
                ]
            }));
            var sumId = sumItem.GetProperty("id").GetString()!;

            var analytic = Analytic(ChartFor(await Widgets(client, dashboardId), sumId));
            Assert.False(analytic.TryGetProperty("trend", out var trend) && trend.ValueKind != JsonValueKind.Null);
        }

        [Fact]
        public async Task Goal_ConditionalTarget_ForAnUnfollowedClause_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("goalconditionalreject");

            var tracker = await CreateCapableTracker(client, "Focus");
            var dashboardId = await CreateDashboard(client);

            var goalItem = await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.Goal,
                Code = AnalyticCodes.Sum,
                GoalTarget = "60",
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }]
                    }
                ]
            }));
            var goalId = goalItem.GetProperty("id").GetString()!;
            var sourceId = goalItem.GetProperty("sources")[0].GetProperty("id").GetString()!;

            var rejected = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{goalId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId }],
                GoalConditionalTargets =
                [
                    new GoalConditionalTargetDto { Conditions = new() { ["not-a-followed-clause"] = "1" }, Target = "999" }
                ]
            });
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
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
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
                Sources = [LineSource(weight), LineSource(steps)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var results = await Widgets(client, dashboardId);

            Assert.Equal(1, results.GetArrayLength());
            var combined = Analytic(results[0]);
            Assert.Equal(AnalyticTypes.Composed, combined.GetProperty("resultType").GetString());
            Assert.Equal(2, combined.GetProperty("series").GetArrayLength());
            Assert.Equal(0, combined.GetProperty("warnings").GetArrayLength());
        }

        [Fact]
        public async Task CreateAndPlaceWidget_TwoCalendarSources_MergesEventsTaggedWithTracker()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            var workouts = await CreateCapableTracker(client, "Workouts", "blue");
            var meals = await CreateCapableTracker(client, "Meals", "green");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.Calendar,
                Code = AnalyticCodes.Calendar,
                Sources = [CalendarSource(workouts), CalendarSource(meals)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var results = await Widgets(client, dashboardId);

            Assert.Equal(1, results.GetArrayLength());
            var merged = Analytic(results[0]);
            Assert.Equal(AnalyticTypes.Calendar, merged.GetProperty("resultType").GetString());

            var points = merged.GetProperty("points").EnumerateArray().ToList();
            Assert.Equal(2, points.Count);

            var trackerNames = points.Select(p => p.GetProperty("trackerName").GetString()).ToHashSet();
            Assert.Equal(new HashSet<string?> { "Workouts", "Meals" }, trackerNames);

            var colors = points.Select(p => p.GetProperty("color").GetString()).ToHashSet();
            Assert.Equal(new HashSet<string?> { "blue", "green" }, colors);
        }

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
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
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
                Code = AnalyticCodes.DonutChart,
                Sources = [LineSource(tracker)]
            });

            Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
        }

        [Fact]
        public async Task CreateAndPlaceWidget_TwoCorrelationSources_PairsValuesOnTheMatchKey()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("correlationpairs");

            var weight = await CreateCapableTracker(client, "Weight");
            var sleep = await CreateCapableTracker(client, "Sleep");
            await AddEntry(client, weight.Id, "2026-01-02", "6");
            await AddEntry(client, weight.Id, "2026-01-03", "7");
            await AddEntry(client, sleep.Id, "2026-01-02", "8");
            await AddEntry(client, sleep.Id, "2026-01-09", "9");

            var dashboardId = await CreateDashboard(client);
            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.ScatterChart,
                Code = AnalyticCodes.CorrelationScatter,
                Sources = [CorrelationSource(weight), CorrelationSource(sleep)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var analytic = Analytic((await Widgets(client, dashboardId))[0]);
            Assert.Equal(AnalyticTypes.ScatterChart, analytic.GetProperty("resultType").GetString());

            var points = analytic.GetProperty("points").EnumerateArray()
                .Select(p => (X: p.GetProperty("x").GetDouble(), Y: p.GetProperty("y").GetDouble()))
                .OrderBy(p => p.X)
                .ToList();

            Assert.Equal(2, points.Count);
            Assert.Equal(5, points[0].X);
            Assert.Equal(5, points[0].Y);
            Assert.Equal(6, points[1].X);
            Assert.Equal(8, points[1].Y);

            Assert.Equal("Weight: Amount", analytic.GetProperty("xField").GetProperty("name").GetString());
            Assert.Equal("Sleep: Amount", analytic.GetProperty("yField").GetProperty("name").GetString());
        }

        [Fact]
        public async Task CreateAndPlaceWidget_CorrelationWithoutExactlyTwoSources_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("correlationcount");

            var weight = await CreateCapableTracker(client, "Weight");
            var sleep = await CreateCapableTracker(client, "Sleep");
            var mood = await CreateCapableTracker(client, "Mood");
            var dashboardId = await CreateDashboard(client);

            var one = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.ScatterChart,
                Code = AnalyticCodes.CorrelationScatter,
                Sources = [CorrelationSource(weight)]
            });
            Assert.Equal(HttpStatusCode.BadRequest, one.StatusCode);

            var three = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.ScatterChart,
                Code = AnalyticCodes.CorrelationScatter,
                Sources = [CorrelationSource(weight), CorrelationSource(sleep), CorrelationSource(mood)]
            });
            Assert.Equal(HttpStatusCode.BadRequest, three.StatusCode);
        }

        // Regression: two items sharing a Tracker under NoTracking used to materialize two CLR
        // instances, and Remove() threw on the second same-key instance. See DashboardService.GetUserDashboard.
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
                    Code = AnalyticCodes.Count,
                    Grouping = AnalyticGroupings.Exact,
                    Sources = [BarSource(tracker)]
                });
                Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
            }

            var deleteResponse = await client.DeleteAsync($"dashboard/{dashboardId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        }

        // Same scenario as above, via the single-item removal endpoint instead.
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
                    Code = AnalyticCodes.Count,
                    Grouping = AnalyticGroupings.Exact,
                    Sources = [BarSource(tracker)]
                });
                Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
                firstItemId ??= (await Data(addResponse)).GetProperty("id").GetString();
            }

            var removeResponse = await client.DeleteAsync($"dashboard/{dashboardId}/items/{firstItemId}");
            Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        }

        // Building a chart inline from a dashboard still creates a first-class Widget Library
        // entry; there's no such thing as a dashboard-only chart definition any more.
        [Fact]
        public async Task CreateAndPlaceWidget_Source_CreatesAReusableLibraryWidgetInsteadOfATrackerAnalytic()
        {
            await _factory.SeedDatabaseAsync();
            // Fresh user: the widget-count assertion below reads every widget this user owns,
            // and the class shares one database across every test on the default user.
            var client = await _factory.NewUserClient("inlinewidgetreuse");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
                Sources = [LineSource(tracker)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var results = await Widgets(client, dashboardId);
            Assert.Equal(1, results.GetArrayLength());
            Assert.Equal(AnalyticTypes.LineChart, Analytic(results[0]).GetProperty("resultType").GetString());

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
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.DayFieldId, Purpose = AnalyticPurposes.Xaxis }]
                    }
                ]
            });

            Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
        }

        // A dashboard spans trackers, so a source must not reach a field on another tracker.
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
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
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

        // Placing a widget is a reference, never a copy: nothing is left on the placement
        // itself once the definition it points at is gone.
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
            Assert.Equal(AnalyticCodes.RawValues, item.GetProperty("code").GetString());
            Assert.Equal(AnalyticGroupings.None, item.GetProperty("grouping").GetString());
            Assert.Equal(1, item.GetProperty("sources").GetArrayLength());
            Assert.Equal(2, item.GetProperty("sources")[0].GetProperty("fields").GetArrayLength());

            var deleteResponse = await client.DeleteAsync($"widgets/{widgetId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(0, widgets.GetArrayLength());
        }

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

        // Regression: charts used to coerce a missing y/value to 0, dragging sums and averages
        // down. Such entries are now left out of the chart entirely.
        [Fact]
        public async Task LineChart_EntryWithNoYValue_IsExcludedFromPoints()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("chartnully");

            var tracker = await CreateCapableTracker(client, "Weight");
            await client.PostAsJsonAsync($"trackers/{tracker.Id}/entries", new CreateEntryDto
            {
                FieldValues = new() { ["Day"] = "2026-01-02", ["Category"] = "Cardio" }
            });

            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);

            Assert.Equal(1, PointsOf(await Widgets(client, dashboardId), chartId));
        }

        [Fact]
        public async Task AverageBarChart_EntryWithNoValue_IsNotCountedAsZero()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("chartnullavg");

            var tracker = await CreateCapableTracker(client, "Weight");
            // A coerced 0 here would drag the category's average down from 5 to 2.5.
            await client.PostAsJsonAsync($"trackers/{tracker.Id}/entries", new CreateEntryDto
            {
                FieldValues = new() { ["Day"] = "2026-01-02", ["Category"] = "Cardio" }
            });

            var dashboardId = await CreateDashboard(client);
            var response = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.BarChart,
                Code = AnalyticCodes.Average,
                Grouping = AnalyticGroupings.Exact,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields =
                        [
                            new CreateAnalyticFieldDto { FieldId = tracker.CategoryFieldId, Purpose = AnalyticPurposes.Name },
                            new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }
                        ]
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var chartId = (await Data(response)).GetProperty("id").GetString()!;

            var points = Analytic(ChartFor(await Widgets(client, dashboardId), chartId)).GetProperty("points");
            Assert.Equal(1, points.GetArrayLength());
            Assert.Equal(5, points[0].GetProperty("value").GetDouble());
        }

        // Date-bucketed bar charts group the Name field's date into a period and order the
        // bars chronologically, the same as the line chart.
        [Fact]
        public async Task MonthlyBarChart_BucketsTheNameDateAndSumsEachPeriod()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("chartmonthlybar");

            // Seeded out of order (March before the later January entry) to prove the sort.
            var tracker = await CreateCapableTracker(client, "Weight");
            await client.PostAsJsonAsync($"trackers/{tracker.Id}/entries", new CreateEntryDto
            {
                FieldValues = new() { ["Day"] = "2026-03-10", ["Amount"] = "7", ["Category"] = "Cardio" }
            });
            await client.PostAsJsonAsync($"trackers/{tracker.Id}/entries", new CreateEntryDto
            {
                FieldValues = new() { ["Day"] = "2026-01-20", ["Amount"] = "3", ["Category"] = "Cardio" }
            });

            var dashboardId = await CreateDashboard(client);
            var response = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.BarChart,
                Code = AnalyticCodes.Sum,
                Grouping = AnalyticGroupings.Monthly,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields =
                        [
                            new CreateAnalyticFieldDto { FieldId = tracker.DayFieldId, Purpose = AnalyticPurposes.Name },
                            new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }
                        ]
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var chartId = (await Data(response)).GetProperty("id").GetString()!;

            var points = Analytic(ChartFor(await Widgets(client, dashboardId), chartId)).GetProperty("points");
            Assert.Equal(2, points.GetArrayLength());
            Assert.Equal("2026-01", points[0].GetProperty("name").GetString());
            Assert.Equal(8, points[0].GetProperty("value").GetDouble());
            Assert.Equal("2026-03", points[1].GetProperty("name").GetString());
            Assert.Equal(7, points[1].GetProperty("value").GetDouble());
        }

        // Grouping and aggregation are independent: a weekly chart can average instead of summing.
        [Fact]
        public async Task WeeklyAverageLineChart_AveragesEachWeeksValues()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("weeklyavgline");

            var tracker = await CreateCapableTracker(client, "Weight");
            await AddEntry(client, tracker.Id, "2026-01-02", "15");
            await AddEntry(client, tracker.Id, "2026-01-08", "20");

            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceGroupedLineChart(
                client, dashboardId, tracker, AnalyticGroupings.Weekly, AnalyticCodes.Average);

            var points = Analytic(ChartFor(await Widgets(client, dashboardId), chartId)).GetProperty("points");
            Assert.Equal(2, points.GetArrayLength());
            Assert.Equal("2025-12-29", points[0].GetProperty("x").GetString());
            Assert.Equal(10, points[0].GetProperty("y").GetDouble());
            Assert.Equal("2026-01-05", points[1].GetProperty("x").GetString());
            Assert.Equal(20, points[1].GetProperty("y").GetDouble());
        }

        // Count reads no value field: each point is the number of entries in that bucket.
        [Fact]
        public async Task DailyCountLineChart_CountsEntriesPerDay_WithNoValueField()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("dailycountline");

            var tracker = await CreateCapableTracker(client, "Weight");
            await AddEntry(client, tracker.Id, "2026-01-01", "99");
            await AddEntry(client, tracker.Id, "2026-01-03", "1");

            var dashboardId = await CreateDashboard(client);
            var response = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.Count,
                Grouping = AnalyticGroupings.Daily,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.DayFieldId, Purpose = AnalyticPurposes.Xaxis }]
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var chartId = (await Data(response)).GetProperty("id").GetString()!;

            var points = Analytic(ChartFor(await Widgets(client, dashboardId), chartId)).GetProperty("points");
            Assert.Equal(2, points.GetArrayLength());
            Assert.Equal("2026-01-01", points[0].GetProperty("x").GetString());
            Assert.Equal(2, points[0].GetProperty("y").GetDouble());
            Assert.Equal("2026-01-03", points[1].GetProperty("x").GetString());
            Assert.Equal(1, points[1].GetProperty("y").GetDouble());
        }

        // Cumulative Sum composes with a date bucket: a running total of each month's total.
        [Fact]
        public async Task MonthlyCumulativeSumLineChart_RunningTotalOfMonthlyTotals()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("monthlycumline");

            var tracker = await CreateCapableTracker(client, "Weight");
            await AddEntry(client, tracker.Id, "2026-01-20", "3");
            await AddEntry(client, tracker.Id, "2026-03-10", "7");

            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceGroupedLineChart(
                client, dashboardId, tracker, AnalyticGroupings.Monthly, AnalyticCodes.CumulativeSum);

            var points = Analytic(ChartFor(await Widgets(client, dashboardId), chartId)).GetProperty("points");
            Assert.Equal(2, points.GetArrayLength());
            Assert.Equal("2026-01", points[0].GetProperty("x").GetString());
            Assert.Equal(8, points[0].GetProperty("y").GetDouble());
            Assert.Equal("2026-03", points[1].GetProperty("x").GetString());
            Assert.Equal(15, points[1].GetProperty("y").GetDouble());
        }

        [Fact]
        public async Task MinPerCategoryBarChart_TakesTheLowestValueInEachCategory()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("minpercatbar");

            var tracker = await CreateCapableTracker(client, "Weight");
            await client.PostAsJsonAsync($"trackers/{tracker.Id}/entries", new CreateEntryDto
            {
                FieldValues = new() { ["Day"] = "2026-01-02", ["Amount"] = "2", ["Category"] = "Cardio" }
            });
            await client.PostAsJsonAsync($"trackers/{tracker.Id}/entries", new CreateEntryDto
            {
                FieldValues = new() { ["Day"] = "2026-01-03", ["Amount"] = "9", ["Category"] = "Strength" }
            });

            var dashboardId = await CreateDashboard(client);
            var response = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.BarChart,
                Code = AnalyticCodes.Min,
                Grouping = AnalyticGroupings.Exact,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields =
                        [
                            new CreateAnalyticFieldDto { FieldId = tracker.CategoryFieldId, Purpose = AnalyticPurposes.Name },
                            new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }
                        ]
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var chartId = (await Data(response)).GetProperty("id").GetString()!;

            var points = Analytic(ChartFor(await Widgets(client, dashboardId), chartId)).GetProperty("points")
                .EnumerateArray()
                .ToDictionary(p => p.GetProperty("name").GetString()!, p => p.GetProperty("value").GetDouble());
            Assert.Equal(2, points.Count);
            Assert.Equal(2, points["Cardio"]);
            Assert.Equal(9, points["Strength"]);
        }

        [Fact]
        public async Task RawValuesBarChart_PlotsOneBarPerEntry_WithNoAggregation()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("rawvaluesbar");

            var tracker = await CreateCapableTracker(client, "Weight");
            await client.PostAsJsonAsync($"trackers/{tracker.Id}/entries", new CreateEntryDto
            {
                FieldValues = new() { ["Day"] = "2026-01-02", ["Amount"] = "2", ["Category"] = "Cardio" }
            });
            await client.PostAsJsonAsync($"trackers/{tracker.Id}/entries", new CreateEntryDto
            {
                FieldValues = new() { ["Day"] = "2026-01-03", ["Amount"] = "9", ["Category"] = "Strength" }
            });

            var dashboardId = await CreateDashboard(client);
            var response = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.BarChart,
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields =
                        [
                            new CreateAnalyticFieldDto { FieldId = tracker.CategoryFieldId, Purpose = AnalyticPurposes.Name },
                            new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }
                        ]
                    }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var chartId = (await Data(response)).GetProperty("id").GetString()!;

            var points = Analytic(ChartFor(await Widgets(client, dashboardId), chartId)).GetProperty("points");
            Assert.Equal(3, points.GetArrayLength());
            var values = points.EnumerateArray()
                .Select(p => (p.GetProperty("name").GetString()!, p.GetProperty("value").GetDouble()))
                .ToList();
            Assert.Equal(2, values.Count(v => v.Item1 == "Cardio"));
            Assert.Contains(("Cardio", 5d), values);
            Assert.Contains(("Cardio", 2d), values);
            Assert.Contains(("Strength", 9d), values);
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

        // No sharing model yet: a stranger's widget id simply doesn't resolve.
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

        // Removing a placement doesn't delete the shared widget; it stays in the Library.
        [Fact]
        public async Task RemoveDashboardItem_LeavesTheSharedWidgetInPlace()
        {
            await _factory.SeedDatabaseAsync();
            // Fresh user: the widget-count assertion below reads every widget this user owns,
            // and the class shares one database across every test on the default user.
            var client = await _factory.NewUserClient("removeitemwidget");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
                Sources = [LineSource(tracker)]
            });
            var itemId = (await Data(addResponse)).GetProperty("id").GetString()!;

            var removeResponse = await client.DeleteAsync($"dashboard/{dashboardId}/items/{itemId}");
            Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);

            var fetched = await Data(await client.GetAsync($"dashboard/{dashboardId}"));
            Assert.Equal(0, fetched.GetProperty("items").GetArrayLength());

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

        // Entries-widget equivalent of PlaceWidget_ReferencesTheWidgetInsteadOfCopyingIt.
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

        // An item removed in another tab must not fail the save for everything else.
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

        // The narrow grid has no room beside anything, so a new widget takes its full width
        // and stacks under what is already there.
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

        // Dragging a widget on a phone must not move it on the desktop board, and vice versa.
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

        // A placement is clamped to the grid it was made on, not the widest one there is.
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

        // Unlike a placement, an unknown variant can't be clamped: there's no telling which
        // grid the numbers beside it belong to.
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

        // Only the wide grid decides the board's reading order, or it would flip back and
        // forth with whichever screen was used last.
        [Fact]
        public async Task UpdateDashboardLayout_MobileVariant_DoesNotRewriteTheReadingOrder()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("mobileorder");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var firstId = await AddLineItem(client, dashboardId, tracker);
            var secondId = await AddLineItem(client, dashboardId, tracker);

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

        private async Task<(string ItemId, string[] TabIds)> AddTabsContainer(HttpClient client, string dashboardId)
        {
            var add = await client.PostAsync($"dashboard/{dashboardId}/items/tabs-container", null);
            Assert.Equal(HttpStatusCode.OK, add.StatusCode);
            var itemId = (await Data(add)).GetProperty("id").GetString()!;
            return (itemId, await TabIds(client, dashboardId, itemId));
        }

        private static async Task<string[]> TabIds(HttpClient client, string dashboardId, string itemId)
        {
            var widgets = await Widgets(client, dashboardId);
            var item = widgets.EnumerateArray().Single(w => w.GetProperty("id").GetString() == itemId);
            var config = JsonDocument.Parse(item.GetProperty("config").GetString()!).RootElement;
            return [.. config.GetProperty("tabs").EnumerateArray().Select(t => t.GetProperty("id").GetString()!)];
        }

        private static JsonElement WidgetById(JsonElement widgets, string id) =>
            widgets.EnumerateArray().Single(w => w.GetProperty("id").GetString() == id);

        [Fact]
        public async Task AddTabsContainerItem_SeedsExactlyOneTab()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("tabsadd");
            var dashboardId = await CreateDashboard(client);

            var (itemId, tabIds) = await AddTabsContainer(client, dashboardId);

            Assert.Single(tabIds);
            var widget = WidgetById(await Widgets(client, dashboardId), itemId);
            Assert.Equal(DashboardWidgetTypes.TabsContainer, widget.GetProperty("type").GetString());
        }

        [Fact]
        public async Task SaveTabsContainer_RenamesKeptTabsAndCreatesNewOnes()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("tabssave");
            var dashboardId = await CreateDashboard(client);
            var (itemId, tabIds) = await AddTabsContainer(client, dashboardId);

            var save = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{itemId}/tabs-container", new SaveTabsContainerDto
            {
                Title = "Overview",
                Tabs =
                [
                    new SaveTabDto { Id = tabIds[0], Name = "Renamed" },
                    new SaveTabDto { Name = "Second" }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, save.StatusCode);

            var config = JsonDocument.Parse(
                WidgetById(await Widgets(client, dashboardId), itemId).GetProperty("config").GetString()!).RootElement;

            Assert.Equal("Overview", config.GetProperty("title").GetString());
            var tabs = config.GetProperty("tabs").EnumerateArray().ToList();
            Assert.Equal(2, tabs.Count);
            Assert.Equal(tabIds[0], tabs[0].GetProperty("id").GetString());
            Assert.Equal("Renamed", tabs[0].GetProperty("name").GetString());
            Assert.NotEqual(tabIds[0], tabs[1].GetProperty("id").GetString());
        }

        [Fact]
        public async Task SaveTabsContainer_RemovingATab_MovesItsChildrenToTheFirstRemainingTab()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("tabsremove");
            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var (containerId, _) = await AddTabsContainer(client, dashboardId);

            var tabIds = await TabIds(client, dashboardId, containerId);
            await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{containerId}/tabs-container", new SaveTabsContainerDto
            {
                Tabs = [new SaveTabDto { Id = tabIds[0], Name = "One" }, new SaveTabDto { Name = "Two" }]
            });
            tabIds = await TabIds(client, dashboardId, containerId);

            var childId = await AddLineItem(client, dashboardId, tracker);
            await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Items =
                [
                    new DashboardLayoutItemDto { ItemId = childId, ParentItemId = containerId, ParentTabId = tabIds[1], X = 0, Y = 0, W = 6, H = 6 }
                ]
            });

            var child = WidgetById(await Widgets(client, dashboardId), childId);
            Assert.Equal(tabIds[1], child.GetProperty("parentTabId").GetString());

            await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{containerId}/tabs-container", new SaveTabsContainerDto
            {
                Tabs = [new SaveTabDto { Id = tabIds[0], Name = "One" }]
            });

            child = WidgetById(await Widgets(client, dashboardId), childId);
            Assert.Equal(containerId, child.GetProperty("parentItemId").GetString());
            Assert.Equal(tabIds[0], child.GetProperty("parentTabId").GetString());
        }

        [Fact]
        public async Task UpdateDashboardLayout_TabsContainerChild_WithUnknownTab_FallsBackToTheFirstTab()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("tabsunknown");
            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var (containerId, tabIds) = await AddTabsContainer(client, dashboardId);
            var childId = await AddLineItem(client, dashboardId, tracker);

            await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Items =
                [
                    new DashboardLayoutItemDto { ItemId = childId, ParentItemId = containerId, ParentTabId = "not-a-real-tab", X = 0, Y = 0, W = 6, H = 6 }
                ]
            });

            var child = WidgetById(await Widgets(client, dashboardId), childId);
            Assert.Equal(containerId, child.GetProperty("parentItemId").GetString());
            Assert.Equal(tabIds[0], child.GetProperty("parentTabId").GetString());
        }

        [Fact]
        public async Task RemoveDashboardItem_TabsContainer_ReparentsChildrenToTheBoard()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("tabsdelete");
            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var (containerId, tabIds) = await AddTabsContainer(client, dashboardId);
            var childId = await AddLineItem(client, dashboardId, tracker);

            await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Items =
                [
                    new DashboardLayoutItemDto { ItemId = containerId, X = 0, Y = 0, W = DashboardGrid.Columns, H = 20 },
                    new DashboardLayoutItemDto { ItemId = childId, ParentItemId = containerId, ParentTabId = tabIds[0], X = 0, Y = 0, W = 6, H = 6 }
                ]
            });

            var remove = await client.DeleteAsync($"dashboard/{dashboardId}/items/{containerId}");
            Assert.Equal(HttpStatusCode.OK, remove.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(1, widgets.GetArrayLength());
            var child = widgets[0];
            Assert.Equal(childId, child.GetProperty("id").GetString());
            Assert.False(child.TryGetProperty("parentItemId", out var p) && p.ValueKind != JsonValueKind.Null && !string.IsNullOrEmpty(p.GetString()));
            Assert.False(child.TryGetProperty("parentTabId", out var t) && t.ValueKind != JsonValueKind.Null && !string.IsNullOrEmpty(t.GetString()));
        }

        [Fact]
        public async Task UpdateDashboardLayout_AContainerCannotBeNestedInATabsContainer()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("tabsnesting");
            var dashboardId = await CreateDashboard(client);
            var (tabsContainerId, tabIds) = await AddTabsContainer(client, dashboardId);
            var plainContainerId = (await Data(await client.PostAsync($"dashboard/{dashboardId}/items/container", null))).GetProperty("id").GetString()!;

            await client.PutAsJsonAsync($"dashboard/{dashboardId}/layout", new UpdateDashboardLayoutDto
            {
                Items =
                [
                    new DashboardLayoutItemDto { ItemId = plainContainerId, ParentItemId = tabsContainerId, ParentTabId = tabIds[0], X = 0, Y = 0, W = 6, H = 6 }
                ]
            });

            var container = WidgetById(await Widgets(client, dashboardId), plainContainerId);
            Assert.False(container.TryGetProperty("parentItemId", out var p) && p.ValueKind != JsonValueKind.Null && !string.IsNullOrEmpty(p.GetString()));
        }

        // Regression: GetUserDashboard's query must be tracked, or SaveChanges silently persists nothing.
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

            // Resolved server-side so the card can render its button without a separate fetch.
            var quickAddTracker = widgets[0].GetProperty("quickAddTracker");
            Assert.Equal(tracker.Id, quickAddTracker.GetProperty("id").GetString());
            Assert.Equal("Weight", quickAddTracker.GetProperty("name").GetString());
        }

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

        // No entry has category Strength, so this view matches nothing; shared by
        // PlaceWidget_ViewIdsNarrowTheWidget and the View widget tests below.
        private static async Task<string> CreateStrengthOnlyView(HttpClient client, CapableTracker tracker)
        {
            var view = await Data(await client.PostAsJsonAsync($"trackers/{tracker.Id}/views", new CreateViewDto
            {
                Name = "Strength only",
                Queries = [TestApi.FilterClause(tracker.CategoryFieldId, OperatorTypes.EqualsOperator, "Strength")]
            }));
            return view.GetProperty("id").GetString()!;
        }

        private static async Task<string> PlaceLineChart(HttpClient client, string dashboardId, CapableTracker tracker, string? viewId = null)
        {
            var source = LineSource(tracker);
            source.ViewId = viewId;

            var response = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
                Sources = [source]
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await Data(response)).GetProperty("id").GetString()!;
        }

        private static async Task<string> PlaceGroupedLineChart(
            HttpClient client, string dashboardId, CapableTracker tracker, string grouping, string code)
        {
            var response = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = code,
                Grouping = grouping,
                Sources = [LineSource(tracker)]
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await Data(response)).GetProperty("id").GetString()!;
        }

        private static JsonElement ChartFor(JsonElement widgets, string itemId)
            => widgets.EnumerateArray().Single(w => w.GetProperty("id").GetString() == itemId);

        private static int PointsOf(JsonElement widgets, string itemId)
            => Analytic(ChartFor(widgets, itemId)).GetProperty("points").GetArrayLength();

        // Nothing else on the board reads this placement, so nothing else is recalculated.
        [Fact]
        public async Task UpdateDashboardItem_ReturnsOnlyThePlacementItChanged()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("itemupdatescope");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var editedId = await PlaceLineChart(client, dashboardId, tracker);
            await PlaceLineChart(client, dashboardId, tracker);

            var sourceId = await SingleSourceId(client, dashboardId, editedId);

            var updated = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{editedId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId, Label = "Trend" }]
            });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

            var widgets = await Data(updated);
            Assert.Equal(1, widgets.GetArrayLength());
            Assert.Equal(editedId, widgets[0].GetProperty("id").GetString());
            Assert.Equal("Trend", Analytic(widgets[0]).GetProperty("name").GetString());
        }

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
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
                Sources = [source]
            }))).GetProperty("id").GetString()!;

            var sourceId = await SingleSourceId(client, dashboardId, itemId);

            var updateResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{itemId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId, Label = "Trend" }]
            });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var updated = (await Data(updateResponse))[0];
            Assert.Equal("Trend", Analytic(updated).GetProperty("name").GetString());
            Assert.Equal(AnalyticTypes.LineChart, Analytic(updated).GetProperty("resultType").GetString());
            Assert.Equal(1, Analytic(updated).GetProperty("points").GetArrayLength());

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal("Trend", Analytic(widgets[0]).GetProperty("name").GetString());
        }

        // Y-axis anchoring is a per-placement choice: an edit can switch a chart from
        // 0-based to fitting the data range.
        [Fact]
        public async Task UpdateDashboardItem_YAxisFromZero_TogglesThePlacementsAxisScaling()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("itemyaxis");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var placed = await Widgets(client, dashboardId);
            Assert.True(Analytic(placed[0]).GetProperty("yAxisFromZero").GetBoolean());

            var sourceId = await SingleSourceId(client, dashboardId, itemId);
            var updateResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{itemId}", new UpdateDashboardItemDto
            {
                YAxisFromZero = false,
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId }]
            });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            Assert.False(Analytic((await Data(updateResponse))[0]).GetProperty("yAxisFromZero").GetBoolean());

            var widgets = await Widgets(client, dashboardId);
            Assert.False(Analytic(widgets[0]).GetProperty("yAxisFromZero").GetBoolean());
        }

        // A blank label falls back to the definition's name, same as no label at all.
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
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
                Sources = [labelled]
            }))).GetProperty("id").GetString()!;

            var sourceId = await SingleSourceId(client, dashboardId, itemId);

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

        // The payload stands for the whole widget: naming only one of two sources is refused
        // rather than half applied.
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
                Code = AnalyticCodes.RawValues,
                Grouping = AnalyticGroupings.None,
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

        // A widget with no sources has nothing this endpoint knows how to edit.
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

        // A date preset, the wrong clause shape for AmountOverClauses(); proves a
        // shape-mismatched preset is rejected.
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

        // Value is left blank for the filter widget to supply on the board. Seeded entry's Amount is 5.
        private static List<ClauseDto> AmountOverClauses() =>
        [
            new ClauseDto
            {
                Kind = QueryKinds.Filter,
                DataType = DataTypes.Number,
                Operator = OperatorTypes.GreaterThan
            }
        ];

        // Clause shape matches AmountOverClauses(), so a filter widget built from those
        // clauses may offer it.
        private static SaveDashboardViewDto AmountOverSet(string value) => new()
        {
            Name = $"Amount over {value}",
            Clauses =
            [
                new ClauseDto
                {
                    Kind = QueryKinds.Filter,
                    DataType = DataTypes.Number,
                    Operator = OperatorTypes.GreaterThan,
                    Value = value
                }
            ]
        };

        // Raw Config JSON of one filter widget on the board.
        private static string FilterConfig(JsonElement widgets, string filterId)
        {
            foreach (var w in widgets.EnumerateArray())
                if (w.GetProperty("id").GetString() == filterId)
                    return w.GetProperty("config").GetString()!;
            throw new InvalidOperationException("filter widget not on the board");
        }

        // Slot id of a filter widget's clause; the key SetFilterValues and a goal's
        // conditional targets expect.
        private static Task<string> FilterSlotId(HttpClient client, string dashboardId, string filterId) =>
            FilterSlotId(client, dashboardId, filterId, 0);

        private static async Task<string> FilterSlotId(HttpClient client, string dashboardId, string filterId, int clauseIndex)
        {
            var widgets = await Widgets(client, dashboardId);
            foreach (var w in widgets.EnumerateArray())
                if (w.GetProperty("id").GetString() == filterId)
                    return w.GetProperty("filter").GetProperty("clauses")[clauseIndex]
                        .GetProperty("slotId").GetString()!;
            throw new InvalidOperationException("filter widget not on the board");
        }

        [Fact]
        public async Task Filter_TypedValue_NarrowsLinkedChartAndClearsWhenBlank()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filternarrows");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);

            var item = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links =
                    [
                        new WidgetLinkDto
                        {
                            ItemId = chartId,
                            TrackerId = tracker.Id,
                            FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
                        }
                    ]
                }));
            var filterId = item.GetProperty("id").GetString()!;
            var slotId = await FilterSlotId(client, dashboardId, filterId);

            // No value yet -- the clause is not applied, so the entry is still there.
            Assert.Equal(1, PointsOf(await Widgets(client, dashboardId), chartId));

            var narrowed = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = "10" } });
            Assert.Equal(HttpStatusCode.OK, narrowed.StatusCode);
            Assert.Equal(0, PointsOf(await Data(narrowed), chartId));

            var cleared = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = "" } });
            Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
            Assert.Equal(1, PointsOf(await Data(cleared), chartId));
        }

        // A write comes back with the widgets it could have changed and nothing else: typing
        // one filter value never recalculates a chart that filter doesn't narrow.
        [Fact]
        public async Task SetFilterValues_ReturnsTheFilterWidgetAndItsFollowersOnly()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filtervaluescope");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var followedId = await PlaceLineChart(client, dashboardId, tracker);
            var unfollowedId = await PlaceLineChart(client, dashboardId, tracker);

            var item = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links =
                    [
                        new WidgetLinkDto
                        {
                            ItemId = followedId,
                            TrackerId = tracker.Id,
                            FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
                        }
                    ]
                }));
            var filterId = item.GetProperty("id").GetString()!;
            var slotId = await FilterSlotId(client, dashboardId, filterId);

            var narrowed = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = "10" } });
            Assert.Equal(HttpStatusCode.OK, narrowed.StatusCode);

            var widgets = await Data(narrowed);
            var returned = widgets.EnumerateArray()
                .Select(w => w.GetProperty("id").GetString())
                .ToList();

            Assert.Equal(2, returned.Count);
            Assert.Contains(filterId, returned);
            Assert.Contains(followedId, returned);

            // The follower comes back narrowed; the chart the filter doesn't reach is simply
            // absent, and a fresh read still draws it in full.
            Assert.Equal(0, PointsOf(widgets, followedId));
            Assert.Equal(1, PointsOf(await Widgets(client, dashboardId), unfollowedId));
        }

        // The one widget an edit that drops a link changes is the widget that has just stopped
        // following, so it has to come back too -- unfiltered.
        [Fact]
        public async Task UpdateFilter_DroppingAFollower_ReturnsItUnfiltered()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filterdropfollower");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);

            var item = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links =
                    [
                        new WidgetLinkDto
                        {
                            ItemId = chartId,
                            TrackerId = tracker.Id,
                            FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
                        }
                    ]
                }));
            var filterId = item.GetProperty("id").GetString()!;
            var slotId = await FilterSlotId(client, dashboardId, filterId);

            var narrowed = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = "10" } });
            Assert.Equal(0, PointsOf(await Data(narrowed), chartId));

            var unlinked = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter",
                new SaveFilterItemDto { Clauses = AmountOverClauses(), Links = [] });
            Assert.Equal(HttpStatusCode.OK, unlinked.StatusCode);

            Assert.Equal(1, PointsOf(await Data(unlinked), chartId));
        }

        [Fact]
        public async Task UpdateFilter_ChangingFollowers_KeepsValuesSetOnTheBoard()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filterupdatekeepsvalues");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);

            WidgetLinkDto LinkTo(string itemId) => new()
            {
                ItemId = itemId,
                TrackerId = tracker.Id,
                FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
            };

            var item = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto { Clauses = AmountOverClauses(), Links = [LinkTo(chartId)] }));
            var filterId = item.GetProperty("id").GetString()!;
            var slotId = await FilterSlotId(client, dashboardId, filterId);

            var narrowed = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = "10" } });
            Assert.Equal(0, PointsOf(await Data(narrowed), chartId));

            // The edit form sends clause shape only, never the value typed on the board.
            var secondChartId = await PlaceLineChart(client, dashboardId, tracker);
            var updated = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links = [LinkTo(chartId), LinkTo(secondChartId)]
                });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

            var widgets = await Data(updated);
            Assert.Equal(0, PointsOf(widgets, chartId));
            Assert.Equal(0, PointsOf(widgets, secondChartId));
            Assert.Equal(0, PointsOf(await Widgets(client, dashboardId), chartId));
        }

        [Fact]
        public async Task RemoveItem_DroppingAFollowedWidget_ClearsItsLinkFromTheFilterWidget()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filterlinkcleanup");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);
            var secondChartId = await PlaceLineChart(client, dashboardId, tracker);

            WidgetLinkDto LinkTo(string itemId) => new()
            {
                ItemId = itemId,
                TrackerId = tracker.Id,
                FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
            };

            var item = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links = [LinkTo(chartId), LinkTo(secondChartId)]
                }));
            var filterId = item.GetProperty("id").GetString()!;

            var removed = await client.DeleteAsync($"dashboard/{dashboardId}/items/{chartId}");
            Assert.Equal(HttpStatusCode.OK, removed.StatusCode);

            // Gone from the stored config, not just the board: resubmitting this widget's
            // links verbatim would otherwise reject the dangling id.
            var config = FilterConfig(await Widgets(client, dashboardId), filterId);
            Assert.DoesNotContain(chartId, config);
            Assert.Contains(secondChartId, config);

            var resaved = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links = [LinkTo(secondChartId)]
                });
            Assert.Equal(HttpStatusCode.OK, resaved.StatusCode);
        }

        [Fact]
        public async Task UpdateFilter_ResubmittingALinkToAWidgetThatIsGone_DropsItInsteadOfFailing()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filterlinkselfheals");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var widgetId = (await CreateWidget(client, tracker)).GetProperty("id").GetString()!;
            var chartId = (await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/place-widget",
                new PlaceWidgetDto { WidgetId = widgetId }))).GetProperty("id").GetString()!;
            var secondChartId = await PlaceLineChart(client, dashboardId, tracker);

            WidgetLinkDto LinkTo(string itemId) => new()
            {
                ItemId = itemId,
                TrackerId = tracker.Id,
                FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
            };

            var item = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links = [LinkTo(chartId), LinkTo(secondChartId)]
                }));
            var filterId = item.GetProperty("id").GetString()!;

            // Deleting the definition cascades the placement off the board without going
            // through RemoveDashboardItem, leaving the link dangling.
            Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"widgets/{widgetId}")).StatusCode);

            var resaved = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links = [LinkTo(chartId), LinkTo(secondChartId)]
                });
            Assert.Equal(HttpStatusCode.OK, resaved.StatusCode);

            var config = FilterConfig(await Widgets(client, dashboardId), filterId);
            Assert.DoesNotContain(chartId, config);
            Assert.Contains(secondChartId, config);
        }

        [Fact]
        public async Task UpdateFilter_LinkingAWidgetThatIsNotOnTheBoard_StillFails()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filterlinkstillvalidates");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);

            WidgetLinkDto LinkTo(string itemId) => new()
            {
                ItemId = itemId,
                TrackerId = tracker.Id,
                FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
            };

            var item = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto { Clauses = AmountOverClauses(), Links = [LinkTo(chartId)] }));
            var filterId = item.GetProperty("id").GetString()!;

            var response = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links = [LinkTo(chartId), LinkTo(Guid.NewGuid().ToString())]
                });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateFilter_ChangingAClauseOperator_DropsThatClausesStaleValue()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filterupdatedropsstale");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);

            var item = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links =
                    [
                        new WidgetLinkDto
                        {
                            ItemId = chartId,
                            TrackerId = tracker.Id,
                            FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
                        }
                    ]
                }));
            var filterId = item.GetProperty("id").GetString()!;
            var slotId = await FilterSlotId(client, dashboardId, filterId);

            await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = "10" } });

            // A changed operator is a new pooled query id, so the old typed value must not
            // ride along. A blank clause is dropped here, unlike Equals/NotEquals, which
            // treat a blank value as its own "is empty" filter.
            var updated = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter",
                new SaveFilterItemDto
                {
                    Clauses =
                    [
                        new ClauseDto
                        {
                            Kind = QueryKinds.Filter,
                            DataType = DataTypes.Number,
                            Operator = OperatorTypes.GreaterThanOrEqual
                        }
                    ],
                    Links =
                    [
                        new WidgetLinkDto
                        {
                            ItemId = chartId,
                            TrackerId = tracker.Id,
                            FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
                        }
                    ]
                });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            Assert.Equal(1, PointsOf(await Data(updated), chartId));
        }

        [Fact]
        public async Task Filter_ResolvedClausesReachAnEntriesWidget()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filterentries");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await PlaceEntriesTable(client, dashboardId, tracker);

            var filterItem = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links =
                    [
                        new WidgetLinkDto
                        {
                            ItemId = itemId,
                            TrackerId = tracker.Id,
                            FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
                        }
                    ]
                }));
            var filterId = filterItem.GetProperty("id").GetString()!;
            var slotId = await FilterSlotId(client, dashboardId, filterId);

            var narrowed = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = "10" } });
            Assert.Equal(0, EntriesRowCount(await Data(narrowed), itemId));
        }

        [Fact]
        public async Task Filter_TwoClausesOfTheSameShape_StayIndependentPerField()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filtersameshape");

            var tracker = await CreateCapableTracker(client, "Weight");
            var scoreFieldId = (await Data(await client.PostAsJsonAsync($"trackers/{tracker.Id}/fields",
                new CreateFieldDto { Name = "Score", Type = DataTypes.Number }))).GetProperty("id").GetString()!;
            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);

            var filter = await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/filter", new SaveFilterItemDto
            {
                Clauses =
                [
                    new ClauseDto { Kind = QueryKinds.Filter, DataType = DataTypes.Number, Operator = OperatorTypes.LessThanOrEqual },
                    new ClauseDto { Kind = QueryKinds.Filter, DataType = DataTypes.Number, Operator = OperatorTypes.LessThanOrEqual }
                ],
                Links =
                [
                    new WidgetLinkDto
                    {
                        ItemId = chartId,
                        TrackerId = tracker.Id,
                        FieldByQuery = new() { ["0"] = tracker.AmountFieldId, ["1"] = scoreFieldId }
                    }
                ]
            }));
            var filterId = filter.GetProperty("id").GetString()!;

            // Both clauses survive as their own input; the second no longer collapses onto
            // the first just because they pool to the same query.
            var clauses = (await Widgets(client, dashboardId)).EnumerateArray()
                .First(w => w.GetProperty("id").GetString() == filterId)
                .GetProperty("filter").GetProperty("clauses");
            Assert.Equal(2, clauses.GetArrayLength());
            var amountSlot = clauses[0].GetProperty("slotId").GetString()!;
            var scoreSlot = clauses[1].GetProperty("slotId").GetString()!;
            Assert.NotEqual(amountSlot, scoreSlot);

            // Entry has Amount 5, no Score: a ceiling on Score alone drops it...
            var byScore = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [scoreSlot] = "100" } });
            Assert.Equal(0, PointsOf(await Data(byScore), chartId));

            // ...while the same ceiling on Amount keeps it: each clause runs against its own field.
            var byAmount = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [amountSlot] = "100" } });
            Assert.Equal(1, PointsOf(await Data(byAmount), chartId));
        }

        [Fact]
        public async Task Goal_ConditionalTargets_KeyOffEachSameShapeClauseIndependently()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("goalsameshape");

            var tracker = await CreateCapableTracker(client, "Focus");
            var reviewedFieldId = (await Data(await client.PostAsJsonAsync($"trackers/{tracker.Id}/fields",
                new CreateFieldDto { Name = "Reviewed", Type = DataTypes.Date }))).GetProperty("id").GetString()!;

            var seedEntry = await client.PostAsJsonAsync($"trackers/{tracker.Id}/entries", new CreateEntryDto
            {
                FieldValues = new()
                {
                    ["Day"] = "2026-02-01",
                    ["Amount"] = "10",
                    ["Category"] = "Cardio",
                    ["Reviewed"] = "2026-02-01"
                }
            });
            Assert.Equal(HttpStatusCode.OK, seedEntry.StatusCode);

            var dashboardId = await CreateDashboard(client);

            var goalItem = await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.Goal,
                Code = AnalyticCodes.Sum,
                GoalTarget = "60",
                Sources =
                [
                    new CreateAndPlaceWidgetSourceDto
                    {
                        TrackerId = tracker.Id,
                        AnalyticFields = [new CreateAnalyticFieldDto { FieldId = tracker.AmountFieldId, Purpose = AnalyticPurposes.Value }]
                    }
                ]
            }));
            var goalId = goalItem.GetProperty("id").GetString()!;
            var sourceId = goalItem.GetProperty("sources")[0].GetProperty("id").GetString()!;

            var filter = await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/filter", new SaveFilterItemDto
            {
                Clauses =
                [
                    new ClauseDto { Kind = QueryKinds.Filter, DataType = DataTypes.Date, Operator = OperatorTypes.LessThanOrEqual },
                    new ClauseDto { Kind = QueryKinds.Filter, DataType = DataTypes.Date, Operator = OperatorTypes.LessThanOrEqual }
                ],
                Links =
                [
                    new WidgetLinkDto
                    {
                        ItemId = goalId,
                        TrackerId = tracker.Id,
                        FieldByQuery = new() { ["0"] = tracker.DayFieldId, ["1"] = reviewedFieldId }
                    }
                ]
            }));
            var filterId = filter.GetProperty("id").GetString()!;
            var daySlot = await FilterSlotId(client, dashboardId, filterId, 0);
            var reviewedSlot = await FilterSlotId(client, dashboardId, filterId, 1);
            Assert.NotEqual(daySlot, reviewedSlot);

            var update = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{goalId}", new UpdateDashboardItemDto
            {
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId }],
                GoalConditionalTargets =
                [
                    new GoalConditionalTargetDto { Conditions = new() { [daySlot] = "2026-03-01" }, Target = "111" },
                    new GoalConditionalTargetDto { Conditions = new() { [reviewedSlot] = "2026-03-01" }, Target = "222" }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            var onDay = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [daySlot] = "2026-03-01" } });
            Assert.Equal("111", Analytic(ChartFor(await Data(onDay), goalId)).GetProperty("target").GetString());

            var onReviewed = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [reviewedSlot] = "2026-03-01" } });
            Assert.Equal("222", Analytic(ChartFor(await Data(onReviewed), goalId)).GetProperty("target").GetString());
        }

        [Fact]
        public async Task AddFilter_NoClauses_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filternoclauses");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            await PlaceLineChart(client, dashboardId, tracker);

            var response = await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddFilter_SortClause_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filtersortclause");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            await PlaceLineChart(client, dashboardId, tracker);

            var response = await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto
                {
                    Clauses =
                    [
                        new ClauseDto
                        {
                            Kind = QueryKinds.Sort,
                            DataType = DataTypes.Number,
                            Descending = true
                        }
                    ]
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddFilter_ClauseMappedToFieldOfWrongType_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filterwrongtype");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);

            var response = await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links =
                    [
                        new WidgetLinkDto
                        {
                            ItemId = chartId,
                            TrackerId = tracker.Id,
                            FieldByQuery = new() { ["0"] = tracker.DayFieldId }
                        }
                    ]
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SetFilterValues_ValueOfWrongType_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filtervaluetype");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var chartId = await PlaceLineChart(client, dashboardId, tracker);

            var item = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto
                {
                    Clauses = AmountOverClauses(),
                    Links =
                    [
                        new WidgetLinkDto
                        {
                            ItemId = chartId,
                            TrackerId = tracker.Id,
                            FieldByQuery = new() { ["0"] = tracker.AmountFieldId }
                        }
                    ]
                }));
            var filterId = item.GetProperty("id").GetString()!;
            var slotId = await FilterSlotId(client, dashboardId, filterId);

            var badValue = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { [slotId] = "not a number" } });
            Assert.Equal(HttpStatusCode.BadRequest, badValue.StatusCode);

            var badKey = await client.PutAsJsonAsync(
                $"dashboard/{dashboardId}/items/{filterId}/filter-values",
                new SetFilterValuesDto { Values = new() { ["not-a-query"] = "5" } });
            Assert.Equal(HttpStatusCode.BadRequest, badKey.StatusCode);
        }

        [Fact]
        public async Task AddFilter_MatchingPreset_ExposesItsValuesOnTheCard()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filterpresetresolve");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            await PlaceLineChart(client, dashboardId, tracker);

            var view = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/views", AmountOverSet("10")));
            var viewId = view.GetProperty("id").GetString()!;

            var item = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto { Clauses = AmountOverClauses(), PresetIds = [viewId] }));
            var filterId = item.GetProperty("id").GetString()!;

            var widgets = await Widgets(client, dashboardId);
            var filter = widgets.EnumerateArray().Single(w => w.GetProperty("id").GetString() == filterId)
                .GetProperty("filter");
            var presets = filter.GetProperty("presets").EnumerateArray().ToList();
            Assert.Single(presets);
            Assert.Equal(viewId, presets[0].GetProperty("id").GetString());
            Assert.Equal("Amount over 10", presets[0].GetProperty("name").GetString());
            Assert.Equal("10", presets[0].GetProperty("values")[0].GetString());
        }

        [Fact]
        public async Task AddFilter_PresetOfMismatchedShape_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filterpresetshape");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            await PlaceLineChart(client, dashboardId, tracker);

            var view = await Data(await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/views", LateInYearSet()));
            var viewId = view.GetProperty("id").GetString()!;

            var response = await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto { Clauses = AmountOverClauses(), PresetIds = [viewId] });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddFilter_PresetIdNotOnBoard_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("filterpresetforeign");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            await PlaceLineChart(client, dashboardId, tracker);

            var response = await client.PostAsJsonAsync(
                $"dashboard/{dashboardId}/items/filter",
                new SaveFilterItemDto { Clauses = AmountOverClauses(), PresetIds = ["not-a-view"] });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddDashboardView_SortClause_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("viewsortclause");
            var dashboardId = await CreateDashboard(client);

            var response = await client.PostAsJsonAsync($"dashboard/{dashboardId}/views",
                new SaveDashboardViewDto
                {
                    Name = "Has a sort",
                    Clauses = [new ClauseDto { Kind = QueryKinds.Sort, DataType = DataTypes.Date, Descending = true }]
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
                var ok = await client.PostAsJsonAsync($"dashboard/{dashboardId}/views", AmountOverSet($"{i}"));
                Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            }

            var overflow = await client.PostAsJsonAsync($"dashboard/{dashboardId}/views", AmountOverSet("999"));
            Assert.Equal(HttpStatusCode.BadRequest, overflow.StatusCode);
        }

        private static async Task<string> PlaceEntriesTable(
            HttpClient client, string dashboardId, CapableTracker tracker, List<string>? columnFieldIds = null)
        {
            var response = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/entries",
                new CreateAndPlaceEntriesWidgetDto
                {
                    TrackerId = tracker.Id,
                    ColumnFieldIds = columnFieldIds ?? []
                });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await Data(response)).GetProperty("id").GetString()!;
        }

        private static JsonElement EntriesWidgetFor(JsonElement widgets, string itemId)
            => widgets.EnumerateArray().Single(w => w.GetProperty("id").GetString() == itemId)
                .GetProperty("entriesWidget");

        private static int EntriesRowCount(JsonElement widgets, string itemId)
            => EntriesWidgetFor(widgets, itemId).GetProperty("entries").GetArrayLength();

        private static List<string> EntriesColumnNames(JsonElement widgets, string itemId)
            => [.. EntriesWidgetFor(widgets, itemId).GetProperty("columns").EnumerateArray()
                .Select(c => c.GetProperty("name").GetString()!)];

        [Fact]
        public async Task EntriesWidget_BuildWidgets_ReturnsRowsResolvedByTheBoard()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("entriesrows");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await PlaceEntriesTable(client, dashboardId, tracker);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(1, EntriesRowCount(widgets, itemId));
            Assert.False(EntriesWidgetFor(widgets, itemId).TryGetProperty("viewId", out _));
        }

        [Fact]
        public async Task CreateAndPlaceEntriesWidget_ColumnFieldIds_LimitAndOrderTheColumns()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("entriescolumns");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await PlaceEntriesTable(client, dashboardId, tracker,
                [tracker.AmountFieldId, tracker.DayFieldId]);

            var columns = EntriesColumnNames(await Widgets(client, dashboardId), itemId);
            Assert.Equal(["Amount", "Day"], columns);
        }

        [Fact]
        public async Task CreateAndPlaceEntriesWidget_NoColumnFieldIds_ShowsEveryField()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("entriesallcolumns");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await PlaceEntriesTable(client, dashboardId, tracker);

            var columns = EntriesColumnNames(await Widgets(client, dashboardId), itemId);
            Assert.Equal(["Day", "Amount", "Category"], columns);
        }

        [Fact]
        public async Task CreateAndPlaceEntriesWidget_UnknownColumnFieldId_ReturnsBadRequest()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("entriesbadcolumn");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);

            var response = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/entries",
                new CreateAndPlaceEntriesWidgetDto
                {
                    TrackerId = tracker.Id,
                    ColumnFieldIds = ["not-a-real-field"]
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEntriesItem_ColumnFieldIds_Persist()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("entriesupdatecolumns");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await PlaceEntriesTable(client, dashboardId, tracker);

            var updated = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{itemId}/entries",
                new UpdateDashboardEntriesItemDto { ColumnFieldIds = [tracker.AmountFieldId] });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            Assert.Equal(["Amount"], EntriesColumnNames(await Data(updated), itemId));

            Assert.Equal(["Amount"], EntriesColumnNames(await Widgets(client, dashboardId), itemId));
        }

        [Fact]
        public async Task EntriesWidget_ColumnFieldDeletedAfterPick_FallsBackGracefully()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("entriescolumngone");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await PlaceEntriesTable(client, dashboardId, tracker,
                [tracker.AmountFieldId, tracker.DayFieldId]);

            var deleted = await client.DeleteAsync($"trackers/{tracker.Id}/fields/{tracker.AmountFieldId}");
            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(["Day"], EntriesColumnNames(widgets, itemId));
            Assert.Equal(1, EntriesRowCount(widgets, itemId));
        }

        // ----- Board document (hand-edited JSON over the same board) -----

        private static readonly JsonSerializerOptions DocumentJsonOptions = new(JsonSerializerDefaults.Web);

        private static async Task<DashboardDocumentDto> GetDocument(HttpClient client, string dashboardId)
        {
            var response = await client.GetAsync($"dashboard/{dashboardId}/document");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await Data(response);
            return JsonSerializer.Deserialize<DashboardDocumentDto>(data.GetRawText(), DocumentJsonOptions)!;
        }

        private static Task<HttpResponseMessage> PutDocument(HttpClient client, string dashboardId, DashboardDocumentDto document)
            => client.PutAsJsonAsync($"dashboard/{dashboardId}/document", document);

        private static async Task<string[]> Messages(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            return [.. json.GetProperty("messages").EnumerateArray().Select(m => m.GetString()!)];
        }

        // The document names items by key and the API by id; these bridge a test that has one or the other.
        private string KeyOf(string itemId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            return db.DashboardItems.Where(i => i.Id == itemId).Select(i => i.Key).Single()!;
        }

        private string ItemIdOf(string dashboardId, string key)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            return db.DashboardItems.Where(i => i.DashboardId == dashboardId && i.Key == key).Select(i => i.Id).Single();
        }

        private DashboardDocumentItemDto DocItem(DashboardDocumentDto document, string itemId)
        {
            var key = KeyOf(itemId);
            return document.Items.Single(i => i.Key == key);
        }

        [Fact]
        public async Task GetDashboardDocument_CarriesPlacementAndEchoesWiringReadOnly()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docget");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);

            Assert.Equal(DashboardDocumentDto.CurrentSchemaVersion, document.SchemaVersion);
            Assert.Equal("My board", document.Board.Name);

            var item = DocItem(document, itemId);
            Assert.Equal(DashboardWidgetTypes.Analytic, item.Type);
            Assert.Equal(DashboardDocumentDisplayModes.Full, item.Layout!.DisplayMode);
            Assert.NotNull(item.Wiring);
            Assert.Equal("Workouts", item.Wiring!.Sources![0].TrackerName);
        }

        [Fact]
        public async Task SaveDashboardDocument_AppliesPlacementAndColor()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docsave");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            var item = DocItem(document, itemId);
            item.Layout = new DashboardDocumentLayoutDto { X = 4, Y = 7, W = 8, H = 10, DisplayMode = DashboardDocumentDisplayModes.Expandable };
            item.Color = "grape";
            document.Board.Name = "Renamed board";

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var widget = WidgetById(await Widgets(client, dashboardId), itemId);
            Assert.Equal(4, Layout(widget).GetProperty("x").GetInt32());
            Assert.Equal(7, Layout(widget).GetProperty("y").GetInt32());
            Assert.Equal(8, Layout(widget).GetProperty("w").GetInt32());
            Assert.Equal(10, Layout(widget).GetProperty("h").GetInt32());
            Assert.Equal((int)DashboardItemDisplayMode.Expandable, Layout(widget).GetProperty("displayMode").GetInt32());
            Assert.Equal("grape", widget.GetProperty("color").GetString());

            var board = await Data(await client.GetAsync($"dashboard/{dashboardId}"));
            Assert.Equal("Renamed board", board.GetProperty("name").GetString());
        }

        // UpdateDashboardLayout clamps a dragged widget; a hand-written number is reported
        // instead, or the edit looks like it silently failed.
        [Fact]
        public async Task SaveDashboardDocument_OutOfBoundsWidth_IsRejectedRatherThanClamped()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docclamp");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var before = Layout(WidgetById(await Widgets(client, dashboardId), itemId)).GetProperty("w").GetInt32();

            var document = await GetDocument(client, dashboardId);
            DocItem(document, itemId).Layout!.W = DashboardGrid.Columns + 5;

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("items[0].layout.w"));

            var after = Layout(WidgetById(await Widgets(client, dashboardId), itemId)).GetProperty("w").GetInt32();
            Assert.Equal(before, after);
        }

        [Fact]
        public async Task SaveDashboardDocument_ReportsEveryProblemAtOnce()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docmulti");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            document.Board.Name = "   ";
            document.Items[0].Layout!.W = 0;
            document.Items[0].MobileLayout!.DisplayMode = "sideways";

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var messages = await Messages(response);
            Assert.Contains(messages, m => m.Contains("board.name"));
            Assert.Contains(messages, m => m.Contains("items[0].layout.w"));
            Assert.Contains(messages, m => m.Contains("items[0].mobileLayout.displayMode"));
        }

        // Source order is what a combined chart draws in, so the echo is order-sensitive.
        [Fact]
        public async Task SaveDashboardDocument_ReorderedWiringList_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docwiringorder");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            var source = DocItem(document, itemId).Wiring!.Sources![0];
            source.Fields.Reverse();

            var reversedFields = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, reversedFields.StatusCode);

            source.Fields.Reverse();
            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);
        }

        private static JsonNode? ReverseKeys(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject obj:
                    var reversed = new JsonObject();
                    foreach (var property in obj.Reverse())
                        reversed[property.Key] = ReverseKeys(property.Value?.DeepClone());
                    return reversed;
                case JsonArray array:
                    var copy = new JsonArray();
                    foreach (var element in array)
                        copy.Add(ReverseKeys(element?.DeepClone()));
                    return copy;
                default:
                    return node?.DeepClone();
            }
        }

        // An editor that reformats, or a user who moves a line, must not read as an edit to
        // the read-only block.
        [Fact]
        public async Task SaveDashboardDocument_ReformattedDocument_IsAccepted()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docreformat");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            DocItem(document, itemId).Layout!.X = 5;

            var shuffled = ReverseKeys(JsonSerializer.SerializeToNode(document, DocumentJsonOptions))!.ToJsonString();
            var content = new StringContent(shuffled, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"dashboard/{dashboardId}/document", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var widget = WidgetById(await Widgets(client, dashboardId), itemId);
            Assert.Equal(5, Layout(widget).GetProperty("x").GetInt32());
        }

        [Fact]
        public async Task SaveDashboardDocument_OmittedReadOnlyBlocks_AreAccepted()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docomit");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            var item = DocItem(document, itemId);
            item.Wiring = null;
            item.Name = null;
            item.Type = null;
            item.Layout!.X = 3;

            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            var widget = WidgetById(await Widgets(client, dashboardId), itemId);
            Assert.Equal(3, Layout(widget).GetProperty("x").GetInt32());
        }

        [Fact]
        public async Task SaveDashboardDocument_SchemaVersionMismatch_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docschema");

            var dashboardId = await CreateDashboard(client);
            var document = await GetDocument(client, dashboardId);
            document.SchemaVersion = 99;

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("schemaVersion"));
        }

        [Fact]
        public async Task SaveDashboardDocument_RenamesAndReordersTabsInPlace()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("doctabs");

            var dashboardId = await CreateDashboard(client);
            var (containerId, _) = await AddTabsContainer(client, dashboardId);
            await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{containerId}/tabs-container", new SaveTabsContainerDto
            {
                Title = "Panel",
                Tabs = [new SaveTabDto { Name = "First" }, new SaveTabDto { Name = "Second" }]
            });
            var tabIds = await TabIds(client, dashboardId, containerId);

            var document = await GetDocument(client, dashboardId);
            var item = DocItem(document, containerId);
            var tabs = item.Tabs.Value!;
            tabs.Reverse();
            tabs[0] = "Renamed";

            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            var config = JsonDocument.Parse(
                WidgetById(await Widgets(client, dashboardId), containerId).GetProperty("config").GetString()!).RootElement;
            var savedTabs = config.GetProperty("tabs").EnumerateArray().ToList();
            Assert.Equal(tabIds[1], savedTabs[0].GetProperty("id").GetString());
            Assert.Equal("Renamed", savedTabs[0].GetProperty("name").GetString());
            Assert.Equal(tabIds[0], savedTabs[1].GetProperty("id").GetString());
        }

        [Fact]
        public async Task SaveDashboardDocument_MovesWidgetIntoATab()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("doctabmove");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var (containerId, tabIds) = await AddTabsContainer(client, dashboardId);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            var item = DocItem(document, itemId);
            item.Parent = KeyOf(containerId);
            item.Tab = "Tab 1";

            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            var widget = WidgetById(await Widgets(client, dashboardId), itemId);
            Assert.Equal(containerId, widget.GetProperty("parentItemId").GetString());
            Assert.Equal(tabIds[0], widget.GetProperty("parentTabId").GetString());
        }

        [Fact]
        public async Task SaveDashboardDocument_UnknownTab_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docbadtab");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var (containerId, _) = await AddTabsContainer(client, dashboardId);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            var item = DocItem(document, itemId);
            item.Parent = KeyOf(containerId);
            item.Tab = "no-such-tab";

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains(".tab"));
        }

        [Fact]
        public async Task SaveDashboardDocument_NestingAContainer_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docnest");

            var dashboardId = await CreateDashboard(client);
            var (tabsContainerId, _) = await AddTabsContainer(client, dashboardId);
            var plainContainerId = (await Data(await client.PostAsync($"dashboard/{dashboardId}/items/container", null))).GetProperty("id").GetString()!;

            var document = await GetDocument(client, dashboardId);
            DocItem(document, plainContainerId).Parent = KeyOf(tabsContainerId);

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("container cannot be nested"));
        }

        [Fact]
        public async Task SaveDashboardDocument_SetsHeaderText()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("dochead");

            var dashboardId = await CreateDashboard(client);
            var headerId = (await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/header",
                new AddDashboardHeaderItemDto { Text = "Before" }))).GetProperty("id").GetString()!;

            var document = await GetDocument(client, dashboardId);
            DocItem(document, headerId).Text = "After";

            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            var config = JsonDocument.Parse(
                WidgetById(await Widgets(client, dashboardId), headerId).GetProperty("config").GetString()!).RootElement;
            Assert.Equal("After", config.GetProperty("text").GetString());
        }

        [Fact]
        public async Task SaveDashboardDocument_TextOnADivider_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docdivider");

            var dashboardId = await CreateDashboard(client);
            var dividerId = (await Data(await client.PostAsync($"dashboard/{dashboardId}/items/divider", null))).GetProperty("id").GetString()!;

            var document = await GetDocument(client, dashboardId);
            DocItem(document, dividerId).Text = "Nope";

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("has no text"));
        }

        [Fact]
        public async Task SaveDashboardDocument_SetsEntriesColumns()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("doccolumns");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await PlaceEntriesTable(client, dashboardId, tracker, [tracker.DayFieldId, tracker.AmountFieldId]);

            var document = await GetDocument(client, dashboardId);
            DocItem(document, itemId).Columns = new List<string> { "Amount" };

            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            Assert.Equal(["Amount"], EntriesColumnNames(await Widgets(client, dashboardId), itemId));
        }

        [Fact]
        public async Task SaveDashboardDocument_ColumnFromAnotherTracker_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docbadcolumn");

            var tracker = await CreateCapableTracker(client, "Weight");
            var other = await CreateCapableTracker(client, "Sleep");
            var dashboardId = await CreateDashboard(client);
            var itemId = await PlaceEntriesTable(client, dashboardId, tracker, [tracker.DayFieldId]);

            var document = await GetDocument(client, dashboardId);
            DocItem(document, itemId).Columns = new List<string> { "Elsewhere" };

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("columns"));
        }

        // Order is derived, never authored: the document carries no order field at all.
        [Fact]
        public async Task SaveDashboardDocument_DerivesOrderFromDesktopPlacement()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docorder");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var first = await AddLineItem(client, dashboardId, tracker);
            var second = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            DocItem(document, first).Layout!.Y = 50;
            DocItem(document, second).Layout!.Y = 0;

            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            var board = await Data(await client.GetAsync($"dashboard/{dashboardId}"));
            var ordered = board.GetProperty("items").EnumerateArray()
                .OrderBy(i => i.GetProperty("order").GetInt32())
                .Select(i => i.GetProperty("id").GetString())
                .ToList();
            Assert.Equal([second, first], ordered);
        }


        // Drops keys from one item so the request looks like a user deleting those lines.
        private async Task<HttpResponseMessage> PutDocumentWithout(
            HttpClient client, string dashboardId, DashboardDocumentDto document, string itemId, params string[] keys)
        {
            var node = JsonSerializer.SerializeToNode(document, DocumentJsonOptions)!;
            foreach (var element in node["items"]!.AsArray())
            {
                if (element!["key"]!.GetValue<string>() != KeyOf(itemId))
                    continue;

                foreach (var key in keys)
                    element.AsObject().Remove(key);
            }

            return await client.PutAsync($"dashboard/{dashboardId}/document",
                new StringContent(node.ToJsonString(), System.Text.Encoding.UTF8, "application/json"));
        }

        private static async Task<string?> ItemColor(HttpClient client, string dashboardId, string itemId)
        {
            var widget = WidgetById(await Widgets(client, dashboardId), itemId);
            return widget.TryGetProperty("color", out var color) && color.ValueKind != JsonValueKind.Null
                ? color.GetString()
                : null;
        }

        [Fact]
        public async Task SaveDashboardDocument_OmittedColor_LeavesItAlone()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("doccolor");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            DocItem(document, itemId).Color = "grape";
            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);
            Assert.Equal("grape", await ItemColor(client, dashboardId, itemId));

            var reread = await GetDocument(client, dashboardId);
            var response = await PutDocumentWithout(client, dashboardId, reread, itemId, "color");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("grape", await ItemColor(client, dashboardId, itemId));
        }

        [Fact]
        public async Task SaveDashboardDocument_NullColor_ClearsItToAuto()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docclearcolor");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            DocItem(document, itemId).Color = "grape";
            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            var reread = await GetDocument(client, dashboardId);
            DocItem(reread, itemId).Color = null;

            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, reread)).StatusCode);
            Assert.Null(await ItemColor(client, dashboardId, itemId));
        }

        [Fact]
        public async Task SaveDashboardDocument_OmittedParent_KeepsTheWidgetInItsTab()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("dockeeptab");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var (containerId, tabIds) = await AddTabsContainer(client, dashboardId);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            var item = DocItem(document, itemId);
            item.Parent = KeyOf(containerId);
            item.Tab = "Tab 1";
            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            var reread = await GetDocument(client, dashboardId);
            var response = await PutDocumentWithout(client, dashboardId, reread, itemId, "parent", "tab");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var widget = WidgetById(await Widgets(client, dashboardId), itemId);
            Assert.Equal(containerId, widget.GetProperty("parentItemId").GetString());
            Assert.Equal(tabIds[0], widget.GetProperty("parentTabId").GetString());
        }

        [Fact]
        public async Task SaveDashboardDocument_NullParent_MovesTheWidgetBackToTheBoard()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docleavetab");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var (containerId, tabIds) = await AddTabsContainer(client, dashboardId);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            var item = DocItem(document, itemId);
            item.Parent = KeyOf(containerId);
            item.Tab = "Tab 1";
            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            var reread = await GetDocument(client, dashboardId);
            var moved = DocItem(reread, itemId);
            moved.Parent = null;
            moved.Tab = null;

            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, reread)).StatusCode);

            var widget = WidgetById(await Widgets(client, dashboardId), itemId);
            Assert.False(widget.TryGetProperty("parentItemId", out var parent) && parent.ValueKind != JsonValueKind.Null);
        }

        // Text has no null state, so null would be a write that quietly did nothing.
        [Fact]
        public async Task SaveDashboardDocument_NullText_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docnulltext");

            var dashboardId = await CreateDashboard(client);
            var headerId = (await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/header",
                new AddDashboardHeaderItemDto { Text = "Kept" }))).GetProperty("id").GetString()!;

            var document = await GetDocument(client, dashboardId);
            DocItem(document, headerId).Text = null;

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("text: cannot be null"));

            var config = JsonDocument.Parse(
                WidgetById(await Widgets(client, dashboardId), headerId).GetProperty("config").GetString()!).RootElement;
            Assert.Equal("Kept", config.GetProperty("text").GetString());
        }

        [Fact]
        public async Task SaveDashboardDocument_EmptyText_ClearsIt()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docemptytext");

            var dashboardId = await CreateDashboard(client);
            var headerId = (await Data(await client.PostAsJsonAsync($"dashboard/{dashboardId}/items/header",
                new AddDashboardHeaderItemDto { Text = "Before" }))).GetProperty("id").GetString()!;

            var document = await GetDocument(client, dashboardId);
            DocItem(document, headerId).Text = "";

            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            var config = JsonDocument.Parse(
                WidgetById(await Widgets(client, dashboardId), headerId).GetProperty("config").GetString()!).RootElement;
            Assert.Equal("", config.GetProperty("text").GetString());
        }

        [Fact]
        public async Task SaveDashboardDocument_NullColumnFieldIds_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docnullcolumns");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await PlaceEntriesTable(client, dashboardId, tracker, [tracker.DayFieldId]);

            var document = await GetDocument(client, dashboardId);
            DocItem(document, itemId).Columns = null;

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("columns: cannot be null"));
        }

        [Fact]
        public async Task SaveDashboardDocument_OtherUsersBoard_IsNotFound()
        {
            await _factory.SeedDatabaseAsync();
            var owner = await _factory.NewUserClient("docowner");
            var stranger = await _factory.NewUserClient("docstranger");

            var dashboardId = await CreateDashboard(owner);
            var document = await GetDocument(owner, dashboardId);

            Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"dashboard/{dashboardId}/document")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await PutDocument(stranger, dashboardId, document)).StatusCode);
        }

        // ----- Board document: building and reshaping a board from JSON -----

        private static Task<HttpResponseMessage> PutJson(HttpClient client, string dashboardId, string json)
            => client.PutAsync($"dashboard/{dashboardId}/document", new StringContent(json, Encoding.UTF8, "application/json"));

        private static Task<HttpResponseMessage> PostJson(HttpClient client, string json)
            => client.PostAsync("dashboard/document", new StringContent(json, Encoding.UTF8, "application/json"));

        private static string Board(string items, string? boardExtras = null) => $$"""
            { "schemaVersion": 3, "board": { "name": "My board"{{(boardExtras == null ? "" : ", " + boardExtras)}} }, "items": [ {{items}} ] }
            """;

        private static async Task AssertSaved(HttpResponseMessage response)
            => Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        private static DashboardDocumentItemDto ItemWithText(DashboardDocumentDto document, string text)
            => document.Items.Single(i => i.Text.IsSet && i.Text.Value == text);

        private static DashboardDocumentItemDto ItemNamed(DashboardDocumentDto document, string name)
            => document.Items.Single(i => i.Name == name);

        [Fact]
        public async Task SaveDashboardDocument_ItemLeftOut_IsDeleted()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docmissing");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            document.Items.Clear();

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(0, (await Widgets(client, dashboardId)).GetArrayLength());
            Assert.Equal(1, (await Data(await client.GetAsync("widgets"))).GetArrayLength());
        }

        [Fact]
        public async Task SaveDashboardDocument_NewKeyWithoutAType_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docnotype");

            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board("""{ "key": "mystery" }"""));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("mystery") && m.Contains("type"));
        }

        [Fact]
        public async Task SaveDashboardDocument_RenamedDefinition_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docdefinition");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            DocItem(document, itemId).Wiring!.Widget!.Code = AnalyticCodes.Sum;

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("wiring.widget is read-only"));
        }

        [Fact]
        public async Task SaveDashboardDocument_ViewOfAnotherTracker_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docviewowner");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            DocItem(document, itemId).Wiring!.Sources![0].View = "some-other-view";

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("sources[0].view"));
        }

        [Fact]
        public async Task SaveDashboardDocument_AddingATab_CreatesIt()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("doctabadd");

            var dashboardId = await CreateDashboard(client);
            var (containerId, tabIds) = await AddTabsContainer(client, dashboardId);

            var document = await GetDocument(client, dashboardId);
            DocItem(document, containerId).Tabs.Value!.Add("Extra");

            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);
            var after = await TabIds(client, dashboardId, containerId);
            Assert.Equal(2, after.Length);
            Assert.Equal(tabIds[0], after[0]);
        }

        [Fact]
        public async Task SaveDashboardDocument_ChildOfADroppedTab_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("doctabdrop");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var (containerId, tabIds) = await AddTabsContainer(client, dashboardId);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            var child = DocItem(document, itemId);
            child.Parent = KeyOf(containerId);
            child.Tab = "Tab 1";
            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            document = await GetDocument(client, dashboardId);
            DocItem(document, containerId).Tabs = new Optional<List<string>>(["Fresh"]);

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("is not a tab of"));
        }

        [Fact]
        public async Task SaveDashboardDocument_ChildOfADeletedContainer_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docorphan");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var (containerId, tabIds) = await AddTabsContainer(client, dashboardId);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            var child = DocItem(document, itemId);
            child.Parent = KeyOf(containerId);
            child.Tab = "Tab 1";
            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            document = await GetDocument(client, dashboardId);
            document.Items.Remove(DocItem(document, containerId));

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("is not a container in this document"));
        }

        [Fact]
        public async Task SaveDashboardDocument_CreatesStructuralWidgets()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("doccreatestruct");

            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board("""
                { "key": "title", "type": "header", "text": "Overview" },
                { "key": "rule", "type": "divider" },
                { "key": "memo", "type": "note", "text": "Remember" },
                { "key": "group", "type": "container", "text": "Group" },
                { "key": "inner", "type": "header", "text": "Inside", "parent": "group" },
                { "key": "panel", "type": "tabsContainer", "text": "Panel",
                  "tabs": [ "First", "Second" ] },
                { "key": "tabbed", "type": "note", "text": "In b", "parent": "panel", "tab": "Second" }
                """));
            await AssertSaved(response);

            var document = await GetDocument(client, dashboardId);
            Assert.Equal(7, document.Items.Count);
            Assert.Equivalent(new[] { "title", "rule", "memo", "group", "inner", "panel", "tabbed" }, document.Items.Select(i => i.Key));

            var group = ItemWithText(document, "Group");
            var panel = ItemWithText(document, "Panel");
            Assert.Equal(group.Key, ItemWithText(document, "Inside").Parent.Value);
            Assert.Equal(["First", "Second"], panel.Tabs.Value);

            var tabbed = ItemWithText(document, "In b");
            Assert.Equal(panel.Key, tabbed.Parent.Value);
            Assert.Equal("Second", tabbed.Tab.Value);
        }

        [Fact]
        public async Task SaveDashboardDocument_NewItemWithoutLayout_IsPlacedBelowTheBoard()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docplaced");

            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board("""
                { "key": "one", "type": "header", "text": "One", "layout": { "x": 0, "y": 0, "w": 24, "h": 5, "displayMode": "full" } },
                { "key": "two", "type": "header", "text": "Two" }
                """));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var document = await GetDocument(client, dashboardId);
            var one = ItemWithText(document, "One").Layout!;
            var two = ItemWithText(document, "Two").Layout!;
            Assert.True(two.Y >= one.Y + one.H);
        }

        [Fact]
        public async Task SaveDashboardDocument_CreatesAnalyticFromAnInlineDefinition()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docinline");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board("""
                { "key": "total", "type": "analytic", "name": "Total",
                  "wiring": {
                    "widget": { "resultType": "Single Value", "code": "Sum" },
                    "sources": [ { "trackerName": "Workouts", "label": "All time", "fields": [ "Value: Amount" ] } ] } }
                """));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var item = ItemNamed(await GetDocument(client, dashboardId), "Total");
            Assert.Equal(AnalyticTypes.SingleValue, item.Wiring!.Widget!.ResultType);
            Assert.Equal(AnalyticCodes.Sum, item.Wiring.Widget.Code);
            Assert.Equal(["Value: Amount"], item.Wiring.Sources![0].Fields);
            Assert.Equal("All time", item.Wiring.Sources[0].Label.Value);

            var library = await Data(await client.GetAsync("widgets"));
            Assert.Equal(1, library.GetArrayLength());
            Assert.Equal("Total", library[0].GetProperty("name").GetString());
        }

        [Fact]
        public async Task SaveDashboardDocument_PlacesALibraryWidgetByReference()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docreference");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            await CreateWidget(client, tracker, "Trend");

            var response = await PutJson(client, dashboardId, Board("""
                { "key": "trend", "type": "analytic",
                  "wiring": { "library": "Trend", "sources": [ { "label": "Mine" } ] } }
                """));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var item = ItemNamed(await GetDocument(client, dashboardId), "Trend");
            Assert.Equal(AnalyticTypes.LineChart, item.Wiring!.Widget!.ResultType);
            Assert.Equal("Mine", item.Wiring.Sources![0].Label.Value);
            Assert.Equal(1, (await Data(await client.GetAsync("widgets"))).GetArrayLength());
        }

        [Fact]
        public async Task SaveDashboardDocument_InvalidDefinition_ListsTheValidValues()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docbaddefinition");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board("""
                { "key": "bad", "type": "analytic",
                  "wiring": {
                    "widget": { "resultType": "Pie", "code": "Sum" },
                    "sources": [ { "trackerName": "Workouts", "fields": [ "Value: Nope", "Amount" ] } ] } }
                """));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var messages = await Messages(response);
            Assert.Contains(messages, m => m.Contains("resultType") && m.Contains("Single Value"));
            Assert.Contains(messages, m => m.Contains("no field \"Nope\""));
            Assert.Contains(messages, m => m.Contains("Purpose: Field name"));
        }

        [Fact]
        public async Task SaveDashboardDocument_WrongCalculation_SaysWhatTheTypeTakes()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docwrongcode");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board("""
                { "key": "bars", "type": "analytic",
                  "wiring": {
                    "widget": { "resultType": "Bar Chart", "code": "Raw Values", "grouping": "Exact" },
                    "sources": [ { "trackerName": "Workouts", "fields": [ "Value: Amount", "Name: Category" ] } ] } }
                """));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("a Bar Chart takes one of") && m.Contains("Sum"));
        }

        [Fact]
        public async Task SaveDashboardDocument_CreatesQuickAddAndEntriesWidgets()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docquickentries");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board("""
                { "key": "add", "type": "quickAdd", "wiring": { "trackerName": "Workouts" } },
                { "key": "log", "type": "entries", "name": "Log", "columns": [ "Day", "Category" ],
                  "wiring": { "trackerName": "Workouts" } }
                """));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var document = await GetDocument(client, dashboardId);
            Assert.Equal("Workouts", document.Items.Single(i => i.Type == DashboardWidgetTypes.QuickAdd).Wiring!.TrackerName);

            var log = ItemNamed(document, "Log");
            Assert.Equal("Workouts", log.Wiring!.TrackerName);
            Assert.Equal(["Day", "Category"], log.Columns.Value);
        }

        [Fact]
        public async Task SaveDashboardDocument_AmbiguousTrackerName_AsksForARename()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docambiguous");

            await CreateCapableTracker(client, "Twin");
            await CreateCapableTracker(client, "Twin");
            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board("""
                { "key": "add", "type": "quickAdd", "wiring": { "trackerName": "Twin" } }
                """));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("2 trackers are named") && m.Contains("Rename one"));
        }

        private const string GoalWithFilterItems = """
            { "key": "goal", "type": "analytic", "name": "Volume",
              "wiring": {
                "widget": { "resultType": "Goal", "code": "Sum", "goalTarget": "60" },
                "sources": [ { "trackerName": "Workouts", "fields": [ "Value: Amount" ] } ],
                "goalConditionalTargets": [ { "conditions": { "min": "1" }, "target": "999" } ] } },
            { "key": "filter", "type": "filter",
              "wiring": { "filter": {
                "clauses": [ { "key": "min", "dataType": "number", "operator": "Greater Than", "value": "1" } ],
                "links": [ { "item": "goal", "fields": { "min": "Amount" } } ] } } }
            """;

        [Fact]
        public async Task SaveDashboardDocument_CreatesAFilterWithLinksAndGoalTargets()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docfilter");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board(GoalWithFilterItems));
            await AssertSaved(response);

            var document = await GetDocument(client, dashboardId);
            var goal = ItemNamed(document, "Volume");
            var filter = document.Items.Single(i => i.Type == DashboardWidgetTypes.Filter).Wiring!.Filter!;

            var clause = Assert.Single(filter.Clauses);
            Assert.Equal("1", clause.Value);
            Assert.Equal(goal.Key, Assert.Single(filter.Links).Item);
            Assert.Equal("Amount", filter.Links[0].Fields[clause.Key!]);

            var row = Assert.Single(goal.Wiring!.GoalConditionalTargets!);
            Assert.Equal("1", row.Conditions[clause.Key!]);

            // The filter is set to "1" and the conditional row keys off it, so 999 replaces 60.
            var target = Analytic(ChartFor(await Widgets(client, dashboardId), ItemIdOf(dashboardId, goal.Key))).GetProperty("target").GetString();
            Assert.Equal("999", target);
        }

        [Fact]
        public async Task SaveDashboardDocument_RelativeDateFilterValue_IsAcceptedAndKept()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docrelativedate");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board("""
                { "key": "range", "type": "filter",
                  "wiring": { "filter": {
                    "clauses": [ { "key": "from", "dataType": "date", "operator": "Greater Than Or Equal", "value": "start_of_month" } ],
                    "links": [ { "item": "volume", "fields": { "from": "Day" } } ] } } },
                { "key": "volume", "type": "analytic", "name": "Volume",
                  "wiring": {
                    "widget": { "resultType": "Goal", "code": "Sum", "goalTarget": "60" },
                    "sources": [ { "trackerName": "Workouts", "fields": [ "Value: Amount" ] } ] } }
                """));
            await AssertSaved(response);

            var filter = (await GetDocument(client, dashboardId)).Items.Single(i => i.Type == DashboardWidgetTypes.Filter).Wiring!.Filter!;
            Assert.Equal("start_of_month", filter.Clauses[0].Value);
            Assert.Equal("Day", Assert.Single(filter.Links).Fields[filter.Clauses[0].Key!]);
        }

        [Fact]
        public async Task SaveDashboardDocument_ExportedBoard_ImportsUnchanged()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docroundtrip");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            Assert.Equal(HttpStatusCode.OK, (await PutJson(client, dashboardId, Board(
                GoalWithFilterItems + """
                , { "key": "title", "type": "header", "text": "Overview" }
                , { "key": "log", "type": "entries", "name": "Log", "wiring": { "trackerName": "Workouts" } }
                """,
                """ "presets": [ { "name": "Big", "clauses": [ { "kind": "filter", "dataType": "number", "operator": "Greater Than", "value": "3" } ] } ] """))).StatusCode);

            var before = JsonSerializer.Serialize(await GetDocument(client, dashboardId), DocumentJsonOptions);

            var document = await GetDocument(client, dashboardId);
            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            Assert.Equal(before, JsonSerializer.Serialize(await GetDocument(client, dashboardId), DocumentJsonOptions));
        }

        [Fact]
        public async Task SaveDashboardDocument_DeletedFollower_IsUnlinkedFromAnUntouchedFilter()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docunlink");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            Assert.Equal(HttpStatusCode.OK, (await PutJson(client, dashboardId, Board(GoalWithFilterItems))).StatusCode);

            var document = await GetDocument(client, dashboardId);
            document.Items.Remove(ItemNamed(document, "Volume"));
            document.Items.Single(i => i.Type == DashboardWidgetTypes.Filter).Wiring = null;
            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            var after = await GetDocument(client, dashboardId);
            Assert.Empty(after.Items.Single().Wiring!.Filter!.Links);
        }

        [Fact]
        public async Task SaveDashboardDocument_LinkToADeletedItem_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docdeadlink");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            Assert.Equal(HttpStatusCode.OK, (await PutJson(client, dashboardId, Board(GoalWithFilterItems))).StatusCode);

            var document = await GetDocument(client, dashboardId);
            document.Items.Remove(ItemNamed(document, "Volume"));

            var response = await PutDocument(client, dashboardId, document);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("is not an item in this document"));
        }

        [Fact]
        public async Task SaveDashboardDocument_GoalConditionOnAMissingClause_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docgoalkey");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board(GoalWithFilterItems.Replace("\"min\": \"1\" }, \"target\"", "\"nope\": \"1\" }, \"target\"")));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("no filter clause has the key \"nope\""));
        }

        [Fact]
        public async Task SaveDashboardDocument_Presets_AreCreatedRenamedAndDeleted()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docpresets");

            var dashboardId = await CreateDashboard(client);
            const string clause = """{ "kind": "filter", "dataType": "number", "operator": "Greater Than", "value": "3" }""";

            Assert.Equal(HttpStatusCode.OK, (await PutJson(client, dashboardId, Board("", $$"""
                "presets": [ { "name": "Big", "clauses": [ {{clause}} ] }, { "name": "Huge", "clauses": [ {{clause}} ] } ]
                """))).StatusCode);

            var document = await GetDocument(client, dashboardId);
            Assert.Equal(["Big", "Huge"], document.Board.Presets!.Select(p => p.Name));

            document.Board.Presets.RemoveAt(1);
            document.Board.Presets[0].Name = "Large";
            Assert.Equal(HttpStatusCode.OK, (await PutDocument(client, dashboardId, document)).StatusCode);

            var renamed = (await GetDocument(client, dashboardId)).Board.Presets!;
            var only = Assert.Single(renamed);
            Assert.Equal("Large", only.Name);

            // A document that says nothing about presets leaves them be.
            Assert.Equal(HttpStatusCode.OK, (await PutJson(client, dashboardId, Board(""))).StatusCode);
            Assert.Single((await GetDocument(client, dashboardId)).Board.Presets!);

            // Listing none is deliberate, and clears them.
            Assert.Equal(HttpStatusCode.OK, (await PutJson(client, dashboardId, Board("", "\"presets\": []"))).StatusCode);
            Assert.Empty((await GetDocument(client, dashboardId)).Board.Presets!);
        }

        [Fact]
        public async Task SaveDashboardDocument_FilterOffersPresetsByName()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docpresetname");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board("""
                { "key": "filter", "type": "filter",
                  "wiring": { "filter": {
                    "clauses": [ { "key": "min", "dataType": "number", "operator": "Greater Than" } ],
                    "presets": [ "Big" ] } } }
                """, """ "presets": [ { "name": "Big", "clauses": [ { "kind": "filter", "dataType": "number", "operator": "Greater Than", "value": "3" } ] } ] """));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var document = await GetDocument(client, dashboardId);
            Assert.Equal(["Big"], document.Items.Single().Wiring!.Filter!.Presets);
        }

        [Fact]
        public async Task SaveDashboardDocument_FailureAfterWriting_RollsEverythingBack()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docrollback");

            await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            // The goal passes every check the document can make on its own; only the Library rejects
            // it, after the first widget has already been created.
            var response = await PutJson(client, dashboardId, Board("""
                { "key": "fine", "type": "analytic", "name": "Fine",
                  "wiring": { "widget": { "resultType": "Single Value", "code": "Sum" },
                              "sources": [ { "trackerName": "Workouts", "fields": [ "Value: Amount" ] } ] } },
                { "key": "title", "type": "header", "text": "Kept out" },
                { "key": "goal", "type": "analytic", "name": "Broken",
                  "wiring": { "widget": { "resultType": "Goal", "code": "Sum" },
                              "sources": [ { "trackerName": "Workouts", "fields": [ "Value: Amount" ] } ] } }
                """));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("items[2].wiring.widget"));

            Assert.Equal(0, (await Widgets(client, dashboardId)).GetArrayLength());
            Assert.Equal(0, (await Data(await client.GetAsync("widgets"))).GetArrayLength());
        }

        [Fact]
        public async Task SaveDashboardDocument_MoreItemsThanABoardHolds_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("doclimit");

            var dashboardId = await CreateDashboard(client);
            var items = string.Join(",", Enumerable.Range(0, DataLimits.MaxDashboardItemCount + 1)
                .Select(i => $$"""{ "key": "h{{i}}", "type": "header", "text": "H{{i}}" }"""));

            var response = await PutJson(client, dashboardId, Board(items));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("dashboard items"));
        }

        [Fact]
        public async Task CreateDashboardFromDocument_BuildsTheWholeBoard()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docnewboard");

            await CreateCapableTracker(client, "Workouts");

            var response = await PostJson(client, $$"""
                { "schemaVersion": 3, "board": { "name": "Generated", "color": "grape" },
                  "items": [ {{GoalWithFilterItems}}, { "key": "title", "type": "header", "text": "Hello" } ] }
                """);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var created = await Data(response);
            var newId = created.GetProperty("id").GetString()!;
            Assert.Equal("Generated", created.GetProperty("name").GetString());
            Assert.Equal("grape", created.GetProperty("color").GetString());
            Assert.Equal(3, (await Widgets(client, newId)).GetArrayLength());
        }

        [Fact]
        public async Task CreateDashboardFromDocument_InvalidDocument_LeavesNoBoardBehind()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docnewbad");

            var before = (await Data(await client.GetAsync("dashboard"))).GetArrayLength();

            var response = await PostJson(client, Board("""{ "key": "add", "type": "quickAdd", "wiring": { "trackerName": "Nowhere" } }"""));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("no tracker named \"Nowhere\""));

            Assert.Equal(before, (await Data(await client.GetAsync("dashboard"))).GetArrayLength());
        }

        private static readonly System.Text.RegularExpressions.Regex AnyGuid =
            new("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

        [Fact]
        public async Task GetDashboardDocument_ContainsNoIds()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("docnoids");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);

            await AddLineItem(client, dashboardId, tracker);
            await AddTabsContainer(client, dashboardId);
            await PlaceEntriesTable(client, dashboardId, tracker, [tracker.DayFieldId]);

            await AssertSaved(await PutJson(client, dashboardId, Board(
                GoalWithFilterItems + """
                , { "key": "inner", "type": "header", "text": "Inside", "parent": "group" }
                , { "key": "group", "type": "container", "text": "Group" }
                """,
                """ "presets": [ { "name": "Big", "clauses": [ { "kind": "filter", "dataType": "number", "operator": "Greater Than", "value": "3" } ] } ] """)));

            var response = await client.GetAsync($"dashboard/{dashboardId}/document");
            var raw = (await Data(response)).GetRawText();

            Assert.DoesNotMatch(AnyGuid, raw);
        }

        [Fact]
        public async Task GetDashboardDocument_GivesKeysOnceAndKeepsThem()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("dockeys");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            await AddLineItem(client, dashboardId, tracker);

            var first = (await GetDocument(client, dashboardId)).Items.Select(i => i.Key).ToList();
            Assert.Equal(first, (await GetDocument(client, dashboardId)).Items.Select(i => i.Key).ToList());

            await AddLineItem(client, dashboardId, tracker);

            var after = (await GetDocument(client, dashboardId)).Items.Select(i => i.Key).ToList();
            Assert.Equal(2, after.Distinct().Count());
            Assert.Contains(first[0], after);
            Assert.All(after, key => Assert.Matches("^[a-z0-9-]+$", key));
        }

        [Fact]
        public async Task SaveDashboardDocument_KeyWithSpaces_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("dockeybad");

            var dashboardId = await CreateDashboard(client);

            var response = await PutJson(client, dashboardId, Board("""{ "key": "my header", "type": "header", "text": "Hi" }"""));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("items[0].key") && m.Contains("letters, digits"));
        }

        [Fact]
        public async Task SaveDashboardDocument_RenamedTab_KeepsItsChildren()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("doctabrename");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var (containerId, _) = await AddTabsContainer(client, dashboardId);
            await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{containerId}/tabs-container", new SaveTabsContainerDto
            {
                Tabs = [new SaveTabDto { Name = "First" }, new SaveTabDto { Name = "Second" }]
            });
            var tabIds = await TabIds(client, dashboardId, containerId);
            var itemId = await AddLineItem(client, dashboardId, tracker);

            var document = await GetDocument(client, dashboardId);
            var child = DocItem(document, itemId);
            child.Parent = KeyOf(containerId);
            child.Tab = "First";
            await AssertSaved(await PutDocument(client, dashboardId, document));

            document = await GetDocument(client, dashboardId);
            DocItem(document, containerId).Tabs = new Optional<List<string>>(["Renamed", "Second"]);
            DocItem(document, itemId).Tab = "Renamed";
            await AssertSaved(await PutDocument(client, dashboardId, document));

            Assert.Equal(tabIds, await TabIds(client, dashboardId, containerId));
            Assert.Equal(tabIds[0], WidgetById(await Widgets(client, dashboardId), itemId).GetProperty("parentTabId").GetString());
        }
    }
}

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
        public async Task CreateAndPlaceWidget_GoalLowerIsBetter_RendersPercentOfCapUsed()
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

            // LowerIsBetter uses the same value/target ratio as HigherIsBetter; the direction
            // only changes whether being at or under 1 counts as "achieved".
            var analytic = Analytic((await Widgets(client, dashboardId))[0]);
            Assert.Equal(GoalDirections.LowerIsBetter, analytic.GetProperty("direction").GetString());
            Assert.Equal(0.5, analytic.GetProperty("progress").GetDouble(), 3);
        }

        [Fact]
        public async Task CreateAndPlaceWidget_GoalLowerIsBetter_OverTheCapReadsAboveOne()
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

            // Over budget: the ratio rises above 1, unlike HigherIsBetter where above 1 means
            // the target was exceeded in a good way. The frontend flips "achieved" for this direction.
            var analytic = Analytic((await Widgets(client, dashboardId))[0]);
            var progress = analytic.GetProperty("progress").GetDouble();
            Assert.True(progress > 1, $"Expected progress above 1 once over the cap, got {progress}");
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

        [Fact]
        public async Task UpdateDashboardItem_CalendarStartMonth_IsStampedOnTheCalendar()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("itemcalstart");

            var tracker = await CreateCapableTracker(client, "Workouts");
            var dashboardId = await CreateDashboard(client);
            var addResponse = await client.PostAsJsonAsync($"dashboard/{dashboardId}/items", new CreateAndPlaceWidgetDto
            {
                ResultType = AnalyticTypes.Calendar,
                Code = AnalyticCodes.Calendar,
                Sources = [CalendarSource(tracker)]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
            var itemId = (await Data(addResponse)).GetProperty("id").GetString()!;

            var placed = await Widgets(client, dashboardId);
            Assert.True(!Analytic(placed[0]).TryGetProperty("startMonth", out var initial)
                || initial.ValueKind == JsonValueKind.Null);

            var sourceId = await SingleSourceId(client, dashboardId, itemId);
            var updateResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{itemId}", new UpdateDashboardItemDto
            {
                CalendarStartMonth = CalendarStartMonths.NextUpcoming,
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId }]
            });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var widgets = await Widgets(client, dashboardId);
            Assert.Equal(CalendarStartMonths.NextUpcoming, Analytic(widgets[0]).GetProperty("startMonth").GetString());
        }

        [Fact]
        public async Task UpdateDashboardItem_UnknownCalendarStartMonth_IsRejected()
        {
            await _factory.SeedDatabaseAsync();
            var client = await _factory.NewUserClient("itemcalbad");

            var tracker = await CreateCapableTracker(client, "Weight");
            var dashboardId = await CreateDashboard(client);
            var itemId = await AddLineItem(client, dashboardId, tracker);
            var sourceId = await SingleSourceId(client, dashboardId, itemId);

            var updateResponse = await client.PutAsJsonAsync($"dashboard/{dashboardId}/items/{itemId}", new UpdateDashboardItemDto
            {
                CalendarStartMonth = "Sometime",
                Sources = [new UpdateDashboardItemSourceDto { SourceId = sourceId }]
            });
            Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
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
    }
}

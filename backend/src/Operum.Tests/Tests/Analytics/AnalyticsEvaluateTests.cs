using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Analytics
{
    public class AnalyticsEvaluateTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("evaluate-owner");
        private Task<HttpClient> OtherClient() => _factory.NewUserClient("evaluate-other");

        // A tracker with an "Amount" number field and a "Location" string field, plus three
        // entries: two tagged "remote", one "office".
        private static async Task<(string trackerId, string amountId, string locationId)> SeedTracker(HttpClient client)
        {
            var trackerId = await TestApi.CreateTracker(client, "Hours");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var locationId = await TestApi.CreateField(client, trackerId, "Location", DataTypes.String);

            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "5", ["Location"] = "remote" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "3", ["Location"] = "remote" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "8", ["Location"] = "office" });

            return (trackerId, amountId, locationId);
        }

        // A tracker with a "Day" date field and an "Amount" number field, one entry per given
        // (day, amount) pair -- the shape a line chart or a correlation source reads.
        private static async Task<(string trackerId, string dayId, string amountId)> SeedDatedTracker(
            HttpClient client, string name, params (string day, string amount)[] entries)
        {
            var trackerId = await TestApi.CreateTracker(client, name);
            var dayId = await TestApi.CreateField(client, trackerId, "Day", DataTypes.Date);
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);

            foreach (var (day, amount) in entries)
                await TestApi.CreateEntry(client, trackerId, new() { ["Day"] = day, ["Amount"] = amount });

            return (trackerId, dayId, amountId);
        }

        private static EvaluateSourceDto Source(string trackerId, params CreateAnalyticFieldDto[] fields) => new()
        {
            TrackerId = trackerId,
            Fields = [.. fields]
        };

        private static EvaluateWidgetDto CountDto(string trackerId, string fieldId) => new()
        {
            ResultType = AnalyticTypes.SingleValue,
            Code = AnalyticCodes.Count,
            Sources = [Source(trackerId, new CreateAnalyticFieldDto { FieldId = fieldId, Purpose = AnalyticPurposes.Value })]
        };

        private static CreateAnalyticFieldDto Field(string fieldId, string purpose) =>
            new() { FieldId = fieldId, Purpose = purpose };

        [Fact]
        public async Task Evaluate_SingleValueCount_CountsEveryEntry()
        {
            var client = await OwnerClient();
            var (trackerId, amountId, _) = await SeedTracker(client);

            var response = await client.PostAsJsonAsync("analytics/evaluate", CountDto(trackerId, amountId));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await TestApi.Data(response);
            Assert.Equal("3", result.GetProperty("value").GetString());
        }

        [Fact]
        public async Task Evaluate_WithInlineFilter_NarrowsToMatchingEntries()
        {
            var client = await OwnerClient();
            var (trackerId, amountId, locationId) = await SeedTracker(client);

            var dto = CountDto(trackerId, amountId);
            dto.Sources[0].Filters =
            [
                new EvaluateFilterClauseDto
                {
                    FieldId = locationId,
                    Operator = OperatorTypes.EqualsOperator,
                    Value = "remote"
                }
            ];

            var response = await client.PostAsJsonAsync("analytics/evaluate", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await TestApi.Data(response);
            Assert.Equal("2", result.GetProperty("value").GetString());
        }

        [Fact]
        public async Task Evaluate_TrackerNotAccessible_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var (trackerId, amountId, _) = await SeedTracker(owner);

            var stranger = await OtherClient();
            var response = await stranger.PostAsJsonAsync("analytics/evaluate", CountDto(trackerId, amountId));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Evaluate_CodeDoesNotBelongToResultType_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var (trackerId, amountId, _) = await SeedTracker(client);

            var dto = CountDto(trackerId, amountId);
            // A bar chart code the single-value definition knows nothing about.
            dto.Code = AnalyticCodes.CountBarChart;

            var response = await client.PostAsJsonAsync("analytics/evaluate", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Evaluate_CorrelationScatter_TwoSources_PairsOnMatchKey()
        {
            var client = await OwnerClient();
            var weight = await SeedDatedTracker(client, "Weight",
                ("2026-01-01", "5"), ("2026-01-02", "6"), ("2026-01-03", "7"));
            var sleep = await SeedDatedTracker(client, "Sleep",
                ("2026-01-01", "5"), ("2026-01-02", "8"), ("2026-01-09", "9"));

            var dto = new EvaluateWidgetDto
            {
                ResultType = AnalyticTypes.ScatterChart,
                Code = AnalyticCodes.CorrelationScatter,
                Sources =
                [
                    Source(weight.trackerId, Field(weight.dayId, AnalyticPurposes.Match), Field(weight.amountId, AnalyticPurposes.Value)),
                    Source(sleep.trackerId, Field(sleep.dayId, AnalyticPurposes.Match), Field(sleep.amountId, AnalyticPurposes.Value))
                ]
            };

            var response = await client.PostAsJsonAsync("analytics/evaluate", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await TestApi.Data(response);
            Assert.Equal(AnalyticTypes.ScatterChart, result.GetProperty("resultType").GetString());

            var points = result.GetProperty("points").EnumerateArray()
                .Select(p => (X: p.GetProperty("x").GetDouble(), Y: p.GetProperty("y").GetDouble()))
                .OrderBy(p => p.X)
                .ToList();

            // Only 2026-01-01 (5, 5) and 2026-01-02 (6, 8) appear on both trackers.
            Assert.Equal(2, points.Count);
            Assert.Equal((5, 5), points[0]);
            Assert.Equal((6, 8), points[1]);
            Assert.Equal("Weight: Amount", result.GetProperty("xField").GetProperty("name").GetString());
            Assert.Equal("Sleep: Amount", result.GetProperty("yField").GetProperty("name").GetString());
        }

        [Fact]
        public async Task Evaluate_CorrelationScatter_NotExactlyTwoSources_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var weight = await SeedDatedTracker(client, "Weight", ("2026-01-01", "5"));

            var dto = new EvaluateWidgetDto
            {
                ResultType = AnalyticTypes.ScatterChart,
                Code = AnalyticCodes.CorrelationScatter,
                Sources =
                [
                    Source(weight.trackerId, Field(weight.dayId, AnalyticPurposes.Match), Field(weight.amountId, AnalyticPurposes.Value))
                ]
            };

            var response = await client.PostAsJsonAsync("analytics/evaluate", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Evaluate_MultiSourceLineChart_MergesIntoComposed()
        {
            var client = await OwnerClient();
            var weight = await SeedDatedTracker(client, "Weight", ("2026-01-01", "5"), ("2026-01-02", "6"));
            var steps = await SeedDatedTracker(client, "Steps", ("2026-01-01", "9000"), ("2026-01-02", "8000"));

            var dto = new EvaluateWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources =
                [
                    Source(weight.trackerId, Field(weight.dayId, AnalyticPurposes.Xaxis), Field(weight.amountId, AnalyticPurposes.Yaxis)),
                    Source(steps.trackerId, Field(steps.dayId, AnalyticPurposes.Xaxis), Field(steps.amountId, AnalyticPurposes.Yaxis))
                ]
            };

            var response = await client.PostAsJsonAsync("analytics/evaluate", dto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await TestApi.Data(response);
            Assert.Equal(AnalyticTypes.Composed, result.GetProperty("resultType").GetString());
            Assert.Equal(2, result.GetProperty("series").GetArrayLength());
        }

        [Fact]
        public async Task Evaluate_MultiSourceSingleValue_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var (trackerA, amountA, _) = await SeedTracker(client);
            var (trackerB, amountB, _) = await SeedTracker(client);

            var dto = new EvaluateWidgetDto
            {
                ResultType = AnalyticTypes.SingleValue,
                Code = AnalyticCodes.Count,
                Sources =
                [
                    Source(trackerA, Field(amountA, AnalyticPurposes.Value)),
                    Source(trackerB, Field(amountB, AnalyticPurposes.Value))
                ]
            };

            var response = await client.PostAsJsonAsync("analytics/evaluate", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Evaluate_SecondSourceNotAccessible_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var mine = await SeedDatedTracker(owner, "Mine", ("2026-01-01", "5"));

            var stranger = await OtherClient();
            var theirs = await SeedDatedTracker(stranger, "Theirs", ("2026-01-01", "5"));

            var dto = new EvaluateWidgetDto
            {
                ResultType = AnalyticTypes.LineChart,
                Code = AnalyticCodes.LineChart,
                Sources =
                [
                    Source(mine.trackerId, Field(mine.dayId, AnalyticPurposes.Xaxis), Field(mine.amountId, AnalyticPurposes.Yaxis)),
                    Source(theirs.trackerId, Field(theirs.dayId, AnalyticPurposes.Xaxis), Field(theirs.amountId, AnalyticPurposes.Yaxis))
                ]
            };

            var response = await owner.PostAsJsonAsync("analytics/evaluate", dto);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}

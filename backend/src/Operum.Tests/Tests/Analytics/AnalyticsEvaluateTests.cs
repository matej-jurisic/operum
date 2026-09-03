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

        private static EvaluateWidgetDto CountDto(string trackerId, string fieldId) => new()
        {
            ResultType = AnalyticTypes.SingleValue,
            Code = AnalyticCodes.Count,
            TrackerId = trackerId,
            Fields = [new CreateAnalyticFieldDto { FieldId = fieldId, Purpose = AnalyticPurposes.Value }]
        };

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
            dto.Filters =
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
        public async Task Evaluate_CorrelationScatter_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var (trackerId, amountId, _) = await SeedTracker(client);

            // A correlation pairs two trackers, one per axis -- it has no single-tracker
            // shape for Explore to evaluate.
            var dto = new EvaluateWidgetDto
            {
                ResultType = AnalyticTypes.ScatterChart,
                Code = AnalyticCodes.CorrelationScatter,
                TrackerId = trackerId,
                Fields =
                [
                    new CreateAnalyticFieldDto { FieldId = amountId, Purpose = AnalyticPurposes.Match },
                    new CreateAnalyticFieldDto { FieldId = amountId, Purpose = AnalyticPurposes.Value }
                ]
            };

            var response = await client.PostAsJsonAsync("analytics/evaluate", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}

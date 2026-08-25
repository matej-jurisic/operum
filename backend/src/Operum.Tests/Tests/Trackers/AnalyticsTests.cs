using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Trackers
{
    public class AnalyticsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("analytics-rename");

        private static async Task<(string trackerId, string fieldId, string analyticId)> CreateTrackerWithAnalytic(
            HttpClient client, string? name = null)
        {
            var trackerId = await TestApi.CreateTracker(client, "Analytic naming");
            var fieldId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);

            var addResponse = await client.PostAsJsonAsync($"trackers/{trackerId}/analytics", new CreateAnalyticDto
            {
                Code = AnalyticCodes.Count,
                Type = AnalyticTypes.SingleValue,
                Name = name,
                AnalyticFields =
                [
                    new CreateAnalyticFieldDto { FieldId = fieldId, Purpose = AnalyticPurposes.Value }
                ]
            });
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var analytics = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/analytics"));
            var analyticId = analytics.EnumerateArray().Single().GetProperty("id").GetString()!;

            return (trackerId, fieldId, analyticId);
        }

        private static async Task<string> AnalyticName(HttpClient client, string trackerId, string analyticId)
        {
            var analytics = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/analytics"));
            return analytics.EnumerateArray()
                .Single(a => a.GetProperty("id").GetString() == analyticId)
                .GetProperty("name").GetString()!;
        }

        [Fact]
        public async Task AddAnalytic_WithoutName_FallsBackToTheDefinitionLabel()
        {
            var client = await OwnerClient();
            var (trackerId, _, analyticId) = await CreateTrackerWithAnalytic(client);

            Assert.Equal("Count", await AnalyticName(client, trackerId, analyticId));
        }

        [Fact]
        public async Task AddAnalytic_WithName_UsesItInsteadOfTheDefinitionLabel()
        {
            var client = await OwnerClient();
            var (trackerId, _, analyticId) = await CreateTrackerWithAnalytic(client, "Entries logged");

            Assert.Equal("Entries logged", await AnalyticName(client, trackerId, analyticId));
        }

        [Fact]
        public async Task UpdateAnalytic_RenamesTheAnalytic()
        {
            var client = await OwnerClient();
            var (trackerId, _, analyticId) = await CreateTrackerWithAnalytic(client);

            var response = await client.PutAsJsonAsync(
                $"trackers/{trackerId}/analytics/{analyticId}",
                new UpdateAnalyticDto { Name = "Renamed" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Renamed", await AnalyticName(client, trackerId, analyticId));
        }

        [Fact]
        public async Task UpdateAnalytic_ClearingTheName_FallsBackToTheDefinitionLabel()
        {
            var client = await OwnerClient();
            var (trackerId, _, analyticId) = await CreateTrackerWithAnalytic(client, "Entries logged");

            var response = await client.PutAsJsonAsync(
                $"trackers/{trackerId}/analytics/{analyticId}",
                new UpdateAnalyticDto { Name = null });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Count", await AnalyticName(client, trackerId, analyticId));
        }

        [Fact]
        public async Task UpdateAnalytic_UnknownAnalytic_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Analytic naming");

            var response = await client.PutAsJsonAsync(
                $"trackers/{trackerId}/analytics/{Guid.NewGuid()}",
                new UpdateAnalyticDto { Name = "Renamed" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateAnalytic_NameTooLong_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var (trackerId, _, analyticId) = await CreateTrackerWithAnalytic(client);

            var response = await client.PutAsJsonAsync(
                $"trackers/{trackerId}/analytics/{analyticId}",
                new UpdateAnalyticDto { Name = new string('a', 101) });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}

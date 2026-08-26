using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Widgets.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Widgets
{
    public class WidgetsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("widgets-owner");
        private Task<HttpClient> OtherClient() => _factory.NewUserClient("widgets-other");

        private static async Task<(string trackerId, string fieldId)> CreateTrackerWithField(HttpClient client, string trackerName = "Widget source")
        {
            var trackerId = await TestApi.CreateTracker(client, trackerName);
            var fieldId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            return (trackerId, fieldId);
        }

        private static CreateWidgetDto SingleValueWidgetDto(string trackerId, string fieldId, string? name = null) => new()
        {
            Name = name,
            ResultType = AnalyticTypes.SingleValue,
            Code = AnalyticCodes.Count,
            Sources =
            [
                new CreateWidgetSourceRequestDto
                {
                    TrackerId = trackerId,
                    Fields = [new CreateAnalyticFieldDto { FieldId = fieldId, Purpose = AnalyticPurposes.Value }]
                }
            ]
        };

        [Fact]
        public async Task CreateWidget_WithoutName_FallsBackToTheDefinitionLabel()
        {
            var client = await OwnerClient();
            var (trackerId, fieldId) = await CreateTrackerWithField(client);

            var response = await client.PostAsJsonAsync("widgets", SingleValueWidgetDto(trackerId, fieldId));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var widget = await TestApi.Data(response);
            Assert.Equal("Count", widget.GetProperty("name").GetString());
            Assert.Single(widget.GetProperty("sources").EnumerateArray());
        }

        [Fact]
        public async Task CreateWidget_WithName_UsesItInsteadOfTheDefinitionLabel()
        {
            var client = await OwnerClient();
            var (trackerId, fieldId) = await CreateTrackerWithField(client);

            var response = await client.PostAsJsonAsync("widgets", SingleValueWidgetDto(trackerId, fieldId, "Entries logged"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var widget = await TestApi.Data(response);
            Assert.Equal("Entries logged", widget.GetProperty("name").GetString());
        }

        [Fact]
        public async Task CreateWidget_MissingRequiredField_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var (trackerId, _) = await CreateTrackerWithField(client);

            var dto = new CreateWidgetDto
            {
                ResultType = AnalyticTypes.SingleValue,
                Code = AnalyticCodes.Count,
                Sources = [new CreateWidgetSourceRequestDto { TrackerId = trackerId, Fields = [] }]
            };

            var response = await client.PostAsJsonAsync("widgets", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateWidget_MultiSourceSingleValue_IsRejected()
        {
            // Combining is only meaningful for line/bar charts -- see WidgetsService.CreateWidget.
            var client = await OwnerClient();
            var (trackerA, fieldA) = await CreateTrackerWithField(client, "Tracker A");
            var (trackerB, fieldB) = await CreateTrackerWithField(client, "Tracker B");

            var dto = new CreateWidgetDto
            {
                ResultType = AnalyticTypes.SingleValue,
                Code = AnalyticCodes.Count,
                Sources =
                [
                    new CreateWidgetSourceRequestDto { TrackerId = trackerA, Fields = [new CreateAnalyticFieldDto { FieldId = fieldA, Purpose = AnalyticPurposes.Value }] },
                    new CreateWidgetSourceRequestDto { TrackerId = trackerB, Fields = [new CreateAnalyticFieldDto { FieldId = fieldB, Purpose = AnalyticPurposes.Value }] }
                ]
            };

            var response = await client.PostAsJsonAsync("widgets", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateWidget_TrackerBelongingToAnotherUser_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var other = await OtherClient();
            var (trackerId, fieldId) = await CreateTrackerWithField(owner);

            var response = await other.PostAsJsonAsync("widgets", SingleValueWidgetDto(trackerId, fieldId));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetWidgets_FilteredByTracker_OnlyReturnsWidgetsWithThatSource()
        {
            var client = await OwnerClient();
            var (trackerA, fieldA) = await CreateTrackerWithField(client, "Tracker A");
            var (trackerB, fieldB) = await CreateTrackerWithField(client, "Tracker B");

            await client.PostAsJsonAsync("widgets", SingleValueWidgetDto(trackerA, fieldA, "Widget A"));
            await client.PostAsJsonAsync("widgets", SingleValueWidgetDto(trackerB, fieldB, "Widget B"));

            var response = await client.GetAsync($"widgets?trackerId={trackerA}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var widgets = await TestApi.Data(response);
            var names = widgets.EnumerateArray().Select(w => w.GetProperty("name").GetString()).ToList();
            Assert.Equal(["Widget A"], names);
        }

        [Fact]
        public async Task GetWidgets_AnotherUsersWidget_IsNotVisible()
        {
            var owner = await OwnerClient();
            var other = await OtherClient();
            var (trackerId, fieldId) = await CreateTrackerWithField(owner);

            await owner.PostAsJsonAsync("widgets", SingleValueWidgetDto(trackerId, fieldId, "Owner's widget"));

            var response = await other.GetAsync("widgets");
            var widgets = await TestApi.Data(response);
            Assert.Empty(widgets.EnumerateArray());
        }

        [Fact]
        public async Task UpdateWidget_RenamesTheWidget()
        {
            var client = await OwnerClient();
            var (trackerId, fieldId) = await CreateTrackerWithField(client);
            var widgetId = await TestApi.IdOf(await client.PostAsJsonAsync("widgets", SingleValueWidgetDto(trackerId, fieldId)));

            var response = await client.PutAsJsonAsync($"widgets/{widgetId}", new UpdateWidgetDto { Name = "Renamed" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var widget = await TestApi.Data(response);
            Assert.Equal("Renamed", widget.GetProperty("name").GetString());
        }

        [Fact]
        public async Task UpdateWidget_AnotherUsersWidget_ReturnsNotFound()
        {
            var owner = await OwnerClient();
            var other = await OtherClient();
            var (trackerId, fieldId) = await CreateTrackerWithField(owner);
            var widgetId = await TestApi.IdOf(await owner.PostAsJsonAsync("widgets", SingleValueWidgetDto(trackerId, fieldId)));

            var response = await other.PutAsJsonAsync($"widgets/{widgetId}", new UpdateWidgetDto { Name = "Hijacked" });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteWidget_RemovesItFromTheLibrary()
        {
            var client = await OwnerClient();
            var (trackerId, fieldId) = await CreateTrackerWithField(client);
            var widgetId = await TestApi.IdOf(await client.PostAsJsonAsync("widgets", SingleValueWidgetDto(trackerId, fieldId)));

            var deleteResponse = await client.DeleteAsync($"widgets/{widgetId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            var getResponse = await client.GetAsync($"widgets/{widgetId}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task CreateEntriesWidget_ThenListedByTracker()
        {
            var client = await OwnerClient();
            var (trackerId, _) = await CreateTrackerWithField(client);

            var response = await client.PostAsJsonAsync("widgets/entries", new CreateEntriesWidgetDto { TrackerId = trackerId, Name = "All entries" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var listResponse = await client.GetAsync($"widgets/entries?trackerId={trackerId}");
            var widgets = await TestApi.Data(listResponse);
            Assert.Single(widgets.EnumerateArray());
            Assert.Equal("All entries", widgets.EnumerateArray().Single().GetProperty("name").GetString());
        }

        [Fact]
        public async Task CreateEntriesWidget_TrackerBelongingToAnotherUser_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var other = await OtherClient();
            var (trackerId, _) = await CreateTrackerWithField(owner);

            var response = await other.PostAsJsonAsync("widgets/entries", new CreateEntriesWidgetDto { TrackerId = trackerId });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEntriesWidget_RemovesItFromTheLibrary()
        {
            var client = await OwnerClient();
            var (trackerId, _) = await CreateTrackerWithField(client);
            var widgetId = await TestApi.IdOf(await client.PostAsJsonAsync("widgets/entries", new CreateEntriesWidgetDto { TrackerId = trackerId }));

            var deleteResponse = await client.DeleteAsync($"widgets/entries/{widgetId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            var getResponse = await client.GetAsync($"widgets/entries/{widgetId}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }
    }
}

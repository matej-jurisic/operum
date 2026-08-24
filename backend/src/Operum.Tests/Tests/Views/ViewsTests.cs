using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Views.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Views
{
    public class ViewsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("views");

        private sealed record Fixture(string TrackerId, string AmountFieldId, string NoteFieldId, string DayFieldId);

        private static async Task<Fixture> CreateTracker(HttpClient client, string name)
        {
            var trackerId = await TestApi.CreateTracker(client, name);
            return new Fixture(
                trackerId,
                await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number),
                await TestApi.CreateField(client, trackerId, "Note", DataTypes.String),
                await TestApi.CreateField(client, trackerId, "Day", DataTypes.Date));
        }

        private static async Task<List<string>> ViewNames(HttpClient client, string trackerId)
        {
            var data = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/views"));
            return [.. data.EnumerateArray().Select(v => v.GetProperty("name").GetString()!)];
        }

        private static Task<HttpResponseMessage> PutView(HttpClient client, string trackerId, string viewId, UpdateViewDto view) =>
            client.PutAsJsonAsync($"trackers/{trackerId}/views/{viewId}", view);

        [Fact]
        public async Task CreateView_StoresItsFiltersAndSorts()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Create view");

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Big first",
                Description = "The heavy ones",
                Filters = [new() { FieldId = fixture.AmountFieldId, Operator = OperatorTypes.GreaterThan, Value = "5" }],
                Sorts = [new() { FieldId = fixture.AmountFieldId, Descending = true }]
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var view = await TestApi.Data(response);
            Assert.Equal("Big first", view.GetProperty("name").GetString());
            var filter = view.GetProperty("filters").EnumerateArray().Single();
            Assert.Equal(OperatorTypes.GreaterThan, filter.GetProperty("operator").GetString());
            Assert.Equal("5", filter.GetProperty("value").GetString());
            Assert.Equal("Amount", filter.GetProperty("field").GetProperty("name").GetString());
            var sort = view.GetProperty("sorts").EnumerateArray().Single();
            Assert.True(sort.GetProperty("descending").GetBoolean());
        }

        [Fact]
        public async Task CreateView_WithoutFiltersOrSorts_IsAccepted()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Plain view");

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto { Name = "Everything" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(["Everything"], await ViewNames(client, fixture.TrackerId));
        }

        [Fact]
        public async Task CreateView_FilterOnAFieldOfAnotherTracker_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "View owner");
            var other = await CreateTracker(client, "Field owner");

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Foreign filter",
                Filters = [new() { FieldId = other.AmountFieldId, Operator = OperatorTypes.GreaterThan, Value = "5" }]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.ItemNotFound("filter field"), await TestApi.Messages(response));
        }

        [Fact]
        public async Task CreateView_SortOnAFieldOfAnotherTracker_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Sort view owner");
            var other = await CreateTracker(client, "Sort field owner");

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Foreign sort",
                Sorts = [new() { FieldId = other.AmountFieldId }]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.ItemNotFound("sort field"), await TestApi.Messages(response));
        }

        [Theory]
        [InlineData(OperatorTypes.Contains)]
        [InlineData(OperatorTypes.StartsWith)]
        [InlineData(OperatorTypes.EndsWith)]
        public async Task CreateView_TextOperatorOnANumberField_ReturnsBadRequest(string op)
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, $"Bad operator {op}");

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Wrong operator",
                Filters = [new() { FieldId = fixture.AmountFieldId, Operator = op, Value = "5" }]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateView_OrderingOperatorOnAStringField_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "String ordering");

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Wrong operator",
                Filters = [new() { FieldId = fixture.NoteFieldId, Operator = OperatorTypes.GreaterThan, Value = "a" }]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateView_UnknownOperator_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Unknown operator");

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Nonsense",
                Filters = [new() { FieldId = fixture.AmountFieldId, Operator = "Sort of equals", Value = "5" }]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateView_ValueThatTheFieldTypeCannotHold_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Bad value");

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Not a number",
                Filters = [new() { FieldId = fixture.AmountFieldId, Operator = OperatorTypes.EqualsOperator, Value = "lots" }]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateView_DynamicDateTokenAsAFilterValue_IsAccepted()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Dynamic date");

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "This month",
                Filters = [new() { FieldId = fixture.DayFieldId, Operator = OperatorTypes.GreaterThanOrEqual, Value = "start_of_month" }]
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateView_FilterWithoutAValue_IsAccepted()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Null filter");

            // A null value is how "has no value" is expressed.
            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Unset",
                Filters = [new() { FieldId = fixture.AmountFieldId, Operator = OperatorTypes.EqualsOperator, Value = null }]
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateView_MoreSortsThanTheLimit_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Too many sorts");
            var fourth = await TestApi.CreateField(client, fixture.TrackerId, "Extra", DataTypes.String);

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Sorted to death",
                Sorts =
                [
                    new() { FieldId = fixture.AmountFieldId },
                    new() { FieldId = fixture.NoteFieldId },
                    new() { FieldId = fixture.DayFieldId },
                    new() { FieldId = fourth }
                ]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateView_SameFieldSortedTwice_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Duplicate sort");

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Twice",
                Sorts =
                [
                    new() { FieldId = fixture.AmountFieldId },
                    new() { FieldId = fixture.AmountFieldId, Descending = true }
                ]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateView_MoreFiltersThanTheLimit_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Too many filters");

            var filters = Enumerable.Range(0, DataLimits.MaxFilters + 1)
                .Select(i => new CreateViewFilterDto
                {
                    FieldId = fixture.AmountFieldId,
                    Operator = OperatorTypes.GreaterThan,
                    Value = i.ToString()
                }).ToList();
            var response = await TestApi.PostView(client, fixture.TrackerId,
                new CreateViewDto { Name = "Filtered to death", Filters = filters });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateView_MoreViewsThanTheLimit_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "View limit");
            for (var i = 0; i < DataLimits.MaxViewCount; i++)
                await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto { Name = $"View {i}" });

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto { Name = "One too many" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.MaxNumberReached("views", DataLimits.MaxViewCount), await TestApi.Messages(response));
        }

        [Fact]
        public async Task UpdateView_ReplacesTheFiltersAndSorts()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Update view");
            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Original",
                Filters = [new() { FieldId = fixture.AmountFieldId, Operator = OperatorTypes.GreaterThan, Value = "5" }],
                Sorts = [new() { FieldId = fixture.AmountFieldId }]
            });

            var response = await PutView(client, fixture.TrackerId, viewId, new UpdateViewDto
            {
                Name = "Rewritten",
                Filters = [new() { FieldId = fixture.NoteFieldId, Operator = OperatorTypes.Contains, Value = "walk" }],
                Sorts = []
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var view = await TestApi.Data(await client.GetAsync($"trackers/{fixture.TrackerId}/views/{viewId}"));
            Assert.Equal("Rewritten", view.GetProperty("name").GetString());
            Assert.Empty(view.GetProperty("sorts").EnumerateArray());
            var filter = view.GetProperty("filters").EnumerateArray().Single();
            Assert.Equal("Note", filter.GetProperty("field").GetProperty("name").GetString());
            Assert.Equal("walk", filter.GetProperty("value").GetString());
        }

        [Fact]
        public async Task UpdateView_InvalidFilter_ReturnsBadRequestAndKeepsTheOldOne()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Update rejected");
            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Original",
                Filters = [new() { FieldId = fixture.AmountFieldId, Operator = OperatorTypes.GreaterThan, Value = "5" }]
            });

            var response = await PutView(client, fixture.TrackerId, viewId, new UpdateViewDto
            {
                Name = "Original",
                Filters = [new() { FieldId = fixture.AmountFieldId, Operator = OperatorTypes.Contains, Value = "5" }]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var view = await TestApi.Data(await client.GetAsync($"trackers/{fixture.TrackerId}/views/{viewId}"));
            Assert.Equal(OperatorTypes.GreaterThan,
                view.GetProperty("filters").EnumerateArray().Single().GetProperty("operator").GetString());
        }

        [Fact]
        public async Task UpdateView_UnknownView_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Update unknown");

            var response = await PutView(client, fixture.TrackerId, Guid.NewGuid().ToString(),
                new UpdateViewDto { Name = "Ghost" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteView_RemovesIt()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Delete view");
            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto { Name = "Doomed" });

            var response = await client.DeleteAsync($"trackers/{fixture.TrackerId}/views/{viewId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(await ViewNames(client, fixture.TrackerId));
            Assert.Equal(HttpStatusCode.Forbidden,
                (await client.GetAsync($"trackers/{fixture.TrackerId}/views/{viewId}")).StatusCode);
        }

        [Fact]
        public async Task DeleteView_UnknownView_ReturnsOk()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Delete unknown view");

            // The delete only checks the caller's rights over the tracker, so a view that was
            // never there reports success.
            var response = await client.DeleteAsync($"trackers/{fixture.TrackerId}/views/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetView_OfAnotherTracker_ReturnsForbidden()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "View home");
            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto { Name = "Home view" });
            var otherTrackerId = await TestApi.CreateTracker(client, "View elsewhere");

            var response = await client.GetAsync($"trackers/{otherTrackerId}/views/{viewId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ReorderViews_AppliesTheGivenOrder()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Reorder views");
            var firstId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto { Name = "First" });
            var secondId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto { Name = "Second" });
            var thirdId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto { Name = "Third" });

            var response = await client.PutAsJsonAsync($"trackers/{fixture.TrackerId}/views/reorder",
                new ReorderViewsDto { ViewIds = [thirdId, firstId, secondId] });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(["Third", "First", "Second"], await ViewNames(client, fixture.TrackerId));
        }

        [Fact]
        public async Task ReorderViews_PartialList_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Reorder partial");
            var firstId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto { Name = "First" });
            await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto { Name = "Second" });

            var response = await client.PutAsJsonAsync($"trackers/{fixture.TrackerId}/views/reorder",
                new ReorderViewsDto { ViewIds = [firstId] });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(["First", "Second"], await ViewNames(client, fixture.TrackerId));
        }

        [Fact]
        public async Task ReadingViews_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var fixture = await CreateTracker(owner, "Read guard");
            var viewId = await TestApi.CreateView(owner, fixture.TrackerId, new CreateViewDto { Name = "Private" });

            var stranger = await _factory.NewUserClient("viewreader");
            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"trackers/{fixture.TrackerId}/views")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"trackers/{fixture.TrackerId}/views/{viewId}")).StatusCode);
        }

        [Fact]
        public async Task WritingViews_OnATrackerOwnedBySomeoneElse_ReturnsNotFound()
        {
            var owner = await OwnerClient();
            var fixture = await CreateTracker(owner, "Write guard");
            var viewId = await TestApi.CreateView(owner, fixture.TrackerId, new CreateViewDto { Name = "Private" });

            var stranger = await _factory.NewUserClient("viewwriter");
            Assert.Equal(HttpStatusCode.NotFound,
                (await TestApi.PostView(stranger, fixture.TrackerId, new CreateViewDto { Name = "Sneak" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await PutView(stranger, fixture.TrackerId, viewId, new UpdateViewDto { Name = "Sneak" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await stranger.DeleteAsync($"trackers/{fixture.TrackerId}/views/{viewId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await stranger.PutAsJsonAsync($"trackers/{fixture.TrackerId}/views/reorder",
                    new ReorderViewsDto { ViewIds = [viewId] })).StatusCode);

            Assert.Equal(["Private"], await ViewNames(owner, fixture.TrackerId));
        }
    }
}

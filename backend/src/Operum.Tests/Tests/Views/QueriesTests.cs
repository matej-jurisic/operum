using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Queries.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Views
{
    public class QueriesTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("queries");

        private sealed record Fixture(string TrackerId, string AmountFieldId, string NoteFieldId);

        private static async Task<Fixture> CreateTracker(HttpClient client, string name)
        {
            var trackerId = await TestApi.CreateTracker(client, name);
            return new Fixture(
                trackerId,
                await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number),
                await TestApi.CreateField(client, trackerId, "Note", DataTypes.String));
        }

        private static Task<HttpResponseMessage> PutQuery(HttpClient client, string trackerId, string queryId, UpdateQueryDto query) =>
            client.PutAsJsonAsync($"trackers/{trackerId}/queries/{queryId}", query);

        private static async Task<List<JsonElement>> QueryList(HttpClient client, string trackerId)
        {
            var data = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/queries"));
            return [.. data.EnumerateArray()];
        }

        [Fact]
        public async Task CreateQuery_Filter_StoresTheClause()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Create filter");

            var response = await TestApi.PostQuery(client, fixture.TrackerId,
                TestApi.FilterClause(fixture.AmountFieldId, OperatorTypes.GreaterThan, "5"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var query = await TestApi.Data(response);
            Assert.Equal(QueryKinds.Filter, query.GetProperty("kind").GetString());
            Assert.Equal("Amount", query.GetProperty("field").GetProperty("name").GetString());
            Assert.Equal(OperatorTypes.GreaterThan, query.GetProperty("operator").GetString());
            Assert.Equal("5", query.GetProperty("value").GetString());
        }

        [Fact]
        public async Task CreateQuery_Sort_StoresTheClauseWithoutAnOperator()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Create sort");

            var response = await TestApi.PostQuery(client, fixture.TrackerId,
                TestApi.SortClause(fixture.AmountFieldId, descending: true));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var query = await TestApi.Data(response);
            Assert.Equal(QueryKinds.Sort, query.GetProperty("kind").GetString());
            Assert.True(query.GetProperty("descending").GetBoolean());
            Assert.Equal(JsonValueKind.Null, query.GetProperty("operator").ValueKind);
        }

        [Fact]
        public async Task CreateQuery_SortCarryingAnOperator_DropsIt()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Sort with operator");

            // The half of the clause the kind does not use is blanked rather than kept.
            var response = await TestApi.PostQuery(client, fixture.TrackerId, new CreateQueryDto
            {
                Kind = QueryKinds.Sort,
                FieldId = fixture.AmountFieldId,
                Operator = OperatorTypes.GreaterThan,
                Value = "5",
                Descending = true
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var query = await TestApi.Data(response);
            Assert.Equal(JsonValueKind.Null, query.GetProperty("operator").ValueKind);
            Assert.Equal(JsonValueKind.Null, query.GetProperty("value").ValueKind);
        }

        [Fact]
        public async Task CreateQuery_FilterWithoutAnOperator_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Operatorless filter");

            var response = await TestApi.PostQuery(client, fixture.TrackerId, new CreateQueryDto
            {
                Kind = QueryKinds.Filter,
                FieldId = fixture.AmountFieldId
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateQuery_UnknownKind_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Unknown kind");

            var response = await TestApi.PostQuery(client, fixture.TrackerId, new CreateQueryDto
            {
                Kind = "group",
                FieldId = fixture.AmountFieldId
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateQuery_OnAFieldOfAnotherTracker_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Query owner");
            var other = await CreateTracker(client, "Field owner");

            var response = await TestApi.PostQuery(client, fixture.TrackerId,
                TestApi.FilterClause(other.AmountFieldId, OperatorTypes.GreaterThan, "5"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.ItemNotFound("filter field"), await TestApi.Messages(response));
        }

        [Fact]
        public async Task CreateQuery_OperatorTheFieldTypeRejects_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Bad operator");

            var response = await TestApi.PostQuery(client, fixture.TrackerId,
                TestApi.FilterClause(fixture.AmountFieldId, OperatorTypes.Contains, "5"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateQuery_MoreQueriesThanTheLimit_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Query limit");
            for (var i = 0; i < DataLimits.MaxQueryCount; i++)
                await TestApi.CreateFilterQuery(client, fixture.TrackerId, fixture.AmountFieldId, OperatorTypes.GreaterThan, i.ToString());

            var response = await TestApi.PostQuery(client, fixture.TrackerId,
                TestApi.FilterClause(fixture.AmountFieldId, OperatorTypes.GreaterThan, "9999"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.MaxNumberReached("queries", DataLimits.MaxQueryCount), await TestApi.Messages(response));
        }

        [Fact]
        public async Task GetQueryList_ReturnsTheTrackersQueries()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "List queries");
            await TestApi.CreateFilterQuery(client, fixture.TrackerId, fixture.AmountFieldId, OperatorTypes.GreaterThan, "5");
            await TestApi.CreateSortQuery(client, fixture.TrackerId, fixture.NoteFieldId, descending: true);

            var queries = await QueryList(client, fixture.TrackerId);

            Assert.Equal(2, queries.Count);
            Assert.Equal([QueryKinds.Filter, QueryKinds.Sort],
                queries.Select(q => q.GetProperty("kind").GetString()));
        }

        [Fact]
        public async Task UpdateQuery_EditsTheClauseEveryViewSees()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Update query");
            var queryId = await TestApi.CreateFilterQuery(client, fixture.TrackerId, fixture.AmountFieldId, OperatorTypes.GreaterThan, "5");
            var viewId = await TestApi.CreateViewFromQueries(client, fixture.TrackerId, "Big", queryId);

            var response = await PutQuery(client, fixture.TrackerId, queryId, new UpdateQueryDto
            {
                Kind = QueryKinds.Filter,
                FieldId = fixture.AmountFieldId,
                Operator = OperatorTypes.LessThan,
                Value = "3"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var view = await TestApi.Data(await client.GetAsync($"trackers/{fixture.TrackerId}/views/{viewId}"));
            var query = view.GetProperty("queries").EnumerateArray().Single();
            Assert.Equal(queryId, query.GetProperty("id").GetString());
            Assert.Equal(OperatorTypes.LessThan, query.GetProperty("operator").GetString());
            Assert.Equal("3", query.GetProperty("value").GetString());
        }

        [Fact]
        public async Task UpdateQuery_TurningAFilterIntoASort_DropsTheOperatorAndValue()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Filter to sort");
            var queryId = await TestApi.CreateFilterQuery(client, fixture.TrackerId, fixture.AmountFieldId, OperatorTypes.GreaterThan, "5");

            var response = await PutQuery(client, fixture.TrackerId, queryId, new UpdateQueryDto
            {
                Kind = QueryKinds.Sort,
                FieldId = fixture.AmountFieldId,
                Descending = true
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var query = await TestApi.Data(response);
            Assert.Equal(QueryKinds.Sort, query.GetProperty("kind").GetString());
            Assert.True(query.GetProperty("descending").GetBoolean());
            Assert.Equal(JsonValueKind.Null, query.GetProperty("operator").ValueKind);
            Assert.Equal(JsonValueKind.Null, query.GetProperty("value").ValueKind);
        }

        [Fact]
        public async Task UpdateQuery_UnknownQuery_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Update unknown query");

            var response = await PutQuery(client, fixture.TrackerId, Guid.NewGuid().ToString(), new UpdateQueryDto
            {
                Kind = QueryKinds.Sort,
                FieldId = fixture.AmountFieldId
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteQuery_DropsItFromEveryViewButKeepsTheViews()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Delete query");
            var doomed = await TestApi.CreateFilterQuery(client, fixture.TrackerId, fixture.AmountFieldId, OperatorTypes.GreaterThan, "5");
            var kept = await TestApi.CreateSortQuery(client, fixture.TrackerId, fixture.AmountFieldId, descending: true);
            var viewId = await TestApi.CreateViewFromQueries(client, fixture.TrackerId, "Big first", doomed, kept);

            var response = await client.DeleteAsync($"trackers/{fixture.TrackerId}/queries/{doomed}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var view = await TestApi.Data(await client.GetAsync($"trackers/{fixture.TrackerId}/views/{viewId}"));
            var remaining = view.GetProperty("queries").EnumerateArray().Single();
            Assert.Equal(kept, remaining.GetProperty("id").GetString());
        }

        [Fact]
        public async Task DeleteQuery_UnknownQuery_ReturnsOk()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Delete unknown query");

            // Like the view delete, this only checks the caller's rights over the tracker.
            var response = await client.DeleteAsync($"trackers/{fixture.TrackerId}/queries/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DeletingAField_TakesTheQueriesOverItAlong()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Field delete");
            await TestApi.CreateFilterQuery(client, fixture.TrackerId, fixture.AmountFieldId, OperatorTypes.GreaterThan, "5");
            await TestApi.CreateSortQuery(client, fixture.TrackerId, fixture.NoteFieldId, descending: false);

            var response = await client.DeleteAsync($"trackers/{fixture.TrackerId}/fields/{fixture.AmountFieldId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var queries = await QueryList(client, fixture.TrackerId);
            Assert.Equal("Note", Assert.Single(queries).GetProperty("field").GetProperty("name").GetString());
        }

        [Fact]
        public async Task ReadingQueries_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var fixture = await CreateTracker(owner, "Read guard");
            var queryId = await TestApi.CreateFilterQuery(owner, fixture.TrackerId, fixture.AmountFieldId, OperatorTypes.GreaterThan, "5");

            var stranger = await _factory.NewUserClient("queryreader");
            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"trackers/{fixture.TrackerId}/queries")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"trackers/{fixture.TrackerId}/queries/{queryId}")).StatusCode);
        }

        [Fact]
        public async Task WritingQueries_OnATrackerOwnedBySomeoneElse_ReturnsNotFound()
        {
            var owner = await OwnerClient();
            var fixture = await CreateTracker(owner, "Write guard");
            var queryId = await TestApi.CreateFilterQuery(owner, fixture.TrackerId, fixture.AmountFieldId, OperatorTypes.GreaterThan, "5");

            var stranger = await _factory.NewUserClient("querywriter");
            Assert.Equal(HttpStatusCode.NotFound,
                (await TestApi.PostQuery(stranger, fixture.TrackerId,
                    TestApi.FilterClause(fixture.AmountFieldId, OperatorTypes.GreaterThan, "1"))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await PutQuery(stranger, fixture.TrackerId, queryId, new UpdateQueryDto
                {
                    Kind = QueryKinds.Sort,
                    FieldId = fixture.AmountFieldId
                })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await stranger.DeleteAsync($"trackers/{fixture.TrackerId}/queries/{queryId}")).StatusCode);

            Assert.Single(await QueryList(owner, fixture.TrackerId));
        }
    }
}

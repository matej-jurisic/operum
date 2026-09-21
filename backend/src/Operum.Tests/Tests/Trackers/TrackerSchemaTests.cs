using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Views.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Text.Json;

namespace Operum.Tests.Tests.Trackers
{
    public class TrackerSchemaTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private static async Task<List<JsonElement>> GetSchema(HttpClient client)
        {
            var response = await client.GetAsync("trackers/schema");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return [.. (await TestApi.Data(response)).EnumerateArray()];
        }

        private static JsonElement Named(IEnumerable<JsonElement> items, string name) =>
            items.Single(i => i.GetProperty("name").GetString() == name);

        [Fact]
        public async Task GetTrackerSchema_DescribesFieldsAndViewsByName()
        {
            var client = await _factory.NewUserClient("schema");
            var trackerId = await TestApi.CreateTracker(client, "Meals");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number, required: true);
            var dayId = await TestApi.CreateField(client, trackerId, "Day", DataTypes.Date);
            await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Kind",
                Type = DataTypes.String,
                SelectOptions = ["lunch", "dinner"]
            });
            await TestApi.CreateCalculatedField(client, trackerId, "Double", "{Amount} * 2", DataTypes.Number);

            await TestApi.CreateView(client, trackerId, new CreateViewDto
            {
                Name = "Big and recent",
                Description = "Heavy meals",
                ColumnFieldIds = [dayId, amountId],
                Queries =
                [
                    TestApi.FilterClause(amountId, OperatorTypes.GreaterThan, "5"),
                    TestApi.SortClause(dayId, descending: true)
                ]
            });

            var tracker = Named(await GetSchema(client), "Meals");

            var fields = tracker.GetProperty("fields").EnumerateArray().ToList();
            Assert.Equal(["Amount", "Day", "Kind", "Double"], fields.Select(f => f.GetProperty("name").GetString()));

            var amount = Named(fields, "Amount");
            Assert.Equal(DataTypes.Number, amount.GetProperty("type").GetString());
            Assert.True(amount.GetProperty("required").GetBoolean());

            Assert.Equal(
                ["lunch", "dinner"],
                Named(fields, "Kind").GetProperty("selectOptions").EnumerateArray().Select(o => o.GetString()));
            Assert.Equal("{Amount} * 2", Named(fields, "Double").GetProperty("formula").GetString());
            Assert.False(Named(fields, "Day").TryGetProperty("formula", out _));

            var view = Named(tracker.GetProperty("views").EnumerateArray(), "Big and recent");
            Assert.Equal("Heavy meals", view.GetProperty("description").GetString());
            Assert.Equal(["Day", "Amount"], view.GetProperty("columns").EnumerateArray().Select(c => c.GetString()));

            var filter = Assert.Single(view.GetProperty("filters").EnumerateArray());
            Assert.Equal("Amount", filter.GetProperty("field").GetString());
            Assert.Equal(OperatorTypes.GreaterThan, filter.GetProperty("operator").GetString());
            Assert.Equal("5", filter.GetProperty("value").GetString());

            var sort = Assert.Single(view.GetProperty("sorts").EnumerateArray());
            Assert.Equal("Day", sort.GetProperty("field").GetString());
            Assert.True(sort.GetProperty("descending").GetBoolean());
        }

        [Fact]
        public async Task GetTrackerSchema_CarriesNoIdsAndNoEntryData()
        {
            var client = await _factory.NewUserClient("schemaclean");
            var trackerId = await TestApi.CreateTracker(client, "Journal");
            var noteId = await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "a-value-that-must-not-leak" });
            await TestApi.CreateView(client, trackerId, new CreateViewDto { Name = "Everything" });

            var response = await client.GetAsync("trackers/schema");
            var body = await response.Content.ReadAsStringAsync();

            Assert.DoesNotContain("a-value-that-must-not-leak", body);
            Assert.DoesNotContain(trackerId, body);
            Assert.DoesNotContain(noteId, body);
        }

        [Fact]
        public async Task GetTrackerSchema_LeavesOutTrackersOfOtherUsers()
        {
            var owner = await _factory.NewUserClient("schemaowner");
            var other = await _factory.NewUserClient("schemaother");
            await TestApi.CreateTracker(owner, "Private tracker");

            var visibleToOther = await GetSchema(other);

            Assert.DoesNotContain(visibleToOther, t => t.GetProperty("name").GetString() == "Private tracker");
        }
    }
}

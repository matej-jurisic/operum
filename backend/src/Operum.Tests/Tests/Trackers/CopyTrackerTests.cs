using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Views.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Trackers
{
    public class CopyTrackerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private static async Task<List<JsonElement>> ListFields(HttpClient client, string trackerId) =>
            [.. (await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/fields"))).EnumerateArray()];

        private static async Task<List<JsonElement>> ListViews(HttpClient client, string trackerId) =>
            [.. (await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/views"))).EnumerateArray()];

        [Fact]
        public async Task CopyTracker_KeepsSchemaAndDropsEntries()
        {
            var client = await _factory.NewUserClient("copy");
            var trackerId = await TestApi.CreateTracker(client, "Budget");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Kind",
                Type = DataTypes.String,
                SelectOptions = ["Food", "Rent"]
            });
            await TestApi.CreateCalculatedField(client, trackerId, "Double", "{Amount} * 2", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "5", ["Kind"] = "Food" });
            var viewId = await TestApi.CreateView(client, trackerId, new CreateViewDto
            {
                Name = "Big",
                Queries = [TestApi.FilterClause(amountId, OperatorTypes.GreaterThan, "3")],
                ColumnFieldIds = [amountId]
            });

            var response = await client.PostAsync($"trackers/{trackerId}/copy", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var copy = await TestApi.Data(response);
            var copyId = copy.GetProperty("id").GetString()!;
            Assert.NotEqual(trackerId, copyId);
            Assert.Equal("Budget (copy)", copy.GetProperty("name").GetString());

            var fields = await ListFields(client, copyId);
            Assert.Equal(["Amount", "Kind", "Double"], fields.Select(f => f.GetProperty("name").GetString()));
            Assert.NotEqual(amountId, fields[0].GetProperty("id").GetString());
            Assert.Equal(["Food", "Rent"], fields[1].GetProperty("selectOptions").EnumerateArray().Select(o => o.GetString()));
            Assert.True(fields[2].GetProperty("isCalculated").GetBoolean());
            Assert.Equal("{Amount} * 2", fields[2].GetProperty("formula").GetString());

            Assert.Empty(await TestApi.ListEntries(client, copyId));
            Assert.Single(await TestApi.ListEntries(client, trackerId));

            var views = await ListViews(client, copyId);
            var copiedView = Assert.Single(views);
            Assert.Equal("Big", copiedView.GetProperty("name").GetString());
            Assert.NotEqual(viewId, copiedView.GetProperty("id").GetString());
            Assert.Single(copiedView.GetProperty("queries").EnumerateArray());
            Assert.Equal(
                [fields[0].GetProperty("id").GetString()],
                copiedView.GetProperty("columnFieldIds").EnumerateArray().Select(id => id.GetString()));
        }

        [Fact]
        public async Task CopyTracker_CopyIsIndependentOfSource()
        {
            var client = await _factory.NewUserClient("copy");
            var trackerId = await TestApi.CreateTracker(client, "Original");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var copyId = await TestApi.IdOf(await client.PostAsync($"trackers/{trackerId}/copy", null));

            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "1" });

            Assert.Empty(await TestApi.ListEntries(client, copyId));
            Assert.Single(await ListFields(client, trackerId));
        }

        [Fact]
        public async Task CopyTracker_OtherUsersTracker_ReturnsNotFound()
        {
            var owner = await _factory.NewUserClient("copyowner");
            var other = await _factory.NewUserClient("copyother");
            var trackerId = await TestApi.CreateTracker(owner, "Private");

            var response = await other.PostAsync($"trackers/{trackerId}/copy", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CopyTracker_UnknownTracker_ReturnsNotFound()
        {
            var client = await _factory.NewUserClient("copy");

            var response = await client.PostAsync("trackers/does-not-exist/copy", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}

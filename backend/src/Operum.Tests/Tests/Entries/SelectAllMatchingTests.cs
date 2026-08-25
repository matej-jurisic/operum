using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Entries
{
    /// <summary>
    /// Bulk delete and recalculate can act on "everything matching the selected views" instead of
    /// an explicit id list, so the client is not limited to the entries on the current page.
    /// </summary>
    public class SelectAllMatchingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("selectall");

        private static Task<HttpResponseMessage> DeleteEntries(HttpClient client, string trackerId, DeleteEntriesDto body) =>
            client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"trackers/{trackerId}/entries")
            {
                Content = JsonContent.Create(body)
            });

        private static Task<HttpResponseMessage> Recalculate(HttpClient client, string trackerId, RecalculateEntriesDto body) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/entries/recalculate", body);

        [Fact]
        public async Task Delete_SelectAllMatching_WithNoViews_DeletesEveryEntryInTheTracker()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Select all delete");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            foreach (var amount in new[] { "1", "2", "3" })
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = amount });

            var response = await DeleteEntries(client, trackerId, new DeleteEntriesDto { SelectAllMatching = true });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(await TestApi.ListEntries(client, trackerId));
        }

        [Fact]
        public async Task Delete_SelectAllMatching_OnlyDeletesEntriesMatchingTheView()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Select all filtered delete");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "1" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "5" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "9" });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "Big", amountId, OperatorTypes.GreaterThan, "4");

            var response = await DeleteEntries(client, trackerId, new DeleteEntriesDto
            {
                SelectAllMatching = true,
                ViewId = viewId
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(["1"], await TestApi.ListValues(client, trackerId, "Amount"));
        }

        [Fact]
        public async Task Delete_SelectAllMatching_KeepsExcludedEntries()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Select all except");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "1" });
            var keptId = await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "2" });

            var response = await DeleteEntries(client, trackerId, new DeleteEntriesDto
            {
                SelectAllMatching = true,
                ExcludedEntryIds = [keptId]
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var remaining = await TestApi.ListEntries(client, trackerId);
            Assert.Equal(keptId, Assert.Single(remaining).GetProperty("id").GetString());
        }

        [Fact]
        public async Task Delete_SelectAllMatching_WithAnUnknownView_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Select all bad view");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "1" });

            var response = await DeleteEntries(client, trackerId, new DeleteEntriesDto
            {
                SelectAllMatching = true,
                ViewId = Guid.NewGuid().ToString()
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Single(await TestApi.ListEntries(client, trackerId));
        }

        [Fact]
        public async Task Delete_SelectAllMatching_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Select all guard");
            await TestApi.CreateField(owner, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(owner, trackerId, new() { ["Amount"] = "1" });

            var (stranger, _) = await _factory.AuthenticatedClientForNewUser("selectallstranger");
            var response = await DeleteEntries(stranger, trackerId, new DeleteEntriesDto { SelectAllMatching = true });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Single(await TestApi.ListEntries(owner, trackerId));
        }

        [Fact]
        public async Task Recalculate_SelectAllMatching_RefreshesEveryMatchingEntry()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Select all recalculate");
            var baseId = await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);
            var doubledId = await TestApi.CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2", DataTypes.Number);
            var smallId = await TestApi.CreateEntry(client, trackerId, new() { ["Base"] = "1" });
            var bigId = await TestApi.CreateEntry(client, trackerId, new() { ["Base"] = "10" });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "Big", baseId, OperatorTypes.GreaterThan, "5");

            await client.PutAsJsonAsync($"trackers/{trackerId}/fields/{doubledId}",
                new Model.DTOs.Fields.Requests.UpdateFieldDto { Name = "Doubled", Type = DataTypes.Number, IsCalculated = true, Formula = "{Base} * 3" });

            var response = await Recalculate(client, trackerId, new RecalculateEntriesDto
            {
                SelectAllMatching = true,
                ViewId = viewId
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(30, await TestApi.NumberValueOf(client, trackerId, bigId, "Doubled"));
            // Outside the view, so it keeps the value from the old formula.
            Assert.Equal(2, await TestApi.NumberValueOf(client, trackerId, smallId, "Doubled"));
        }

        [Fact]
        public async Task Recalculate_SelectAllMatching_SkipsExcludedEntries()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Select all recalculate except");
            await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);
            var doubledId = await TestApi.CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2", DataTypes.Number);
            var recalculatedId = await TestApi.CreateEntry(client, trackerId, new() { ["Base"] = "1" });
            var excludedId = await TestApi.CreateEntry(client, trackerId, new() { ["Base"] = "1" });

            await client.PutAsJsonAsync($"trackers/{trackerId}/fields/{doubledId}",
                new Model.DTOs.Fields.Requests.UpdateFieldDto { Name = "Doubled", Type = DataTypes.Number, IsCalculated = true, Formula = "{Base} * 3" });

            var response = await Recalculate(client, trackerId, new RecalculateEntriesDto
            {
                SelectAllMatching = true,
                ExcludedEntryIds = [excludedId]
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(3, await TestApi.NumberValueOf(client, trackerId, recalculatedId, "Doubled"));
            Assert.Equal(2, await TestApi.NumberValueOf(client, trackerId, excludedId, "Doubled"));
        }

        [Fact]
        public async Task Recalculate_ExplicitIds_AreNoLongerCapped()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Recalculate uncapped");
            await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);
            await TestApi.CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2", DataTypes.Number);

            var manyIds = Enumerable.Range(0, 200).Select(_ => Guid.NewGuid().ToString()).ToList();
            var response = await Recalculate(client, trackerId, new RecalculateEntriesDto { EntryIds = manyIds });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}

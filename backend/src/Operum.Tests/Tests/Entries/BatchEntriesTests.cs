using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Entries
{
    /// <summary>
    /// The batch endpoint applies creates, updates and deletes in one transaction, and is
    /// deliberately more forgiving about missing fields than the single-entry endpoints.
    /// </summary>
    public class BatchEntriesTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("batch");

        private static Task<HttpResponseMessage> Batch(HttpClient client, string trackerId, BatchEntriesDto batch) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/entries/batch", batch);

        [Fact]
        public async Task Batch_CreatesUpdatesAndDeletesInOneCall()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Batch all three");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var toUpdate = await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "1" });
            var toDelete = await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "2" });

            var response = await Batch(client, trackerId, new BatchEntriesDto
            {
                Creates = [new() { ["Amount"] = "3" }],
                Updates = [new() { EntryId = toUpdate, FieldValues = new() { ["Amount"] = "10" } }],
                Deletes = [toDelete]
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var amounts = (await TestApi.ListValues(client, trackerId, "Amount")).Order().ToList();
            Assert.Equal(["10", "3"], amounts);
        }

        [Fact]
        public async Task Batch_CreateMayOmitAnOptionalField()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Batch omits");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);

            // The same payload is rejected by POST /entries, which insists on a key for every
            // manual field.
            var response = await Batch(client, trackerId, new BatchEntriesDto
            {
                Creates = [new() { ["Note"] = "hi" }]
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var entries = await TestApi.ListEntries(client, trackerId);
            Assert.Single(entries);
            Assert.Null(TestApi.ValueOf(entries[0], "Amount"));
        }

        [Fact]
        public async Task Batch_CreateWithoutARequiredField_ReturnsBadRequestAndCreatesNothing()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Batch required");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number, required: true);

            var response = await Batch(client, trackerId, new BatchEntriesDto
            {
                Creates =
                [
                    new() { ["Note"] = "fine", ["Amount"] = "1" },
                    new() { ["Note"] = "missing its amount" }
                ]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            // The whole batch is one transaction, so the row before the bad one is not kept.
            Assert.Empty(await TestApi.ListEntries(client, trackerId));
        }

        [Fact]
        public async Task Batch_UpdateOfAnUnknownEntry_ReturnsNotFoundAndAppliesNothing()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Batch unknown update");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);

            var response = await Batch(client, trackerId, new BatchEntriesDto
            {
                Creates = [new() { ["Amount"] = "1" }],
                Updates = [new() { EntryId = Guid.NewGuid().ToString(), FieldValues = new() { ["Amount"] = "2" } }]
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(await TestApi.ListEntries(client, trackerId));
        }

        [Fact]
        public async Task Batch_UpdateOfAnEntryInAnotherTracker_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Batch tracker A");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "1" });
            var otherTrackerId = await TestApi.CreateTracker(client, "Batch tracker B");

            var response = await Batch(client, otherTrackerId, new BatchEntriesDto
            {
                Updates = [new() { EntryId = entryId, FieldValues = new() { ["Amount"] = "2" } }]
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(1, await TestApi.NumberValueOf(client, trackerId, entryId, "Amount"));
        }

        [Fact]
        public async Task Batch_UpdateOmittingAField_ClearsIt()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Batch clears");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "hi", ["Amount"] = "5" });

            var response = await Batch(client, trackerId, new BatchEntriesDto
            {
                Updates = [new() { EntryId = entryId, FieldValues = new() { ["Note"] = "hi" } }]
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var entry = await TestApi.GetEntry(client, trackerId, entryId);
            Assert.Null(TestApi.ValueOf(entry, "Amount"));
        }

        [Fact]
        public async Task Batch_DeleteOfAnUnknownEntry_IsIgnored()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Batch unknown delete");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "1" });

            var response = await Batch(client, trackerId, new BatchEntriesDto { Deletes = [Guid.NewGuid().ToString()] });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(await TestApi.ListEntries(client, trackerId));
        }

        [Fact]
        public async Task Batch_RecalculatesCalculatedFieldsForBothCreatesAndUpdates()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Batch calculated");
            await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);
            await TestApi.CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2", DataTypes.Number);
            var existing = await TestApi.CreateEntry(client, trackerId, new() { ["Base"] = "1" });

            var response = await Batch(client, trackerId, new BatchEntriesDto
            {
                Creates = [new() { ["Base"] = "4" }],
                Updates = [new() { EntryId = existing, FieldValues = new() { ["Base"] = "3" } }]
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(6, await TestApi.NumberValueOf(client, trackerId, existing, "Doubled"));
            var created = (await TestApi.ListEntries(client, trackerId)).Single(e => e.GetProperty("id").GetString() != existing);
            Assert.Equal(8, TestApi.ValueOf(created, "Doubled")!.Value.GetDouble());
        }

        [Fact]
        public async Task Batch_EmptyPayload_ReturnsOk()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Batch nothing");

            var response = await Batch(client, trackerId, new BatchEntriesDto());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Batch_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Batch guard");
            await TestApi.CreateField(owner, trackerId, "Amount", DataTypes.Number);
            var entryId = await TestApi.CreateEntry(owner, trackerId, new() { ["Amount"] = "1" });

            var stranger = await _factory.NewUserClient("batchstranger");
            var response = await Batch(stranger, trackerId, new BatchEntriesDto { Deletes = [entryId] });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Single(await TestApi.ListEntries(owner, trackerId));
        }
    }
}

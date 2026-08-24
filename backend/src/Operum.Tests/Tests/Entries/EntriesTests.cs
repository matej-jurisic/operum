using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Entries
{
    public class EntriesTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        // Every test owns its trackers outright: the class shares one database, and a single
        // account would run into the 20-tracker limit part way through the file.
        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("owner");

        // A tracker with one optional field of every type.
        private static async Task<string> CreateTypedTracker(HttpClient client, string name)
        {
            var trackerId = await TestApi.CreateTracker(client, name);
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateField(client, trackerId, "Day", DataTypes.Date);
            await TestApi.CreateField(client, trackerId, "Duration", DataTypes.TimeSpan);
            await TestApi.CreateField(client, trackerId, "Done", DataTypes.Bool);
            return trackerId;
        }

        private static Dictionary<string, string?> FullEntry(string note = "hi", string amount = "5", string day = "2026-01-01",
            string duration = "04:00:00", string done = "true") =>
            new() { ["Note"] = note, ["Amount"] = amount, ["Day"] = day, ["Duration"] = duration, ["Done"] = done };

        [Fact]
        public async Task CreateEntry_EveryFieldType_RoundTripsTheValue()
        {
            var client = await OwnerClient();
            var trackerId = await CreateTypedTracker(client, "Round trip");

            var entryId = await TestApi.CreateEntry(client, trackerId, FullEntry());
            var entry = await TestApi.GetEntry(client, trackerId, entryId);

            Assert.Equal("hi", TestApi.ValueOf(entry, "Note")!.Value.GetString());
            Assert.Equal(5, TestApi.ValueOf(entry, "Amount")!.Value.GetDouble());
            Assert.StartsWith("2026-01-01", TestApi.ValueOf(entry, "Day")!.Value.GetString());
            Assert.Equal(TimeSpan.FromHours(4), TimeSpan.Parse(TestApi.ValueOf(entry, "Duration")!.Value.GetString()!));
            Assert.True(TestApi.ValueOf(entry, "Done")!.Value.GetBoolean());
        }

        [Fact]
        public async Task CreateEntry_FieldValuesComeBackInFieldOrder()
        {
            var client = await OwnerClient();
            var trackerId = await CreateTypedTracker(client, "Ordering");

            var entryId = await TestApi.CreateEntry(client, trackerId, FullEntry());
            var entry = await TestApi.GetEntry(client, trackerId, entryId);

            var names = entry.GetProperty("fieldValues").EnumerateArray()
                .Select(fv => fv.GetProperty("fieldName").GetString())
                .ToList();
            Assert.Equal(["Note", "Amount", "Day", "Duration", "Done"], names);
        }

        [Fact]
        public async Task CreateEntry_OmittedOptionalField_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Omitted optional");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);

            // Creating an entry demands a key for every manual field, required or not — only
            // its value may be null. Batch create is more forgiving about the same thing.
            var response = await TestApi.PostEntry(client, trackerId, new() { ["Note"] = "hi" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.Required("Amount"), await TestApi.Messages(response));
        }

        [Fact]
        public async Task CreateEntry_OptionalFieldExplicitlyNull_StoresNoValue()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Explicit null");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);

            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "hi", ["Amount"] = null });
            var entry = await TestApi.GetEntry(client, trackerId, entryId);

            Assert.Null(TestApi.ValueOf(entry, "Amount"));
        }

        [Fact]
        public async Task CreateEntry_RequiredFieldLeftEmpty_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Required empty");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String, required: true);

            var response = await TestApi.PostEntry(client, trackerId, new() { ["Note"] = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.Required("Note"), await TestApi.Messages(response));
        }

        [Fact]
        public async Task CreateEntry_NoFieldValuesAtAll_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Empty payload");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);

            var response = await TestApi.PostEntry(client, trackerId, []);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateEntry_ValueLongerThanTheLimit_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Long value");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);

            Assert.Equal(HttpStatusCode.OK,
                (await TestApi.PostEntry(client, trackerId, new() { ["Note"] = new string('a', 1000) })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await TestApi.PostEntry(client, trackerId, new() { ["Note"] = new string('a', 1001) })).StatusCode);
        }

        [Fact]
        public async Task CreateEntry_UnparsableNumber_StoresNoValue()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Bad number");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);

            // SetFieldValue swallows the conversion failure, so the entry is accepted with the
            // value dropped rather than rejected.
            var response = await TestApi.PostEntry(client, trackerId, new() { ["Amount"] = "not a number" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var entry = await TestApi.GetEntry(client, trackerId, await TestApi.IdOf(response));
            Assert.Null(TestApi.ValueOf(entry, "Amount"));
        }

        [Fact]
        public async Task CreateEntry_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Private");
            await TestApi.CreateField(owner, trackerId, "Note", DataTypes.String);

            var (stranger, _) = await _factory.AuthenticatedClientForNewUser("stranger");
            var response = await TestApi.PostEntry(stranger, trackerId, new() { ["Note"] = "hi" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateEntry_OnAnUnknownTracker_ReturnsForbidden()
        {
            var client = await OwnerClient();

            var response = await TestApi.PostEntry(client, Guid.NewGuid().ToString(), new() { ["Note"] = "hi" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetEntry_UnknownEntry_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Unknown entry");

            var response = await client.GetAsync($"trackers/{trackerId}/entries/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetEntry_ThroughAnotherTrackerOfTheSameUser_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Holder");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "hi" });
            var otherTrackerId = await TestApi.CreateTracker(client, "Other holder");

            var response = await client.GetAsync($"trackers/{otherTrackerId}/entries/{entryId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetEntry_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Private read");
            await TestApi.CreateField(owner, trackerId, "Note", DataTypes.String);
            var entryId = await TestApi.CreateEntry(owner, trackerId, new() { ["Note"] = "hi" });

            var (stranger, _) = await _factory.AuthenticatedClientForNewUser("reader");
            var response = await stranger.GetAsync($"trackers/{trackerId}/entries/{entryId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetEntries_Paging_ReturnsTheRequestedSliceAndTheTotal()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Paging");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            for (var i = 1; i <= 5; i++)
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = i.ToString() });

            var data = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/entries?page=2&pageSize=2"));

            Assert.Equal(5, data.GetProperty("totalCount").GetInt32());
            Assert.Equal(2, data.GetProperty("page").GetInt32());
            Assert.Equal(2, data.GetProperty("pageSize").GetInt32());
            Assert.Equal(2, data.GetProperty("items").GetArrayLength());
        }

        [Fact]
        public async Task GetEntries_PagePastTheEnd_ReturnsNoItemsButTheRealTotal()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Past the end");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "1" });

            var data = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/entries?page=5&pageSize=10"));

            Assert.Equal(1, data.GetProperty("totalCount").GetInt32());
            Assert.Equal(0, data.GetProperty("items").GetArrayLength());
        }

        [Fact]
        public async Task GetEntries_UnknownView_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Unknown view");

            var response = await client.GetAsync($"trackers/{trackerId}/entries?viewId={Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetEntries_ViewBelongingToAnotherTracker_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "View owner");
            var fieldId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var viewId = await TestApi.CreateFilterView(client, trackerId, "Positive", fieldId, OperatorTypes.GreaterThan, "0");
            var otherTrackerId = await TestApi.CreateTracker(client, "View borrower");

            var response = await client.GetAsync($"trackers/{otherTrackerId}/entries?viewId={viewId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEntry_ChangesTheGivenValueAndKeepsTheRest()
        {
            var client = await OwnerClient();
            var trackerId = await CreateTypedTracker(client, "Update");
            var entryId = await TestApi.CreateEntry(client, trackerId, FullEntry());

            var response = await TestApi.PutEntry(client, trackerId, entryId, FullEntry(note: "changed"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var entry = await TestApi.GetEntry(client, trackerId, entryId);
            Assert.Equal("changed", TestApi.ValueOf(entry, "Note")!.Value.GetString());
            Assert.Equal(5, TestApi.ValueOf(entry, "Amount")!.Value.GetDouble());
        }

        [Fact]
        public async Task UpdateEntry_OmittedField_ClearsThatValue()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Update clears");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "hi", ["Amount"] = "5" });

            // Update treats the payload as the whole entry: what it leaves out is removed.
            await TestApi.PutEntry(client, trackerId, entryId, new() { ["Note"] = "hi" });

            var entry = await TestApi.GetEntry(client, trackerId, entryId);
            Assert.Equal("hi", TestApi.ValueOf(entry, "Note")!.Value.GetString());
            Assert.Null(TestApi.ValueOf(entry, "Amount"));
        }

        [Fact]
        public async Task UpdateEntry_UnknownEntry_ReturnsForbidden()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Update unknown");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);

            // A missing entry is reported as Forbidden here, while GetEntry answers 404 for
            // the same id.
            var response = await TestApi.PutEntry(client, trackerId, Guid.NewGuid().ToString(), new() { ["Note"] = "hi" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEntry_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Private update");
            await TestApi.CreateField(owner, trackerId, "Note", DataTypes.String);
            var entryId = await TestApi.CreateEntry(owner, trackerId, new() { ["Note"] = "hi" });

            var (stranger, _) = await _factory.AuthenticatedClientForNewUser("writer");
            var response = await TestApi.PutEntry(stranger, trackerId, entryId, new() { ["Note"] = "mine now" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("hi", await TestApi.StringValueOf(owner, trackerId, entryId, "Note"));
        }

        [Fact]
        public async Task DeleteEntry_RemovesIt()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Delete one");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "hi" });

            var response = await client.DeleteAsync($"trackers/{trackerId}/entries/{entryId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var afterDelete = await client.GetAsync($"trackers/{trackerId}/entries/{entryId}");
            Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
        }

        [Fact]
        public async Task DeleteEntry_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Private delete");
            await TestApi.CreateField(owner, trackerId, "Note", DataTypes.String);
            var entryId = await TestApi.CreateEntry(owner, trackerId, new() { ["Note"] = "hi" });

            var (stranger, _) = await _factory.AuthenticatedClientForNewUser("deleter");
            var response = await stranger.DeleteAsync($"trackers/{trackerId}/entries/{entryId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"trackers/{trackerId}/entries/{entryId}")).StatusCode);
        }

        [Fact]
        public async Task DeleteEntries_RemovesOnlyTheListedOnes()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Delete many");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var first = await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "1" });
            var second = await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "2" });
            var third = await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "3" });

            var request = new HttpRequestMessage(HttpMethod.Delete, $"trackers/{trackerId}/entries")
            {
                Content = JsonContent.Create(new DeleteEntriesDto { EntryIds = [first, third] })
            };
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var remaining = await TestApi.ListEntries(client, trackerId);
            Assert.Single(remaining);
            Assert.Equal(second, remaining[0].GetProperty("id").GetString());
        }

        [Fact]
        public async Task DeleteEntries_UnknownIds_ReturnsOkAndChangesNothing()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Delete unknown");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "1" });

            var request = new HttpRequestMessage(HttpMethod.Delete, $"trackers/{trackerId}/entries")
            {
                Content = JsonContent.Create(new DeleteEntriesDto { EntryIds = [Guid.NewGuid().ToString()] })
            };
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(await TestApi.ListEntries(client, trackerId));
        }

        [Fact]
        public async Task DeleteEntries_OnATrackerOwnedBySomeoneElse_DeletesNothing()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Bulk delete guard");
            await TestApi.CreateField(owner, trackerId, "Amount", DataTypes.Number);
            var entryId = await TestApi.CreateEntry(owner, trackerId, new() { ["Amount"] = "1" });

            var (stranger, _) = await _factory.AuthenticatedClientForNewUser("bulkdeleter");
            var request = new HttpRequestMessage(HttpMethod.Delete, $"trackers/{trackerId}/entries")
            {
                Content = JsonContent.Create(new DeleteEntriesDto { EntryIds = [entryId] })
            };
            var response = await stranger.SendAsync(request);

            // The bulk delete filters by ownership instead of refusing outright, so it reports
            // success while leaving the other user's entry alone.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(await TestApi.ListEntries(owner, trackerId));
        }

        [Fact]
        public async Task RecalculateEntries_RefreshesValuesAfterTheFormulaChanged()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Recalculate");
            await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);
            var doubledId = await TestApi.CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2", DataTypes.Number);
            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Base"] = "10" });
            Assert.Equal(20, await TestApi.NumberValueOf(client, trackerId, entryId, "Doubled"));

            await client.PutAsJsonAsync($"trackers/{trackerId}/fields/{doubledId}",
                new UpdateFieldDto { Name = "Doubled", Type = DataTypes.Number, IsCalculated = true, Formula = "{Base} * 3" });
            // Changing a formula leaves stored values stale until they are recalculated.
            Assert.Equal(20, await TestApi.NumberValueOf(client, trackerId, entryId, "Doubled"));

            var response = await client.PostAsJsonAsync($"trackers/{trackerId}/entries/recalculate",
                new RecalculateEntriesDto { EntryIds = [entryId] });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(30, await TestApi.NumberValueOf(client, trackerId, entryId, "Doubled"));
        }

        [Fact]
        public async Task RecalculateEntries_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Recalculate guard");

            var (stranger, _) = await _factory.AuthenticatedClientForNewUser("recalculator");
            var response = await stranger.PostAsJsonAsync($"trackers/{trackerId}/entries/recalculate",
                new RecalculateEntriesDto { EntryIds = [] });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}

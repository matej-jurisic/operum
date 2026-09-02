using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using Operum.Tests.Extensions;
using Operum.Tests.Util;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Entries
{
    /// <summary>
    /// EntryWriter is the write path an integration uses, so unlike every other suite here it
    /// is exercised through the service rather than through HTTP -- the whole point of the
    /// type is that it works with no request and no signed-in user behind it. Trackers and
    /// fields are still set up over the API, because that is how they are really made.
    /// </summary>
    public class EntryWriterTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private const string Source = "test-provider";

        private readonly CustomWebApplicationFactory _factory = factory;

        private static async Task<JsonElement> Data(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            if (!json.TryGetProperty("data", out var data))
                throw new Exception($"Response had no 'data' property. Status: {response.StatusCode}. Body: {body}");
            return data;
        }

        private async Task<HttpClient> AuthenticatedClient()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);
            return client;
        }

        private static async Task<string> CreateTracker(HttpClient client, string name) =>
            (await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = name })))
                .GetProperty("id").GetString()!;

        private static Task<HttpResponseMessage> CreateField(
            HttpClient client, string trackerId, string name, string type, bool required = false) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = name, Type = type, Required = required });

        private static Task<HttpResponseMessage> CreateCalculatedField(
            HttpClient client, string trackerId, string name, string formula, string type = DataTypes.Number) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = name, Type = type, IsCalculated = true, Formula = formula });

        /// <summary>Field ids by name, which is what a mapping stores.</summary>
        private static async Task<Dictionary<string, string>> FieldIds(HttpClient client, string trackerId)
        {
            var fields = await Data(await client.GetAsync($"trackers/{trackerId}/fields"));
            return fields.EnumerateArray().ToDictionary(
                f => f.GetProperty("name").GetString()!,
                f => f.GetProperty("id").GetString()!);
        }

        /// <summary>
        /// Runs the writer in its own scope, the way a sync tick or a webhook would.
        /// </summary>
        private async Task<EntryWriteResult> Apply(string trackerId, params EntryWriteRecord[] records)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var writer = scope.ServiceProvider.GetRequiredService<IEntryWriter>();

            var fields = await db.Fields.Where(f => f.TrackerId == trackerId).ToListAsync();
            return await writer.ApplyAsync(trackerId, Source, records, fields, TimeZoneInfo.Utc);
        }

        private async Task<List<Entry>> StoredEntries(string trackerId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            return await db.Entries
                .Include(e => e.FieldValues)
                .Where(e => e.TrackerId == trackerId)
                .ToListAsync();
        }

        private async Task<string?> ValueOf(string trackerId, string externalId, string fieldId)
        {
            var entries = await StoredEntries(trackerId);
            var entry = entries.SingleOrDefault(e => e.ExternalId == externalId);
            var fieldValue = entry?.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId);
            if (fieldValue == null) return null;

            // The row is EAV, so read whichever column the type landed in.
            return fieldValue.StringValue
                ?? fieldValue.NumberValue?.ToString()
                ?? fieldValue.TimeSpanValue?.ToString()
                ?? fieldValue.BooleanValue?.ToString()
                ?? fieldValue.DateTimeValue?.ToString();
        }

        private static EntryWriteRecord Upsert(string externalId, Dictionary<string, string?> values) =>
            new(externalId, EntryWriteOperation.Upsert, values);

        [Fact]
        public async Task Apply_SameRecordTwice_UpdatesInsteadOfDuplicating()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Idempotency");
            await CreateField(client, trackerId, "Weight", DataTypes.Number);
            var fields = await FieldIds(client, trackerId);

            var first = await Apply(trackerId, Upsert("2026-01-01", new() { [fields["Weight"]] = "80" }));
            Assert.Equal(1, first.Created);
            Assert.Equal(0, first.Updated);

            var second = await Apply(trackerId, Upsert("2026-01-01", new() { [fields["Weight"]] = "81" }));
            Assert.Equal(0, second.Created);
            Assert.Equal(1, second.Updated);

            Assert.Single(await StoredEntries(trackerId));
            Assert.Equal("81", await ValueOf(trackerId, "2026-01-01", fields["Weight"]));
        }

        [Fact]
        public async Task Apply_TwoSourcesSameExternalId_AreSeparateEntries()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Two sources");
            await CreateField(client, trackerId, "Weight", DataTypes.Number);
            var fields = await FieldIds(client, trackerId);

            await Apply(trackerId, Upsert("shared-id", new() { [fields["Weight"]] = "80" }));

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
                var writer = scope.ServiceProvider.GetRequiredService<IEntryWriter>();
                var allFields = await db.Fields.Where(f => f.TrackerId == trackerId).ToListAsync();

                var result = await writer.ApplyAsync(trackerId, "other-provider",
                    [Upsert("shared-id", new() { [fields["Weight"]] = "90" })], allFields, TimeZoneInfo.Utc);

                Assert.Equal(1, result.Created);
            }

            // The unique index is per source, so the same id from a second provider is a
            // different record, not a collision.
            Assert.Equal(2, (await StoredEntries(trackerId)).Count);
        }

        [Fact]
        public async Task Apply_Delete_RemovesTheEntry_AndIsANoOpWhenUnknown()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Deletes");
            await CreateField(client, trackerId, "Amount", DataTypes.Number);
            var fields = await FieldIds(client, trackerId);

            await Apply(trackerId, Upsert("txn-1", new() { [fields["Amount"]] = "12" }));

            var result = await Apply(trackerId,
                new EntryWriteRecord("txn-1", EntryWriteOperation.Delete, new Dictionary<string, string?>()),
                new EntryWriteRecord("never-imported", EntryWriteOperation.Delete, new Dictionary<string, string?>()));

            // The provider is allowed to tell us about records we never took.
            Assert.Equal(1, result.Deleted);
            Assert.Equal(0, result.ErrorCount);
            Assert.Empty(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task Apply_AbsentKeyIsLeftAlone_NullKeyIsCleared()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Presence");
            await CreateField(client, trackerId, "Hrv", DataTypes.Number);
            await CreateField(client, trackerId, "Note", DataTypes.String);
            var fields = await FieldIds(client, trackerId);

            await Apply(trackerId, Upsert("day-1", new()
            {
                [fields["Hrv"]] = "55",
                [fields["Note"]] = "felt good",
            }));

            // Hrv omitted entirely (what SkipWhenNull produces), Note explicitly nulled.
            await Apply(trackerId, Upsert("day-1", new() { [fields["Note"]] = null }));

            Assert.Equal("55", await ValueOf(trackerId, "day-1", fields["Hrv"]));
            Assert.Null(await ValueOf(trackerId, "day-1", fields["Note"]));
        }

        [Fact]
        public async Task Apply_DuplicateExternalIdInOneBatch_LastWins()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Batch dupes");
            await CreateField(client, trackerId, "Steps", DataTypes.Number);
            var fields = await FieldIds(client, trackerId);

            // Two revisions of one record arriving in the same payload must not both insert.
            var result = await Apply(trackerId,
                Upsert("day-1", new() { [fields["Steps"]] = "1000" }),
                Upsert("day-1", new() { [fields["Steps"]] = "2000" }));

            Assert.Equal(1, result.Created);
            Assert.Single(await StoredEntries(trackerId));
            Assert.Equal("2000", await ValueOf(trackerId, "day-1", fields["Steps"]));
        }

        [Fact]
        public async Task Apply_TimeSpanValue_CoercesAndFeedsCalculatedFields()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Sleep");
            await CreateField(client, trackerId, "Sleep", DataTypes.TimeSpan);
            await CreateCalculatedField(client, trackerId, "SleepHours", "{Sleep.hours}");
            var fields = await FieldIds(client, trackerId);

            // A provider converts its own units before handing over: intervals.icu sends
            // sleepSecs as 28800, the provider emits the timespan string.
            await Apply(trackerId, Upsert("day-1", new() { [fields["Sleep"]] = "08:00:00" }));

            Assert.Equal(TimeSpan.FromHours(8).ToString(), await ValueOf(trackerId, "day-1", fields["Sleep"]));
            Assert.Equal("8", await ValueOf(trackerId, "day-1", fields["SleepHours"]));
        }

        [Fact]
        public async Task Apply_CalculatedField_RecomputesOnUpdateNotJustInsert()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Recompute");
            await CreateField(client, trackerId, "Base", DataTypes.Number);
            await CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2");
            var fields = await FieldIds(client, trackerId);

            await Apply(trackerId, Upsert("day-1", new() { [fields["Base"]] = "10" }));
            Assert.Equal("20", await ValueOf(trackerId, "day-1", fields["Doubled"]));

            await Apply(trackerId, Upsert("day-1", new() { [fields["Base"]] = "15" }));
            Assert.Equal("30", await ValueOf(trackerId, "day-1", fields["Doubled"]));
        }

        [Fact]
        public async Task Apply_MissingRequiredFieldOnCreate_SkipsRecordWithReason()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Required");
            await CreateField(client, trackerId, "Weight", DataTypes.Number, required: true);
            await CreateField(client, trackerId, "Note", DataTypes.String);
            var fields = await FieldIds(client, trackerId);

            var result = await Apply(trackerId, Upsert("day-1", new() { [fields["Note"]] = "no weight logged" }));

            Assert.Equal(0, result.Created);
            Assert.Equal(1, result.Skipped);
            Assert.Contains(result.Errors, e => e.Contains("Weight"));
            Assert.Empty(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task Apply_RequiredFieldNotResentOnUpdate_KeepsTheExistingValue()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Required update");
            await CreateField(client, trackerId, "Weight", DataTypes.Number, required: true);
            await CreateField(client, trackerId, "Note", DataTypes.String);
            var fields = await FieldIds(client, trackerId);

            await Apply(trackerId, Upsert("day-1", new() { [fields["Weight"]] = "80" }));

            // An update inherits what the entry already holds, so the required check that
            // guards a create must not fire here.
            var result = await Apply(trackerId, Upsert("day-1", new() { [fields["Note"]] = "later revision" }));

            Assert.Equal(1, result.Updated);
            Assert.Equal(0, result.Skipped);
            Assert.Equal("80", await ValueOf(trackerId, "day-1", fields["Weight"]));
        }

        [Fact]
        public async Task Apply_UnparseableValue_ReportsItAndStillWritesTheRest()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Bad value");
            await CreateField(client, trackerId, "Amount", DataTypes.Number);
            await CreateField(client, trackerId, "Note", DataTypes.String);
            var fields = await FieldIds(client, trackerId);

            var result = await Apply(trackerId, Upsert("day-1", new()
            {
                [fields["Amount"]] = "not a number",
                [fields["Note"]] = "kept",
            }));

            Assert.Equal(1, result.Created);
            Assert.Equal(1, result.ErrorCount);
            Assert.Equal("kept", await ValueOf(trackerId, "day-1", fields["Note"]));
            Assert.Null(await ValueOf(trackerId, "day-1", fields["Amount"]));
        }

        [Fact]
        public async Task Apply_LeavesManuallyCreatedEntriesAlone()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Mixed origins");
            await CreateField(client, trackerId, "Weight", DataTypes.Number);
            var fields = await FieldIds(client, trackerId);

            // Two hand-made entries: both leave Source null, which the filtered unique index
            // has to keep permitting.
            await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Weight"] = "70" } });
            await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Weight"] = "71" } });

            await Apply(trackerId, Upsert("day-1", new() { [fields["Weight"]] = "80" }));

            var entries = await StoredEntries(trackerId);
            Assert.Equal(3, entries.Count);
            Assert.Equal(2, entries.Count(e => e.Source == null));
            Assert.Single(entries, e => e.Source == Source);
        }

        [Fact]
        public async Task Apply_GroupWithFewerChildrenThanBefore_RemovesTheMissingOnes()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Group shrink");
            await CreateField(client, trackerId, "Amount", DataTypes.Number);
            var fields = await FieldIds(client, trackerId);

            EntryWriteRecord InGroup(string id, string groupId, string amount) =>
                new(id, EntryWriteOperation.Upsert, new Dictionary<string, string?> { [fields["Amount"]] = amount }, groupId);

            await Apply(trackerId, InGroup("a1", "g1", "10"), InGroup("a2", "g1", "5"));
            Assert.Equal(2, (await StoredEntries(trackerId)).Count);

            // The parent now has one child, so the other was deleted upstream.
            var result = await Apply(trackerId, InGroup("a1", "g1", "15"));

            Assert.Equal(1, result.Deleted);
            Assert.Equal(1, result.Updated);

            var entry = Assert.Single(await StoredEntries(trackerId));
            Assert.Equal("a1", entry.ExternalId);
        }

        [Fact]
        public async Task Apply_GroupReconciliation_LeavesOtherGroupsAndUngroupedEntriesAlone()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Group isolation");
            await CreateField(client, trackerId, "Amount", DataTypes.Number);
            var fields = await FieldIds(client, trackerId);

            EntryWriteRecord InGroup(string id, string? groupId, string amount) =>
                new(id, EntryWriteOperation.Upsert, new Dictionary<string, string?> { [fields["Amount"]] = amount }, groupId);

            await Apply(trackerId,
                InGroup("a1", "g1", "10"),
                InGroup("b1", "g2", "20"),
                InGroup("flat", null, "30"));

            // Reconciling g1 must not reach into g2, nor touch a record with no parent at all.
            await Apply(trackerId, InGroup("a1", "g1", "11"));

            var entries = await StoredEntries(trackerId);
            Assert.Equal(3, entries.Count);
            Assert.Contains(entries, e => e.ExternalId == "b1");
            Assert.Contains(entries, e => e.ExternalId == "flat");
        }

        [Fact]
        public async Task Apply_EmptyBatch_DoesNothing()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Empty");
            await CreateField(client, trackerId, "Weight", DataTypes.Number);

            var result = await Apply(trackerId);

            // Compared field by field: the record holds a List, so its generated equality is
            // reference equality on that member and two empty results never match.
            Assert.Equal(0, result.Created);
            Assert.Equal(0, result.Updated);
            Assert.Equal(0, result.Deleted);
            Assert.Equal(0, result.Skipped);
            Assert.Equal(0, result.ErrorCount);
            Assert.Empty(await StoredEntries(trackerId));
        }
    }
}

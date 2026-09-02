using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Operum.Model;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.Integrations;
using Operum.Model.Models;
using Operum.Service.Domain.Integrations;
using Operum.Service.Integrations;
using Operum.Service.Interfaces;
using Operum.Tests.Extensions;
using Operum.Tests.Mocks;
using Operum.Tests.Util;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Integrations
{
    /// <summary>
    /// The shared pipeline -- provider to projector to writer -- driven by a fake provider, so
    /// it is proven before any real integration exists and a later regression surfaces here
    /// rather than against a live account.
    /// </summary>
    public class ProviderPipelineTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
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

        private static async Task<Dictionary<string, string>> FieldIds(HttpClient client, string trackerId)
        {
            var fields = await Data(await client.GetAsync($"trackers/{trackerId}/fields"));
            return fields.EnumerateArray().ToDictionary(
                f => f.GetProperty("name").GetString()!,
                f => f.GetProperty("id").GetString()!);
        }

        private static SourceRecord Upsert(string externalId, Dictionary<string, string?> values) =>
            new(externalId, SourceOperation.Upsert, DateTime.UtcNow, values);

        // ---- registry ----

        [Fact]
        public void Registry_ResolvesByCapability()
        {
            var pullAndPush = new FakeIntegrationProvider("both");
            var registry = new IntegrationProviderRegistry([pullAndPush]);

            Assert.Same(pullAndPush, registry.Get("both"));
            Assert.Same(pullAndPush, registry.GetPull("both"));
            Assert.Same(pullAndPush, registry.GetPush("both"));

            // An unknown key is a stale saved connection, not a crash.
            Assert.Null(registry.Get("missing"));
            Assert.Null(registry.GetPull("missing"));
            Assert.Null(registry.GetPush("missing"));
        }

        [Fact]
        public void Registry_IsCaseInsensitiveOnKey()
        {
            var registry = new IntegrationProviderRegistry([new FakeIntegrationProvider("Intervals.ICU")]);
            Assert.NotNull(registry.Get("intervals.icu"));
        }

        [Fact]
        public void Registry_RefusesTwoProvidersUnderOneKey()
        {
            // Otherwise which one ran would depend on registration order, and the loser's
            // stored connections would quietly start syncing from somewhere else.
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new IntegrationProviderRegistry([new FakeIntegrationProvider("dup"), new FakeIntegrationProvider("dup")]));

            Assert.Contains("dup", ex.Message);
        }

        // ---- projector ----

        [Fact]
        public void Project_OnlyMappedKeysSurvive()
        {
            var mappings = new List<FieldMapping> { new("amount", "field-amount") };

            var result = SourceRecordProjector.Project(
                Upsert("r1", new() { ["amount"] = "12", ["note"] = "ignored, not mapped" }),
                mappings);

            Assert.Equal("r1", result.ExternalId);
            Assert.Equal(Model.Common.EntryWriteOperation.Upsert, result.Operation);
            Assert.Equal(new Dictionary<string, string?> { ["field-amount"] = "12" }, result.ValuesByFieldId);
        }

        [Fact]
        public void Project_SkipWhenNull_OmitsTheKeyEntirely()
        {
            var mappings = new List<FieldMapping> { new("amount", "field-amount", SkipWhenNull: true) };

            var result = SourceRecordProjector.Project(
                Upsert("r1", new() { ["amount"] = null }), mappings);

            // Omitted rather than null, which is how the writer is told to leave the field
            // as it found it instead of clearing it.
            Assert.Empty(result.ValuesByFieldId);
        }

        [Fact]
        public void Project_SkipWhenNullFalse_KeepsTheNullSoTheFieldIsCleared()
        {
            var mappings = new List<FieldMapping> { new("amount", "field-amount", SkipWhenNull: false) };

            var result = SourceRecordProjector.Project(
                Upsert("r1", new() { ["amount"] = null }), mappings);

            Assert.True(result.ValuesByFieldId.ContainsKey("field-amount"));
            Assert.Null(result.ValuesByFieldId["field-amount"]);
        }

        [Fact]
        public void Project_KeyTheProviderNeverSent_IsNotWrittenEvenWithSkipWhenNullOff()
        {
            var mappings = new List<FieldMapping> { new("amount", "field-amount", SkipWhenNull: false) };

            // Saying nothing about a key is different from saying it has no value.
            var result = SourceRecordProjector.Project(
                Upsert("r1", new() { ["note"] = "something else" }), mappings);

            Assert.Empty(result.ValuesByFieldId);
        }

        [Fact]
        public void Project_Delete_CarriesNoValues()
        {
            var mappings = new List<FieldMapping> { new("amount", "field-amount") };

            var result = SourceRecordProjector.Project(SourceRecord.Deleted("r1"), mappings);

            Assert.Equal(Model.Common.EntryWriteOperation.Delete, result.Operation);
            Assert.Empty(result.ValuesByFieldId);
        }

        // ---- mapping validation ----

        private static List<Field> TrackerFields() =>
        [
            new() { Id = "f-number", Name = "Amount", Type = DataTypes.Number, TrackerId = "t" },
            new() { Id = "f-string", Name = "Note", Type = DataTypes.String, TrackerId = "t" },
            new() { Id = "f-timespan", Name = "Duration", Type = DataTypes.TimeSpan, TrackerId = "t" },
            new() { Id = "f-date", Name = "Day", Type = DataTypes.Date, TrackerId = "t" },
            new() { Id = "f-calc", Name = "Derived", Type = DataTypes.Number, TrackerId = "t", IsCalculated = true },
            new() { Id = "f-required", Name = "Weight", Type = DataTypes.Number, TrackerId = "t", Required = true },
        ];

        private static string? Validate(params FieldMapping[] mappings) =>
            MappingValidator.Validate(mappings, new FakeIntegrationProvider().Catalog(FakeIntegrationProvider.ResourceType), TrackerFields());

        [Fact]
        public void Validate_MatchingTypes_Passes()
        {
            Assert.Null(Validate(
                new FieldMapping("amount", "f-number"),
                new FieldMapping("note", "f-string"),
                new FieldMapping("duration", "f-timespan")));
        }

        [Fact]
        public void Validate_TimeSpanIntoNumber_IsAllowedAsRawSeconds()
        {
            Assert.Null(Validate(new FieldMapping("duration", "f-number")));
        }

        [Fact]
        public void Validate_DateTimeIntoDate_IsAllowed()
        {
            // date and datetime are one storage column and interchangeable everywhere else.
            Assert.Null(Validate(new FieldMapping("occurred", "f-date")));
        }

        [Fact]
        public void Validate_MismatchedTypes_AreRejectedWithBothNames()
        {
            var error = Validate(new FieldMapping("note", "f-number"));

            Assert.NotNull(error);
            Assert.Contains("Note", error);
            Assert.Contains("Amount", error);
        }

        [Fact]
        public void Validate_UnknownSourceKey_IsRejected()
        {
            Assert.Contains("nonsense", Validate(new FieldMapping("nonsense", "f-number"))!);
        }

        [Fact]
        public void Validate_FieldFromAnotherTracker_IsRejected()
        {
            Assert.NotNull(Validate(new FieldMapping("amount", "f-does-not-exist")));
        }

        [Fact]
        public void Validate_CalculatedTarget_IsRejected()
        {
            Assert.Contains("calculated", Validate(new FieldMapping("amount", "f-calc"))!);
        }

        [Fact]
        public void Validate_SameFieldTwice_IsRejected()
        {
            var error = Validate(new FieldMapping("amount", "f-number"), new FieldMapping("duration", "f-number"));
            Assert.Contains("more than once", error!);
        }

        [Fact]
        public void Validate_RequiredFieldWithSkipWhenNull_IsRejected()
        {
            // The pairing that would drop every record missing the metric, silently.
            var error = Validate(new FieldMapping("amount", "f-required", SkipWhenNull: true));
            Assert.Contains("required", error!);
        }

        [Fact]
        public void Validate_RequiredFieldThatClearsInstead_IsAllowed()
        {
            Assert.Null(Validate(new FieldMapping("amount", "f-required", SkipWhenNull: false)));
        }

        [Fact]
        public void Validate_NoMappings_IsRejected()
        {
            Assert.NotNull(MappingValidator.Validate([], new FakeIntegrationProvider().Catalog(FakeIntegrationProvider.ResourceType), TrackerFields()));
        }

        // ---- provider -> projector -> writer ----

        [Fact]
        public async Task Pipeline_PullThenReSync_WritesOnceThenUpdates()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Pipeline pull");
            await CreateField(client, trackerId, "Amount", DataTypes.Number);
            await CreateField(client, trackerId, "Note", DataTypes.String);
            var fields = await FieldIds(client, trackerId);

            var provider = new FakeIntegrationProvider();
            var mappings = new List<FieldMapping>
            {
                new("amount", fields["Amount"]),
                new("note", fields["Note"]),
            };

            provider.Records.Add(Upsert("r1", new() { ["amount"] = "10", ["note"] = "first" }));
            provider.Records.Add(Upsert("r2", new() { ["amount"] = "20", ["note"] = "second" }));

            var first = await RunPull(provider, trackerId, mappings);
            Assert.Equal(2, first.Created);
            Assert.Equal(0, first.Updated);

            // Same window again, one record revised upstream.
            provider.Records.Clear();
            provider.Records.Add(Upsert("r1", new() { ["amount"] = "11", ["note"] = "first, revised" }));
            provider.Records.Add(Upsert("r2", new() { ["amount"] = "20", ["note"] = "second" }));

            var second = await RunPull(provider, trackerId, mappings);
            Assert.Equal(0, second.Created);
            Assert.Equal(2, second.Updated);

            var entries = await StoredEntries(trackerId);
            Assert.Equal(2, entries.Count);
            var revised = entries.Single(e => e.ExternalId == "r1");
            Assert.Equal(11, revised.FieldValues.Single(fv => fv.FieldId == fields["Amount"]).NumberValue);
        }

        [Fact]
        public async Task Pipeline_PushDelivery_VerifiesBeforeWriting()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Pipeline push");
            await CreateField(client, trackerId, "Amount", DataTypes.Number);
            var fields = await FieldIds(client, trackerId);

            var provider = new FakeIntegrationProvider();
            provider.Records.Add(Upsert("r1", new() { ["amount"] = "42" }));
            var mappings = new List<FieldMapping> { new("amount", fields["Amount"]) };

            var rejected = provider.VerifyAndParse(
                FakeIntegrationProvider.ResourceType, FakeIntegrationProvider.ValidSecret,
                "{}", new Dictionary<string, string> { ["X-Fake-Signature"] = "wrong" });

            Assert.True(rejected.IsFailure);
            Assert.Equal(Model.Enums.ResultStatusCodes.Forbidden, rejected.StatusCode);
            Assert.Empty(await StoredEntries(trackerId));

            var accepted = provider.VerifyAndParse(
                FakeIntegrationProvider.ResourceType, FakeIntegrationProvider.ValidSecret,
                "{}", new Dictionary<string, string> { ["X-Fake-Signature"] = FakeIntegrationProvider.ValidSecret });

            Assert.True(accepted.IsSuccess);

            var result = await Write(trackerId, provider.Key, SourceRecordProjector.Project(accepted.Data, mappings).ToArray());
            Assert.Equal(1, result.Created);
            Assert.Single(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task Pipeline_DeleteRecord_RemovesWhatAnEarlierSyncWrote()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Pipeline delete");
            await CreateField(client, trackerId, "Amount", DataTypes.Number);
            var fields = await FieldIds(client, trackerId);

            var provider = new FakeIntegrationProvider();
            var mappings = new List<FieldMapping> { new("amount", fields["Amount"]) };

            provider.Records.Add(Upsert("r1", new() { ["amount"] = "10" }));
            await RunPull(provider, trackerId, mappings);
            Assert.Single(await StoredEntries(trackerId));

            provider.Records.Clear();
            provider.Records.Add(SourceRecord.Deleted("r1"));
            var result = await RunPull(provider, trackerId, mappings);

            Assert.Equal(1, result.Deleted);
            Assert.Empty(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task Pipeline_UnloggedMetric_LeavesTheFieldAloneOnResync()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Pipeline nulls");
            await CreateField(client, trackerId, "Amount", DataTypes.Number);
            var fields = await FieldIds(client, trackerId);

            var provider = new FakeIntegrationProvider();
            var mappings = new List<FieldMapping> { new("amount", fields["Amount"], SkipWhenNull: true) };

            provider.Records.Add(Upsert("r1", new() { ["amount"] = "55" }));
            await RunPull(provider, trackerId, mappings);

            // The device stopped reporting it; the value already recorded must survive.
            provider.Records.Clear();
            provider.Records.Add(Upsert("r1", new() { ["amount"] = null }));
            await RunPull(provider, trackerId, mappings);

            var entry = (await StoredEntries(trackerId)).Single();
            Assert.Equal(55, entry.FieldValues.Single(fv => fv.FieldId == fields["Amount"]).NumberValue);
        }

        // ---- helpers ----

        /// <summary>Provider to projector to writer, the way the sync service will run it.</summary>
        private async Task<Model.Common.EntryWriteResult> RunPull(
            FakeIntegrationProvider provider, string trackerId, List<FieldMapping> mappings)
        {
            var window = new SyncWindow(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                DateOnly.FromDateTime(DateTime.UtcNow),
                null);

            var connection = new ProviderConnection(null, "good-key", "athlete-1");

            var projected = new List<Model.Common.EntryWriteRecord>();
            await foreach (var record in provider.FetchAsync(connection, FakeIntegrationProvider.ResourceType, window))
                projected.Add(SourceRecordProjector.Project(record, mappings));

            return await Write(trackerId, provider.Key, projected.ToArray());
        }

        private async Task<Model.Common.EntryWriteResult> Write(
            string trackerId, string source, params Model.Common.EntryWriteRecord[] records)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var writer = scope.ServiceProvider.GetRequiredService<IEntryWriter>();

            var fields = await db.Fields.Where(f => f.TrackerId == trackerId).ToListAsync();
            return await writer.ApplyAsync(trackerId, source, records, fields, TimeZoneInfo.Utc);
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
    }
}

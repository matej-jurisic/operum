using Microsoft.Extensions.Logging.Abstractions;
using Operum.Model.Constants.Integrations;
using Operum.Model.Integrations;
using Operum.Service.Integrations.Intervals;
using Operum.Tests.Mocks;
using System.Net;
using System.Text;

namespace Operum.Tests.Tests.Integrations
{
    public class IntervalsProviderTests
    {
        private static (IntervalsProvider Provider, StubHttpMessageHandler Handler) Build(
            HttpStatusCode status, string body)
        {
            var handler = new StubHttpMessageHandler(status, body);
            var factory = new StubHttpClientFactory(handler, "https://intervals.icu/");
            return (new IntervalsProvider(factory, NullLogger<IntervalsProvider>.Instance), handler);
        }

        private static ProviderConnection Connection(string? athleteId = "i123") =>
            new(null, "test-key", athleteId);

        private static async Task<List<SourceRecord>> Fetch(IntervalsProvider provider, ProviderConnection connection)
        {
            var window = new SyncWindow(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), null);
            var records = new List<SourceRecord>();

            await foreach (var record in provider.FetchAsync(connection, IntervalsWellnessCatalog.ResourceType, window))
                records.Add(record);

            return records;
        }

        [Fact]
        public void Catalog_IsEmptyForAnUnknownResource()
        {
            var (provider, _) = Build(HttpStatusCode.OK, "[]");

            Assert.NotEmpty(provider.Catalog(IntervalsWellnessCatalog.ResourceType));
            Assert.NotEmpty(provider.Catalog(IntervalsActivitiesCatalog.ResourceType));
            Assert.Empty(provider.Catalog("nonsense"));
        }

        [Fact]
        public void Catalog_Activities_DoesNotOfferTheOpaqueRecordId()
        {
            var (provider, _) = Build(HttpStatusCode.OK, "[]");
            var keys = provider.Catalog(IntervalsActivitiesCatalog.ResourceType).Select(f => f.Key).ToList();

            // The activity id is "iNNNN", not a date -- it is the external id, not worth mapping.
            Assert.DoesNotContain(IntervalsActivitiesCatalog.RecordKey, keys);
            Assert.Contains("start_date_local", keys);
        }

        [Fact]
        public void Catalog_DoesNotOfferTheCursorForMapping()
        {
            var (provider, _) = Build(HttpStatusCode.OK, "[]");
            var keys = provider.Catalog(IntervalsWellnessCatalog.ResourceType).Select(f => f.Key).ToList();

            // "updated" drives the sync cursor and is not something a user maps.
            Assert.DoesNotContain(IntervalsWellnessCatalog.UpdatedKey, keys);
            // The record date is both the external id and a field worth having.
            Assert.Contains(IntervalsWellnessCatalog.RecordKey, keys);
        }

        [Fact]
        public async Task Fetch_SendsBasicAuthAndTheDateRange()
        {
            var (provider, handler) = Build(HttpStatusCode.OK, "[]");

            await Fetch(provider, Connection());

            var request = Assert.Single(handler.Requests);
            Assert.Contains("api/v1/athlete/i123/wellness", request.RequestUri!.ToString());
            Assert.Contains("oldest=2026-01-01", request.RequestUri.Query);
            Assert.Contains("newest=2026-01-31", request.RequestUri.Query);

            // Documented as Basic with the literal username API_KEY.
            Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!));
            Assert.Equal("API_KEY:test-key", decoded);
        }

        [Fact]
        public async Task Fetch_WithNoAthleteId_UsesTheSelfAlias()
        {
            var (provider, handler) = Build(HttpStatusCode.OK, "[]");

            await Fetch(provider, Connection(athleteId: null));

            // "0" means "whoever this key belongs to", so a user never has to find their id.
            Assert.Contains("api/v1/athlete/0/wellness", handler.Requests.Single().RequestUri!.ToString());
        }

        [Fact]
        public async Task Fetch_UsesTheRecordDateAsExternalId_AndUpdatedAsCursor()
        {
            var (provider, _) = Build(HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "updated": "2026-01-05T18:30:00Z", "restingHR": 52 }]
                """);

            var record = Assert.Single(await Fetch(provider, Connection()));

            Assert.Equal("2026-01-05", record.ExternalId);
            Assert.Equal(SourceOperation.Upsert, record.Operation);
            Assert.Equal(new DateTime(2026, 1, 5, 18, 30, 0, DateTimeKind.Utc), record.UpdatedAt);
        }

        [Fact]
        public async Task Fetch_ConvertsSleepSecondsToATimeSpan()
        {
            var (provider, _) = Build(HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "sleepSecs": 28800 }]
                """);

            var record = Assert.Single(await Fetch(provider, Connection()));

            Assert.Equal(TimeSpan.FromHours(8).ToString(), record.ValuesBySourceKey[IntervalsWellnessCatalog.SleepSecondsKey]);
        }

        [Fact]
        public async Task Fetch_ToleratesSnakeCaseSpelling()
        {
            // Both spellings are in circulation for this API; the provider matches keys with
            // case and underscores ignored so either resolves.
            var (provider, _) = Build(HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "sleep_secs": 28800, "resting_hr": 52 }]
                """);

            var record = Assert.Single(await Fetch(provider, Connection()));

            Assert.Equal(TimeSpan.FromHours(8).ToString(), record.ValuesBySourceKey[IntervalsWellnessCatalog.SleepSecondsKey]);
            Assert.Equal("52", record.ValuesBySourceKey["restingHR"]);
        }

        [Fact]
        public async Task Fetch_UnloggedMetricsArePresentAndNull_NotZero()
        {
            var (provider, _) = Build(HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "restingHR": 52, "hrv": null }]
                """);

            var record = Assert.Single(await Fetch(provider, Connection()));

            // Present, so a mapping's SkipWhenNull has something to act on; null rather than 0,
            // which would drag every average and chart around.
            Assert.True(record.ValuesBySourceKey.ContainsKey("hrv"));
            Assert.Null(record.ValuesBySourceKey["hrv"]);

            // A key the payload omitted entirely reads the same way -- the record is a full
            // daily snapshot, so absence means "not logged", not "no opinion".
            Assert.True(record.ValuesBySourceKey.ContainsKey("steps"));
            Assert.Null(record.ValuesBySourceKey["steps"]);
        }

        [Fact]
        public async Task Fetch_ReadsBoolsAndStrings()
        {
            var (provider, _) = Build(HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "locked": true, "comments": "felt strong" }]
                """);

            var record = Assert.Single(await Fetch(provider, Connection()));

            Assert.Equal(bool.TrueString, record.ValuesBySourceKey["locked"]);
            Assert.Equal("felt strong", record.ValuesBySourceKey["comments"]);
        }

        [Fact]
        public async Task Fetch_ValueOfAnUnexpectedShape_ReadsAsNullRatherThanThrowing()
        {
            // A number field arriving as a string, or an object where a scalar was expected:
            // one odd value must not cost the whole record.
            var (provider, _) = Build(HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "restingHR": { "nested": 1 }, "hrv": 44 }]
                """);

            var record = Assert.Single(await Fetch(provider, Connection()));

            Assert.Null(record.ValuesBySourceKey["restingHR"]);
            Assert.Equal("44", record.ValuesBySourceKey["hrv"]);
        }

        [Fact]
        public async Task Fetch_RecordWithoutAnId_IsDropped()
        {
            var (provider, _) = Build(HttpStatusCode.OK, """
                [{ "restingHR": 52 }, { "id": "2026-01-06", "restingHR": 53 }]
                """);

            // Without an id there is no idempotency key, so the record cannot be written safely.
            var record = Assert.Single(await Fetch(provider, Connection()));
            Assert.Equal("2026-01-06", record.ExternalId);
        }

        [Fact]
        public async Task Fetch_UnknownResourceType_YieldsNothing()
        {
            var (provider, handler) = Build(HttpStatusCode.OK, "[]");
            var window = new SyncWindow(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), null);

            var records = new List<SourceRecord>();
            await foreach (var record in provider.FetchAsync(Connection(), "nonsense", window))
                records.Add(record);

            Assert.Empty(records);
            Assert.Empty(handler.Requests);
        }

        // ---- activities ----

        private static async Task<List<SourceRecord>> FetchActivities(IntervalsProvider provider, ProviderConnection connection)
        {
            var window = new SyncWindow(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), null);
            var records = new List<SourceRecord>();

            await foreach (var record in provider.FetchAsync(connection, IntervalsActivitiesCatalog.ResourceType, window))
                records.Add(record);

            return records;
        }

        [Fact]
        public async Task Fetch_Activities_HitsTheActivitiesRouteWithTheDateRange()
        {
            var (provider, handler) = Build(HttpStatusCode.OK, "[]");

            await FetchActivities(provider, Connection());

            var request = Assert.Single(handler.Requests);
            Assert.Contains("api/v1/athlete/i123/activities", request.RequestUri!.ToString());
            Assert.Contains("oldest=2026-01-01", request.RequestUri.Query);
            Assert.Contains("newest=2026-01-31", request.RequestUri.Query);
        }

        [Fact]
        public async Task Fetch_Activities_UsesTheActivityIdAsExternalId_AndHasNoCursor()
        {
            var (provider, _) = Build(HttpStatusCode.OK, """
                [{ "id": "i7712345", "start_date_local": "2026-01-05T07:30:00", "type": "Ride" }]
                """);

            var record = Assert.Single(await FetchActivities(provider, Connection()));

            Assert.Equal("i7712345", record.ExternalId);
            Assert.Equal(SourceOperation.Upsert, record.Operation);
            // Activities carry no revision timestamp, so every record reads as fresh.
            Assert.Null(record.UpdatedAt);
        }

        [Fact]
        public async Task Fetch_Activities_ConvertsMovingTimeSecondsToATimeSpan()
        {
            var (provider, _) = Build(HttpStatusCode.OK, """
                [{ "id": "i7712345", "moving_time": 3600, "elapsed_time": 3900 }]
                """);

            var record = Assert.Single(await FetchActivities(provider, Connection()));

            Assert.Equal(TimeSpan.FromHours(1).ToString(), record.ValuesBySourceKey["moving_time"]);
            Assert.Equal(TimeSpan.FromMinutes(65).ToString(), record.ValuesBySourceKey["elapsed_time"]);
        }

        [Fact]
        public async Task Fetch_Activities_ReadsTheTrainingSubsetAndLeavesUnloggedMetricsNull()
        {
            var (provider, _) = Build(HttpStatusCode.OK, """
                [{
                    "id": "i7712345",
                    "type": "Run",
                    "name": "Morning run",
                    "distance": 10000.0,
                    "icu_average_watts": 240,
                    "average_heartrate": 148,
                    "icu_training_load": 62,
                    "feel": 4,
                    "icu_ctl": null
                }]
                """);

            var record = Assert.Single(await FetchActivities(provider, Connection()));

            Assert.Equal("Run", record.ValuesBySourceKey["type"]);
            Assert.Equal("Morning run", record.ValuesBySourceKey["name"]);
            Assert.Equal("10000", record.ValuesBySourceKey["distance"]);
            Assert.Equal("240", record.ValuesBySourceKey["icu_average_watts"]);
            Assert.Equal("148", record.ValuesBySourceKey["average_heartrate"]);
            Assert.Equal("62", record.ValuesBySourceKey["icu_training_load"]);
            Assert.Equal("4", record.ValuesBySourceKey["feel"]);

            // Present so SkipWhenNull has something to act on, null rather than 0.
            Assert.True(record.ValuesBySourceKey.ContainsKey("icu_ctl"));
            Assert.Null(record.ValuesBySourceKey["icu_ctl"]);
            Assert.True(record.ValuesBySourceKey.ContainsKey("polarization_index"));
            Assert.Null(record.ValuesBySourceKey["polarization_index"]);
        }

        [Fact]
        public async Task Fetch_Activities_RecordWithoutAnId_IsDropped()
        {
            var (provider, _) = Build(HttpStatusCode.OK, """
                [{ "type": "Ride" }, { "id": "i7712346", "type": "Run" }]
                """);

            var record = Assert.Single(await FetchActivities(provider, Connection()));
            Assert.Equal("i7712346", record.ExternalId);
        }

        [Fact]
        public async Task Fetch_NonSuccessResponse_Throws_SoTheExecutorCanRecordIt()
        {
            var (provider, _) = Build(HttpStatusCode.Unauthorized, "");

            await Assert.ThrowsAsync<HttpRequestException>(() => Fetch(provider, Connection()));
        }

        [Fact]
        public async Task Validate_GoodKey_ResolvesTheAthlete()
        {
            var (provider, handler) = Build(HttpStatusCode.OK, """
                { "id": "i123", "name": "Test Athlete" }
                """);

            var result = await provider.ValidateCredentialAsync(Connection());

            Assert.True(result.IsSuccess);
            Assert.Equal("i123", result.Data.ExternalAccountId);
            Assert.Equal("Test Athlete", result.Data.DisplayName);
            Assert.Contains("api/v1/athlete/0", handler.Requests.Single().RequestUri!.ToString());
        }

        [Fact]
        public async Task Validate_RejectedKey_FailsWithSomethingWorthShowing()
        {
            var (provider, _) = Build(HttpStatusCode.Unauthorized, "");

            var result = await provider.ValidateCredentialAsync(Connection());

            Assert.True(result.IsFailure);
            Assert.Contains(result.Messages, m => m.Contains("rejected"));
        }

        [Fact]
        public async Task Validate_EmptyKey_IsRefusedWithoutACall()
        {
            var (provider, handler) = Build(HttpStatusCode.OK, "{}");

            var result = await provider.ValidateCredentialAsync(new ProviderConnection(null, "  ", null));

            Assert.True(result.IsFailure);
            Assert.Empty(handler.Requests);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Operum.Model;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.Constants.Integrations;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Integrations;
using Operum.Model.DTOs.Integrations.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Service.Integrations.Intervals;
using Operum.Tests.Extensions;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Integrations
{
    public class IntegrationsApiTests(IntegrationsEnabledFactory factory) : IClassFixture<IntegrationsEnabledFactory>
    {
        private readonly IntegrationsEnabledFactory _factory = factory;

        private static async Task<JsonElement> Data(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            if (!json.TryGetProperty("data", out var data))
                throw new Exception($"Response had no 'data' property. Status: {response.StatusCode}. Body: {body}");
            return data;
        }

        private static async Task<List<string>> Messages(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            return json.TryGetProperty("messages", out var messages)
                ? [.. messages.EnumerateArray().Select(m => m.GetString()!)]
                : [];
        }

        private static int _userCounter;

        /// <summary>
        /// A brand new user per test. Tests in a class share one database, and one user may
        /// hold only MaxIntegrationCount connections and MaxTrackerCount trackers -- with a
        /// shared account the later tests in the class would fail on those caps rather than on
        /// what they are actually testing.
        /// </summary>
        private Task<HttpClient> AuthenticatedClient() =>
            _factory.NewUserClient($"itest{Interlocked.Increment(ref _userCounter)}");

        private static async Task<(string TrackerId, Dictionary<string, string> Fields)> CreateTracker(
            HttpClient client, string name)
        {
            var trackerId = (await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = name })))
                .GetProperty("id").GetString()!;

            await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Day", Type = DataTypes.Date });
            await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Resting HR", Type = DataTypes.Number });
            await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Sleep", Type = DataTypes.TimeSpan });

            var fieldsJson = await Data(await client.GetAsync($"trackers/{trackerId}/fields"));
            var fields = fieldsJson.EnumerateArray().ToDictionary(
                f => f.GetProperty("name").GetString()!,
                f => f.GetProperty("id").GetString()!);

            return (trackerId, fields);
        }

        /// <summary>
        /// Connects with a fresh athlete id each time, since tests in a class share one
        /// database and a user may hold only one connection per provider account.
        /// </summary>
        private async Task<string> Connect(HttpClient client, string athleteId)
        {
            _factory.IntervalsResponse = (HttpStatusCode.OK, $$"""{ "id": "{{athleteId}}", "name": "Athlete" }""");

            var response = await client.PostAsJsonAsync("integrations",
                new ConnectIntegrationDto { Provider = IntervalsProvider.ProviderKey, Credential = "good-key" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await Data(response)).GetProperty("id").GetString()!;
        }

        private static SaveIntegrationTargetDto TargetFor(string trackerId, Dictionary<string, string> fields) => new()
        {
            TrackerId = trackerId,
            ResourceType = IntervalsWellnessCatalog.ResourceType,
            Mappings =
            [
                new FieldMappingDto { SourceKey = IntervalsWellnessCatalog.RecordKey, FieldId = fields["Day"] },
                new FieldMappingDto { SourceKey = "restingHR", FieldId = fields["Resting HR"] },
                new FieldMappingDto { SourceKey = IntervalsWellnessCatalog.SleepSecondsKey, FieldId = fields["Sleep"] },
            ],
        };

        // ---- providers ----

        [Fact]
        public async Task Providers_ListsIntervalsWithItsCatalog()
        {
            var client = await AuthenticatedClient();

            var data = await Data(await client.GetAsync("integrations/providers"));
            var intervals = data.EnumerateArray()
                .Single(p => p.GetProperty("key").GetString() == IntervalsProvider.ProviderKey);

            Assert.True(intervals.GetProperty("supportsPull").GetBoolean());
            Assert.False(intervals.GetProperty("supportsPush").GetBoolean());
            Assert.False(intervals.GetProperty("requiresBaseUrl").GetBoolean());

            var resources = intervals.GetProperty("resources").EnumerateArray().ToList();

            var wellness = resources
                .Single(r => r.GetProperty("resourceType").GetString() == IntervalsWellnessCatalog.ResourceType);
            Assert.NotEmpty(wellness.GetProperty("fields").EnumerateArray());

            var activities = resources
                .Single(r => r.GetProperty("resourceType").GetString() == IntervalsActivitiesCatalog.ResourceType);
            Assert.NotEmpty(activities.GetProperty("fields").EnumerateArray());
        }

        // ---- connecting ----

        [Fact]
        public async Task Connect_StoresTheAccountAndMasksTheCredential()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-connect");

            var data = await Data(await client.GetAsync("integrations"));
            var integration = data.EnumerateArray()
                .Single(i => i.GetProperty("id").GetString() == integrationId);

            Assert.Equal("athlete-connect", integration.GetProperty("externalAccountId").GetString());

            // A suffix, never the key.
            var masked = integration.GetProperty("maskedCredential").GetString()!;
            Assert.Equal("…-key", masked);
            Assert.DoesNotContain("good-key", masked);
        }

        [Fact]
        public async Task Connect_NeverReturnsTheRawCredentialAnywhere()
        {
            var client = await AuthenticatedClient();
            await Connect(client, "athlete-secrecy");

            var body = await (await client.GetAsync("integrations")).Content.ReadAsStringAsync();
            Assert.DoesNotContain("good-key", body);
        }

        [Fact]
        public async Task Connect_RejectedCredential_IsRefusedBeforeAnythingIsStored()
        {
            var client = await AuthenticatedClient();
            _factory.IntervalsResponse = (HttpStatusCode.Unauthorized, "");

            var response = await client.PostAsJsonAsync("integrations",
                new ConnectIntegrationDto { Provider = IntervalsProvider.ProviderKey, Credential = "bad-key" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("rejected"));

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            Assert.False(await db.Integrations.AnyAsync(i => i.ExternalAccountId == null));
        }

        [Fact]
        public async Task Connect_UnknownProvider_IsRefused()
        {
            var client = await AuthenticatedClient();

            var response = await client.PostAsJsonAsync("integrations",
                new ConnectIntegrationDto { Provider = "not-a-provider", Credential = "x" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Connect_SameAccountTwice_Conflicts()
        {
            var client = await AuthenticatedClient();
            await Connect(client, "athlete-duplicate");

            _factory.IntervalsResponse = (HttpStatusCode.OK, """{ "id": "athlete-duplicate", "name": "Athlete" }""");
            var second = await client.PostAsJsonAsync("integrations",
                new ConnectIntegrationDto { Provider = IntervalsProvider.ProviderKey, Credential = "good-key" });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Connect_PrivateBaseUrl_IsRefused()
        {
            var client = await AuthenticatedClient();

            // A user-supplied instance address is a request this server will make, so an
            // address pointing back into its own network must not be accepted.
            foreach (var badUrl in new[] { "https://169.254.169.254/", "https://10.0.0.5/", "https://localhost/", "http://example.com/" })
            {
                var response = await client.PostAsJsonAsync("integrations",
                    new ConnectIntegrationDto
                    {
                        Provider = IntervalsProvider.ProviderKey,
                        Credential = "good-key",
                        BaseUrl = badUrl,
                    });

                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        // ---- targets ----

        [Fact]
        public async Task CreateTarget_WithValidMappings_Succeeds()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-target");
            var (trackerId, fields) = await CreateTracker(client, "Wellness target");

            var response = await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var target = await Data(response);
            Assert.Equal("Pull", target.GetProperty("mode").GetString());
            Assert.Equal(3, target.GetProperty("mappings").GetArrayLength());
            Assert.Equal("Never", target.GetProperty("lastSyncStatus").GetString());

            // intervals.icu is pull-only, so no webhook is provisioned.
            Assert.Equal(JsonValueKind.Null, target.GetProperty("webhookUrl").ValueKind);
        }

        [Fact]
        public async Task CreateTarget_MismatchedTypes_AreRefusedAtSaveTime()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-types");
            var (trackerId, fields) = await CreateTracker(client, "Type mismatch");

            var dto = TargetFor(trackerId, fields);
            // comments is a string; Resting HR is a number.
            dto.Mappings = [new FieldMappingDto { SourceKey = "comments", FieldId = fields["Resting HR"] }];

            var response = await client.PostAsJsonAsync($"integrations/{integrationId}/targets", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(await Messages(response), m => m.Contains("cannot fill"));
        }

        [Fact]
        public async Task CreateTarget_UnknownSourceKey_IsRefused()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-unknown-key");
            var (trackerId, fields) = await CreateTracker(client, "Unknown key");

            var dto = TargetFor(trackerId, fields);
            dto.Mappings = [new FieldMappingDto { SourceKey = "notARealMetric", FieldId = fields["Resting HR"] }];

            var response = await client.PostAsJsonAsync($"integrations/{integrationId}/targets", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateTarget_NoMappings_IsRefusedByValidation()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-empty");
            var (trackerId, _) = await CreateTracker(client, "No mappings");

            var response = await client.PostAsJsonAsync($"integrations/{integrationId}/targets",
                new SaveIntegrationTargetDto
                {
                    TrackerId = trackerId,
                    ResourceType = IntervalsWellnessCatalog.ResourceType,
                    Mappings = [],
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateTarget_UnsupportedResource_IsRefused()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-resource");
            var (trackerId, fields) = await CreateTracker(client, "Bad resource");

            var dto = TargetFor(trackerId, fields);
            dto.ResourceType = "nonsense";

            var response = await client.PostAsJsonAsync($"integrations/{integrationId}/targets", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateTarget_PushModeOnAPullOnlyProvider_IsRefused()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-mode");
            var (trackerId, fields) = await CreateTracker(client, "Bad mode");

            var dto = TargetFor(trackerId, fields);
            dto.Mode = "Push";

            var response = await client.PostAsJsonAsync($"integrations/{integrationId}/targets", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateTarget_SameTrackerAndResourceTwice_Conflicts()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-dupe-target");
            var (trackerId, fields) = await CreateTracker(client, "Duplicate target");

            await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields));
            var second = await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields));

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task CreateTarget_OnSomeoneElsesTracker_IsForbidden()
        {
            var owner = await AuthenticatedClient();
            var (trackerId, fields) = await CreateTracker(owner, "Owner's tracker");

            var other = await _factory.NewUserClient("outsider");
            var otherIntegrationId = await Connect(other, "athlete-outsider");

            // Owner-only, matching how tracker metadata and collaborators already work.
            var response = await other.PostAsJsonAsync($"integrations/{otherIntegrationId}/targets", TargetFor(trackerId, fields));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Targets_OfAnotherUsersIntegration_AreNotFound()
        {
            var owner = await AuthenticatedClient();
            var integrationId = await Connect(owner, "athlete-private");

            var other = await _factory.NewUserClient("nosy");
            var response = await other.DeleteAsync($"integrations/{integrationId}");

            // Not Forbidden: another user's connection should not be confirmed to exist.
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTarget_ReplacesMappings()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-update");
            var (trackerId, fields) = await CreateTracker(client, "Update mappings");

            var created = await Data(await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields)));
            var targetId = created.GetProperty("id").GetString()!;

            var dto = TargetFor(trackerId, fields);
            dto.Mappings = [new FieldMappingDto { SourceKey = "restingHR", FieldId = fields["Resting HR"] }];
            dto.IsEnabled = false;

            var updated = await Data(await client.PutAsJsonAsync($"integrations/{integrationId}/targets/{targetId}", dto));

            Assert.Equal(1, updated.GetProperty("mappings").GetArrayLength());
            Assert.False(updated.GetProperty("isEnabled").GetBoolean());
        }

        [Fact]
        public async Task UpdateTarget_MovingItToAnotherTracker_IsRefused()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-move");
            var (trackerId, fields) = await CreateTracker(client, "Original tracker");
            var (otherTrackerId, otherFields) = await CreateTracker(client, "Other tracker");

            var created = await Data(await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields)));
            var targetId = created.GetProperty("id").GetString()!;

            // Moving it would orphan everything already imported under the old pairing.
            var response = await client.PutAsJsonAsync($"integrations/{integrationId}/targets/{targetId}",
                TargetFor(otherTrackerId, otherFields));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTarget_LeavesImportedEntriesAlone()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-delete");
            var (trackerId, fields) = await CreateTracker(client, "Delete target");

            var created = await Data(await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields)));
            var targetId = created.GetProperty("id").GetString()!;

            _factory.IntervalsResponse = (HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "updated": "2026-01-05T10:00:00Z", "restingHR": 52, "sleepSecs": 28800 }]
                """);
            await client.PostAsync($"integrations/{integrationId}/targets/{targetId}/sync", null);

            Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"integrations/{integrationId}/targets/{targetId}")).StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();

            // The data is the user's, not the integration's.
            Assert.Equal(1, await db.Entries.CountAsync(e => e.TrackerId == trackerId));
        }

        // ---- sync now ----

        [Fact]
        public async Task SyncNow_ImportsThroughTheWholePipeline()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-sync");
            var (trackerId, fields) = await CreateTracker(client, "Sync now");

            var created = await Data(await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields)));
            var targetId = created.GetProperty("id").GetString()!;

            _factory.IntervalsResponse = (HttpStatusCode.OK, """
                [
                  { "id": "2026-01-05", "updated": "2026-01-05T10:00:00Z", "restingHR": 52, "sleepSecs": 28800 },
                  { "id": "2026-01-06", "updated": "2026-01-06T10:00:00Z", "restingHR": 54, "sleepSecs": null }
                ]
                """);

            var result = await Data(await client.PostAsync($"integrations/{integrationId}/targets/{targetId}/sync", null));
            Assert.Equal(2, result.GetProperty("created").GetInt32());

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var entries = await db.Entries.Include(e => e.FieldValues)
                .Where(e => e.TrackerId == trackerId).ToListAsync();

            Assert.Equal(2, entries.Count);
            Assert.All(entries, e => Assert.Equal(IntervalsProvider.ProviderKey, e.Source));

            var first = entries.Single(e => e.ExternalId == "2026-01-05");
            Assert.Equal(52, first.FieldValues.Single(fv => fv.FieldId == fields["Resting HR"]).NumberValue);
            Assert.Equal(TimeSpan.FromHours(8), first.FieldValues.Single(fv => fv.FieldId == fields["Sleep"]).TimeSpanValue);

            // Sleep was null on the second day and the mapping skips nulls, so no value was
            // written for it at all.
            var second = entries.Single(e => e.ExternalId == "2026-01-06");
            Assert.DoesNotContain(second.FieldValues, fv => fv.FieldId == fields["Sleep"]);
        }

        [Fact]
        public async Task SyncNow_RunTwice_UpdatesRatherThanDuplicates()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-resync");
            var (trackerId, fields) = await CreateTracker(client, "Resync");

            var created = await Data(await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields)));
            var targetId = created.GetProperty("id").GetString()!;

            _factory.IntervalsResponse = (HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "updated": "2026-01-05T10:00:00Z", "restingHR": 52 }]
                """);
            await client.PostAsync($"integrations/{integrationId}/targets/{targetId}/sync", null);

            // Revised upstream: newer timestamp, so the cursor lets it through.
            _factory.IntervalsResponse = (HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "updated": "2026-01-05T18:00:00Z", "restingHR": 49 }]
                """);
            var second = await Data(await client.PostAsync($"integrations/{integrationId}/targets/{targetId}/sync", null));

            Assert.Equal(0, second.GetProperty("created").GetInt32());
            Assert.Equal(1, second.GetProperty("updated").GetInt32());

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var entry = await db.Entries.Include(e => e.FieldValues)
                .SingleAsync(e => e.TrackerId == trackerId);

            Assert.Equal(49, entry.FieldValues.Single(fv => fv.FieldId == fields["Resting HR"]).NumberValue);
        }

        [Fact]
        public async Task Resync_BackfillsAFieldMappedAfterTheFirstImport()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-resync-map");
            var (trackerId, fields) = await CreateTracker(client, "Resync mapping");

            // First import covers only Day and Resting HR.
            var dto = TargetFor(trackerId, fields);
            dto.Mappings =
            [
                new FieldMappingDto { SourceKey = IntervalsWellnessCatalog.RecordKey, FieldId = fields["Day"] },
                new FieldMappingDto { SourceKey = "restingHR", FieldId = fields["Resting HR"] },
            ];

            var created = await Data(await client.PostAsJsonAsync($"integrations/{integrationId}/targets", dto));
            var targetId = created.GetProperty("id").GetString()!;

            _factory.IntervalsResponse = (HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "updated": "2026-01-05T10:00:00Z", "restingHR": 52, "sleepSecs": 28800 }]
                """);
            await client.PostAsync($"integrations/{integrationId}/targets/{targetId}/sync", null);

            // Sleep is mapped only now, after that record was already imported.
            dto.Mappings =
            [
                .. dto.Mappings,
                new FieldMappingDto { SourceKey = IntervalsWellnessCatalog.SleepSecondsKey, FieldId = fields["Sleep"] },
            ];
            await client.PutAsJsonAsync($"integrations/{integrationId}/targets/{targetId}", dto);

            var resync = await Data(await client.PostAsync($"integrations/{integrationId}/targets/{targetId}/resync", null));
            Assert.Equal(0, resync.GetProperty("created").GetInt32());
            Assert.Equal(1, resync.GetProperty("updated").GetInt32());

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            var entry = await db.Entries.Include(e => e.FieldValues)
                .SingleAsync(e => e.TrackerId == trackerId);

            // The day that was already imported now carries the newly mapped field.
            Assert.Equal(TimeSpan.FromHours(8),
                entry.FieldValues.Single(fv => fv.FieldId == fields["Sleep"]).TimeSpanValue);
        }

        [Fact]
        public async Task Resync_OfAnotherUsersConnection_IsNotFound()
        {
            var owner = await AuthenticatedClient();
            var integrationId = await Connect(owner, "athlete-resync-private");
            var (trackerId, fields) = await CreateTracker(owner, "Resync private");

            var created = await Data(await owner.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields)));
            var targetId = created.GetProperty("id").GetString()!;

            var other = await _factory.NewUserClient("resync-nosy");
            var response = await other.PostAsync($"integrations/{integrationId}/targets/{targetId}/resync", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task SyncIntegration_PullsEveryTrackerFromOneFetch()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-group-sync");

            var (trackerA, fieldsA) = await CreateTracker(client, "Group A");
            var (trackerB, fieldsB) = await CreateTracker(client, "Group B");
            await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerA, fieldsA));
            await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerB, fieldsB));

            _factory.IntervalsResponse = (HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "updated": "2026-01-05T10:00:00Z", "restingHR": 52, "sleepSecs": 28800 }]
                """);

            var before = _factory.IntervalsHandler.Requests
                .Count(r => r.RequestUri!.ToString().Contains("/athlete/athlete-group-sync/wellness"));

            var result = await Data(await client.PostAsync($"integrations/{integrationId}/sync", null));
            Assert.Equal(2, result.GetProperty("created").GetInt32());

            // Two trackers, one call to intervals.icu -- not one per tracker.
            var after = _factory.IntervalsHandler.Requests
                .Count(r => r.RequestUri!.ToString().Contains("/athlete/athlete-group-sync/wellness"));
            Assert.Equal(1, after - before);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            Assert.Equal(1, await db.Entries.CountAsync(e => e.TrackerId == trackerA));
            Assert.Equal(1, await db.Entries.CountAsync(e => e.TrackerId == trackerB));
        }

        [Fact]
        public async Task SyncIntegration_OfAnotherUsersConnection_IsNotFound()
        {
            var owner = await AuthenticatedClient();
            var integrationId = await Connect(owner, "athlete-group-private");

            var other = await _factory.NewUserClient("group-nosy");
            var response = await other.PostAsync($"integrations/{integrationId}/sync", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task SyncNow_ProviderFailure_IsReportedAndRecorded()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-failure");
            var (trackerId, fields) = await CreateTracker(client, "Sync failure");

            var created = await Data(await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields)));
            var targetId = created.GetProperty("id").GetString()!;

            _factory.IntervalsResponse = (HttpStatusCode.Unauthorized, "");
            var response = await client.PostAsync($"integrations/{integrationId}/targets/{targetId}/sync", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var listed = await Data(await client.GetAsync("integrations"));
            var target = listed.EnumerateArray()
                .Single(i => i.GetProperty("id").GetString() == integrationId)
                .GetProperty("targets").EnumerateArray().Single();

            Assert.Equal("Error", target.GetProperty("lastSyncStatus").GetString());
            Assert.False(string.IsNullOrEmpty(target.GetProperty("lastSyncError").GetString()));
        }

        [Fact]
        public async Task RotateSecret_OnAPullTarget_IsRefused()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-rotate");
            var (trackerId, fields) = await CreateTracker(client, "Rotate");

            var created = await Data(await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields)));
            var targetId = created.GetProperty("id").GetString()!;

            var response = await client.PostAsync($"integrations/{integrationId}/targets/{targetId}/rotate-secret", null);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ---- disconnecting ----

        [Fact]
        public async Task Disconnect_RemovesTheConnectionButNotTheData()
        {
            var client = await AuthenticatedClient();
            var integrationId = await Connect(client, "athlete-disconnect");
            var (trackerId, fields) = await CreateTracker(client, "Disconnect");

            var created = await Data(await client.PostAsJsonAsync($"integrations/{integrationId}/targets", TargetFor(trackerId, fields)));
            var targetId = created.GetProperty("id").GetString()!;

            _factory.IntervalsResponse = (HttpStatusCode.OK, """
                [{ "id": "2026-01-05", "updated": "2026-01-05T10:00:00Z", "restingHR": 52 }]
                """);
            await client.PostAsync($"integrations/{integrationId}/targets/{targetId}/sync", null);

            Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"integrations/{integrationId}")).StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();

            Assert.False(await db.Integrations.AnyAsync(i => i.Id == integrationId));
            Assert.False(await db.IntegrationTargets.AnyAsync(t => t.Id == targetId));
            Assert.Equal(1, await db.Entries.CountAsync(e => e.TrackerId == trackerId));
        }
    }

    /// <summary>
    /// The same API with the feature switched off, which is how it ships until a deployment
    /// opts in.
    /// </summary>
    public class IntegrationsDisabledTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        [Fact]
        public async Task EveryEndpoint_AnswersNotFound()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);

            // 404 rather than 403: with the feature off the route should look as though it
            // never existed.
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("integrations")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("integrations/providers")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("integrations",
                new ConnectIntegrationDto { Provider = IntervalsProvider.ProviderKey, Credential = "x" })).StatusCode);
        }
    }
}

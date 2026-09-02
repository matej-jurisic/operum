using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Operum.Model;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.Integrations;
using Operum.Model.Models;
using Operum.Service.Integrations;
using Operum.Service.Interfaces;
using Operum.Service.Services.Integrations;
using Operum.Tests.Extensions;
using Operum.Tests.Mocks;
using Operum.Tests.Util;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Integrations
{
    /// <summary>
    /// The sync executor against a fake provider: window and cursor arithmetic, and what a
    /// failing target does to everything around it.
    /// </summary>
    public class IntegrationSyncExecutorTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
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

        /// <summary>
        /// A tracker with two fields, plus a connection and a pull target wired onto it.
        /// Returns the target id and the field ids by name.
        /// </summary>
        private async Task<(string TargetId, string TrackerId, Dictionary<string, string> Fields)> Wire(
            string trackerName, IntegrationMode mode = IntegrationMode.Pull, string providerKey = "fake")
        {
            var client = await AuthenticatedClient();

            var trackerId = (await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = trackerName })))
                .GetProperty("id").GetString()!;

            await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number });
            await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = "Note", Type = DataTypes.String });

            var fieldsJson = await Data(await client.GetAsync($"trackers/{trackerId}/fields"));
            var fields = fieldsJson.EnumerateArray().ToDictionary(
                f => f.GetProperty("name").GetString()!,
                f => f.GetProperty("id").GetString()!);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();

            var ownerId = await db.Trackers.Where(t => t.Id == trackerId).Select(t => t.OwnerId).SingleAsync();

            var integration = new Integration
            {
                Provider = providerKey,
                UserId = ownerId,
                // Distinct per test: tests in a class share one database, and a user may hold
                // only one connection per provider account.
                ExternalAccountId = $"acct-{trackerName}",
                CredentialCiphertext = null,
            };

            var target = new IntegrationTarget
            {
                IntegrationId = integration.Id,
                TrackerId = trackerId,
                ResourceType = FakeIntegrationProvider.ResourceType,
                Mode = mode,
                BackfillFrom = new DateOnly(2026, 1, 1),
                Mappings =
                [
                    new IntegrationFieldMapping { SourceKey = "amount", FieldId = fields["Amount"] },
                    new IntegrationFieldMapping { SourceKey = "note", FieldId = fields["Note"] },
                ],
            };

            db.Integrations.Add(integration);
            db.IntegrationTargets.Add(target);
            await db.SaveChangesAsync();

            return (target.Id, trackerId, fields);
        }

        /// <summary>
        /// One connection feeding <paramref name="trackerCount"/> trackers the same kind of
        /// data, which is the shape the grouped sync exists for. Returns the integration id
        /// and, per tracker, its target id and field ids by name.
        /// </summary>
        private static int _groupCounter;

        private async Task<(string IntegrationId, List<(string TargetId, string TrackerId, Dictionary<string, string> Fields)> Targets)> WireGroup(
            string namePrefix, int trackerCount, string resourceType = FakeIntegrationProvider.ResourceType)
        {
            // A fresh user per call: this fixture shares one database across the class, and a
            // group of trackers on the shared account would run into the per-user tracker cap.
            var suffix = Interlocked.Increment(ref _groupCounter);
            var client = await _factory.NewUserClient($"groupsync{suffix}");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();

            var integration = new Integration
            {
                Provider = "fake",
                // Resolved below from the first tracker's owner.
                UserId = string.Empty,
                ExternalAccountId = $"acct-{namePrefix}-{suffix}",
                CredentialCiphertext = null,
            };

            var targets = new List<(string, string, Dictionary<string, string>)>();

            for (var i = 0; i < trackerCount; i++)
            {
                var trackerId = (await Data(await client.PostAsJsonAsync("trackers",
                        new CreateTrackerDto { Name = $"{namePrefix} {i}" })))
                    .GetProperty("id").GetString()!;

                await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                    new CreateFieldDto { Name = "Amount", Type = DataTypes.Number });
                await client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                    new CreateFieldDto { Name = "Note", Type = DataTypes.String });

                var fieldsJson = await Data(await client.GetAsync($"trackers/{trackerId}/fields"));
                var fields = fieldsJson.EnumerateArray().ToDictionary(
                    f => f.GetProperty("name").GetString()!,
                    f => f.GetProperty("id").GetString()!);

                if (string.IsNullOrEmpty(integration.UserId))
                    integration.UserId = await db.Trackers.Where(t => t.Id == trackerId).Select(t => t.OwnerId).SingleAsync();

                var target = new IntegrationTarget
                {
                    IntegrationId = integration.Id,
                    TrackerId = trackerId,
                    ResourceType = resourceType,
                    Mode = IntegrationMode.Pull,
                    BackfillFrom = new DateOnly(2026, 1, 1),
                    Mappings =
                    [
                        new IntegrationFieldMapping { SourceKey = "amount", FieldId = fields["Amount"] },
                        new IntegrationFieldMapping { SourceKey = "note", FieldId = fields["Note"] },
                    ],
                };

                db.IntegrationTargets.Add(target);
                targets.Add((target.Id, trackerId, fields));
            }

            db.Integrations.Add(integration);
            await db.SaveChangesAsync();

            return (integration.Id, targets);
        }

        /// <summary>
        /// Built by hand so the fake provider can stand in for the registry's contents; the
        /// rest of the dependencies come from the app's own container.
        /// </summary>
        private IntegrationSyncExecutor Executor(IServiceScope scope, params IIntegrationProvider[] providers) =>
            new(scope.ServiceProvider.GetRequiredService<OperumContext>(),
                new IntegrationProviderRegistry(providers),
                scope.ServiceProvider.GetRequiredService<ICredentialProtector>(),
                scope.ServiceProvider.GetRequiredService<IEntryWriter>(),
                new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Integrations:ReconciliationDays"] = "7",
                    ["Integrations:BatchSize"] = "2",
                }).Build(),
                NullLogger<IntegrationSyncExecutor>.Instance);

        private static SourceRecord Upsert(string id, string amount, DateTime? updatedAt = null) =>
            new(id, SourceOperation.Upsert, updatedAt, new Dictionary<string, string?>
            {
                ["amount"] = amount,
                ["note"] = $"note for {id}",
            });

        private async Task<IntegrationTarget> ReloadTarget(string targetId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            return await db.IntegrationTargets.SingleAsync(t => t.Id == targetId);
        }

        private async Task<List<Entry>> StoredEntries(string trackerId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            return await db.Entries.Include(e => e.FieldValues).Where(e => e.TrackerId == trackerId).ToListAsync();
        }

        [Fact]
        public async Task Sync_FirstRun_ReachesBackToTheBackfillDate()
        {
            var (targetId, _, _) = await Wire("Backfill window");
            var provider = new FakeIntegrationProvider();

            using var scope = _factory.Services.CreateScope();
            await Executor(scope, provider).SyncTargetAsync(targetId);

            var window = Assert.Single(provider.RequestedWindows);
            Assert.Equal(new DateOnly(2026, 1, 1), window.From);
            Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), window.To);
        }

        [Fact]
        public async Task Sync_AfterAFirstRun_OnlyRereadsTheReconciliationWindow()
        {
            var (targetId, _, _) = await Wire("Reconciliation window");
            var provider = new FakeIntegrationProvider();

            using (var scope = _factory.Services.CreateScope())
                await Executor(scope, provider).SyncTargetAsync(targetId);

            using (var scope = _factory.Services.CreateScope())
                await Executor(scope, provider).SyncTargetAsync(targetId);

            // Devices sync late and athletes edit yesterday, so an incremental run still
            // re-reads the last few days rather than only what is new.
            var second = provider.RequestedWindows.Last();
            Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), second.From);
        }

        [Fact]
        public async Task Sync_WritesRecordsThrough_AndRecordsSuccess()
        {
            var (targetId, trackerId, fields) = await Wire("Writes through");
            var provider = new FakeIntegrationProvider();
            provider.Records.Add(Upsert("r1", "10"));
            provider.Records.Add(Upsert("r2", "20"));

            using (var scope = _factory.Services.CreateScope())
            {
                var result = await Executor(scope, provider).SyncTargetAsync(targetId);
                Assert.True(result.IsSuccess);
                Assert.Equal(2, result.Data.Created);
            }

            var entries = await StoredEntries(trackerId);
            Assert.Equal(2, entries.Count);
            Assert.All(entries, e => Assert.Equal("fake", e.Source));
            Assert.Equal(10, entries.Single(e => e.ExternalId == "r1")
                .FieldValues.Single(fv => fv.FieldId == fields["Amount"]).NumberValue);

            var target = await ReloadTarget(targetId);
            Assert.Equal(SyncStatus.Ok, target.LastSyncStatus);
            Assert.Null(target.LastSyncError);
            Assert.NotNull(target.LastSyncedAt);
        }

        [Fact]
        public async Task Sync_BatchesLargerThanTheBatchSize_StillWriteEverything()
        {
            // BatchSize is 2 in these tests, so five records exercise the flush path.
            var (targetId, trackerId, _) = await Wire("Batching");
            var provider = new FakeIntegrationProvider();
            for (var i = 1; i <= 5; i++)
                provider.Records.Add(Upsert($"r{i}", i.ToString()));

            using (var scope = _factory.Services.CreateScope())
            {
                var result = await Executor(scope, provider).SyncTargetAsync(targetId);
                Assert.Equal(5, result.Data.Created);
            }

            Assert.Equal(5, (await StoredEntries(trackerId)).Count);
        }

        [Fact]
        public async Task Sync_AdvancesTheCursorToTheNewestRevisionSeen()
        {
            var (targetId, _, _) = await Wire("Cursor advance");
            var provider = new FakeIntegrationProvider();
            var newest = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

            provider.Records.Add(Upsert("r1", "10", new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc)));
            provider.Records.Add(Upsert("r2", "20", newest));

            using (var scope = _factory.Services.CreateScope())
                await Executor(scope, provider).SyncTargetAsync(targetId);

            Assert.Equal(newest, (await ReloadTarget(targetId)).LastCursor);
        }

        [Fact]
        public async Task Sync_SkipsRecordsUnchangedSinceTheCursor()
        {
            var (targetId, trackerId, _) = await Wire("Cursor skip");
            var provider = new FakeIntegrationProvider();
            var firstRevision = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc);

            provider.Records.Add(Upsert("r1", "10", firstRevision));
            using (var scope = _factory.Services.CreateScope())
                await Executor(scope, provider).SyncTargetAsync(targetId);

            // Same record, same revision timestamp: nothing changed upstream, so there is no
            // reason to write it again.
            using (var scope = _factory.Services.CreateScope())
            {
                var result = await Executor(scope, provider).SyncTargetAsync(targetId);
                Assert.Equal(0, result.Data.Created);
                Assert.Equal(0, result.Data.Updated);
            }

            // A newer revision does come through.
            provider.Records.Clear();
            provider.Records.Add(Upsert("r1", "99", firstRevision.AddHours(1)));
            using (var scope = _factory.Services.CreateScope())
            {
                var result = await Executor(scope, provider).SyncTargetAsync(targetId);
                Assert.Equal(1, result.Data.Updated);
            }

            Assert.Single(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task Resync_ReachesBackToTheBackfillDate_EvenAfterAFirstRun()
        {
            var (targetId, _, _) = await Wire("Resync window");
            var provider = new FakeIntegrationProvider();

            using (var scope = _factory.Services.CreateScope())
                await Executor(scope, provider).SyncTargetAsync(targetId);

            using (var scope = _factory.Services.CreateScope())
                await Executor(scope, provider).SyncTargetAsync(targetId, fullResync: true);

            // A plain incremental run would only re-read the reconciliation window; a full
            // resync goes all the way back to the backfill date.
            var last = provider.RequestedWindows.Last();
            Assert.Equal(new DateOnly(2026, 1, 1), last.From);
            Assert.Null(last.Cursor);
        }

        [Fact]
        public async Task Resync_ReappliesRecordsThatAPlainSyncWouldSkipAsUnchanged()
        {
            var (targetId, trackerId, fields) = await Wire("Resync reapplies");
            var provider = new FakeIntegrationProvider();
            var revision = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc);
            provider.Records.Add(Upsert("r1", "10", revision));

            using (var scope = _factory.Services.CreateScope())
                await Executor(scope, provider).SyncTargetAsync(targetId);

            // Same record, same revision: a plain sync sees nothing to do.
            using (var scope = _factory.Services.CreateScope())
            {
                var result = await Executor(scope, provider).SyncTargetAsync(targetId);
                Assert.Equal(0, result.Data.Updated);
            }

            // The upstream value has since changed, but the revision timestamp did not move --
            // exactly the case a mapping added later needs to pick up. A full resync writes it.
            provider.Records.Clear();
            provider.Records.Add(Upsert("r1", "42", revision));

            using (var scope = _factory.Services.CreateScope())
            {
                var result = await Executor(scope, provider).SyncTargetAsync(targetId, fullResync: true);
                Assert.Equal(1, result.Data.Updated);
            }

            var entry = Assert.Single(await StoredEntries(trackerId));
            Assert.Equal(42, entry.FieldValues.Single(fv => fv.FieldId == fields["Amount"]).NumberValue);
        }

        [Fact]
        public async Task Sync_RecordsWithNoRevisionTimestamp_AreAlwaysReapplied()
        {
            var (targetId, _, _) = await Wire("No cursor");
            var provider = new FakeIntegrationProvider();
            provider.Records.Add(Upsert("r1", "10", updatedAt: null));

            using (var scope = _factory.Services.CreateScope())
                await Executor(scope, provider).SyncTargetAsync(targetId);

            using var second = _factory.Services.CreateScope();
            var result = await Executor(second, provider).SyncTargetAsync(targetId);

            // Nothing to compare against, so the record is considered fresh every time.
            Assert.Equal(1, result.Data.Updated);
        }

        [Fact]
        public async Task Sync_ProviderFailure_IsRecordedOnTheTargetRatherThanThrown()
        {
            var (targetId, trackerId, _) = await Wire("Provider failure");
            var provider = new FakeIntegrationProvider
            {
                FetchThrows = new HttpRequestException("boom", null, System.Net.HttpStatusCode.Unauthorized),
            };

            using (var scope = _factory.Services.CreateScope())
            {
                var result = await Executor(scope, provider).SyncTargetAsync(targetId);
                Assert.True(result.IsFailure);
            }

            var target = await ReloadTarget(targetId);
            Assert.Equal(SyncStatus.Error, target.LastSyncStatus);
            Assert.Contains("401", target.LastSyncError);

            // Nothing half-written.
            Assert.Empty(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task Sync_FailureMessage_DoesNotLeakTheExceptionText()
        {
            var (targetId, _, _) = await Wire("No leak");
            var provider = new FakeIntegrationProvider
            {
                // An exception message can carry a URL with a credential in it.
                FetchThrows = new InvalidOperationException("https://intervals.icu/?key=SECRET123"),
            };

            using (var scope = _factory.Services.CreateScope())
                await Executor(scope, provider).SyncTargetAsync(targetId);

            var target = await ReloadTarget(targetId);
            Assert.DoesNotContain("SECRET123", target.LastSyncError);
        }

        [Fact]
        public async Task Sync_PushTarget_IsRefused()
        {
            var (targetId, _, _) = await Wire("Push target", IntegrationMode.Push);

            using var scope = _factory.Services.CreateScope();
            var result = await Executor(scope, new FakeIntegrationProvider()).SyncTargetAsync(targetId);

            Assert.True(result.IsFailure);
            Assert.Contains(result.Messages, m => m.Contains("webhook"));
        }

        [Fact]
        public async Task Sync_UninstalledProvider_IsRecordedNotCrashed()
        {
            var (targetId, _, _) = await Wire("Missing provider", providerKey: "no-longer-installed");

            using var scope = _factory.Services.CreateScope();
            var result = await Executor(scope, new FakeIntegrationProvider()).SyncTargetAsync(targetId);

            Assert.True(result.IsFailure);
            Assert.Equal(SyncStatus.Error, (await ReloadTarget(targetId)).LastSyncStatus);
        }

        [Fact]
        public async Task Sync_UnknownTarget_IsNotFound()
        {
            using var scope = _factory.Services.CreateScope();
            var result = await Executor(scope, new FakeIntegrationProvider()).SyncTargetAsync("nope");

            Assert.True(result.IsFailure);
            Assert.Equal(Model.Enums.ResultStatusCodes.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task Sync_TrackerOwnedBySomeoneElse_IsRefused()
        {
            var (targetId, _, _) = await Wire("Ownership");

            // The connection's user no longer owns the tracker: the credential must not keep
            // writing into it.
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
                var integration = await db.Integrations
                    .AsTracking()
                    .SingleAsync(i => i.Targets.Any(t => t.Id == targetId));
                integration.UserId = (await db.Users.AsTracking().FirstAsync(u => u.Id != integration.UserId)).Id;
                await db.SaveChangesAsync();
            }

            using var syncScope = _factory.Services.CreateScope();
            var result = await Executor(syncScope, new FakeIntegrationProvider()).SyncTargetAsync(targetId);

            Assert.True(result.IsFailure);
            Assert.Contains(result.Messages, m => m.Contains("owned"));
        }

        // ---- grouped sync: one fetch for many trackers ----

        [Fact]
        public async Task SyncIntegration_FetchesOnce_ForEveryTargetSharingAResourceType()
        {
            var (integrationId, targets) = await WireGroup("Shared fetch", trackerCount: 3);
            var provider = new FakeIntegrationProvider();
            provider.Records.Add(Upsert("r1", "10"));

            using (var scope = _factory.Services.CreateScope())
            {
                var result = await Executor(scope, provider).SyncIntegrationAsync(integrationId);
                Assert.True(result.IsSuccess);
            }

            // Three trackers, one call -- not one call per tracker.
            Assert.Single(provider.RequestedWindows);

            // ...and every tracker still got the record.
            foreach (var (_, trackerId, _) in targets)
                Assert.Single(await StoredEntries(trackerId));
        }

        [Fact]
        public async Task SyncIntegration_WindowReachesBackForWhicheverTargetNeedsItMost()
        {
            var (integrationId, targets) = await WireGroup("Union window", trackerCount: 2);
            var provider = new FakeIntegrationProvider();

            // One target has already synced and only wants the reconciliation window; the
            // other is still on its first run and needs the full backfill.
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
                var synced = await db.IntegrationTargets.AsTracking().SingleAsync(t => t.Id == targets[0].TargetId);
                synced.LastSyncedAt = DateTime.UtcNow.AddDays(-1);
                await db.SaveChangesAsync();
            }

            using (var scope = _factory.Services.CreateScope())
                await Executor(scope, provider).SyncIntegrationAsync(integrationId);

            var window = Assert.Single(provider.RequestedWindows);
            Assert.Equal(new DateOnly(2026, 1, 1), window.From);
        }

        [Fact]
        public async Task SyncIntegration_OneTargetFailing_DoesNotStopTheOthers()
        {
            var (integrationId, targets) = await WireGroup("Partial failure", trackerCount: 2);
            var provider = new FakeIntegrationProvider();
            provider.Records.Add(Upsert("r1", "10"));

            // The first target's tracker is now owned by someone else, so the connection may
            // no longer write into it; the second target is untouched.
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
                var tracker = await db.Trackers.AsTracking().SingleAsync(t => t.Id == targets[0].TrackerId);
                tracker.OwnerId = (await db.Users.FirstAsync(u => u.Id != tracker.OwnerId)).Id;
                await db.SaveChangesAsync();
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var result = await Executor(scope, provider).SyncIntegrationAsync(integrationId);
                // The second target got its record, so the overall result is a success even
                // though the first one was refused.
                Assert.True(result.IsSuccess);
            }

            Assert.Equal(SyncStatus.Error, (await ReloadTarget(targets[0].TargetId)).LastSyncStatus);
            Assert.Single(await StoredEntries(targets[1].TrackerId));
        }

        [Fact]
        public async Task SyncIntegration_ProviderFailure_IsRecordedOnEveryTarget()
        {
            var (integrationId, targets) = await WireGroup("Fetch failure", trackerCount: 2);
            var provider = new FakeIntegrationProvider
            {
                FetchThrows = new HttpRequestException("boom", null, System.Net.HttpStatusCode.Unauthorized),
            };

            using (var scope = _factory.Services.CreateScope())
            {
                var result = await Executor(scope, provider).SyncIntegrationAsync(integrationId);
                Assert.True(result.IsFailure);
            }

            foreach (var (targetId, _, _) in targets)
            {
                var target = await ReloadTarget(targetId);
                Assert.Equal(SyncStatus.Error, target.LastSyncStatus);
                Assert.Contains("401", target.LastSyncError);
            }
        }

        [Fact]
        public async Task SyncIntegration_DisabledTarget_IsLeftOut()
        {
            var (integrationId, targets) = await WireGroup("Disabled target", trackerCount: 2);
            var provider = new FakeIntegrationProvider();
            provider.Records.Add(Upsert("r1", "10"));

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
                var off = await db.IntegrationTargets.AsTracking().SingleAsync(t => t.Id == targets[0].TargetId);
                off.IsEnabled = false;
                await db.SaveChangesAsync();
            }

            using (var scope = _factory.Services.CreateScope())
                await Executor(scope, provider).SyncIntegrationAsync(integrationId);

            Assert.Empty(await StoredEntries(targets[0].TrackerId));
            Assert.Single(await StoredEntries(targets[1].TrackerId));
        }

        [Fact]
        public async Task SyncIntegration_UnknownIntegration_IsNotFound()
        {
            using var scope = _factory.Services.CreateScope();
            var result = await Executor(scope, new FakeIntegrationProvider()).SyncIntegrationAsync("nope");

            Assert.True(result.IsFailure);
            Assert.Equal(Model.Enums.ResultStatusCodes.NotFound, result.StatusCode);
        }
    }
}

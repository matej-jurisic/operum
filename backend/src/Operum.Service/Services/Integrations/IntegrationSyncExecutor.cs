using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Integrations;
using Operum.Model.Models;
using Operum.Service.Domain.Integrations;
using Operum.Service.Integrations;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Integrations
{
    public class IntegrationSyncExecutor(
        OperumContext db,
        IIntegrationProviderRegistry registry,
        ICredentialProtector credentialProtector,
        IEntryWriter entryWriter,
        IConfiguration configuration,
        ILogger<IntegrationSyncExecutor> logger) : IIntegrationSyncExecutor
    {
        /// <summary>
        /// How far back an incremental sync re-reads. Devices sync late and athletes edit
        /// yesterday, so a window of only "since last time" would miss revisions; the cursor
        /// then keeps the cost of re-reading down to records that actually changed.
        /// </summary>
        private int ReconciliationDays =>
            configuration.GetValue("Integrations:ReconciliationDays", 7);

        /// <summary>
        /// Records handed to the writer at a time, so a long backfill does not build one
        /// enormous change set.
        /// </summary>
        private int BatchSize =>
            configuration.GetValue("Integrations:BatchSize", 200);

        public async Task<Result<EntryWriteResult>> SyncTargetAsync(string targetId, bool fullResync = false, CancellationToken ct = default)
        {
            var target = await db.IntegrationTargets
                .AsTracking()
                .Include(t => t.Integration)
                .Include(t => t.Mappings)
                .Include(t => t.Tracker)
                    .ThenInclude(t => t.Owner)
                .FirstOrDefaultAsync(t => t.Id == targetId, ct);

            if (target == null)
                return Result.Failure(ResultStatusCodes.NotFound, "Integration target not found.");

            if (target.Mode != IntegrationMode.Pull)
                return Result.Failure(ResultStatusCodes.BadRequest, "This target receives data by webhook and is not pulled.");

            var prepared = Prepare(target.Integration);
            if (prepared.IsFailure)
                return await Fail(target, prepared.Messages.First(), ct);

            var (provider, connection) = prepared.Data;

            // One target only, so the fetch stays a stream: a paginated backfill is consumed
            // page by page rather than materialised whole.
            var window = WindowFor(target, fullResync);
            return await ApplyAsync(target, provider.FetchAsync(connection, target.ResourceType, window, ct), fullResync, ct);
        }

        public async Task<Result<EntryWriteResult>> SyncIntegrationAsync(string integrationId, CancellationToken ct = default)
        {
            var integration = await db.Integrations
                .AsTracking()
                .Include(i => i.Targets)
                    .ThenInclude(t => t.Mappings)
                .Include(i => i.Targets)
                    .ThenInclude(t => t.Tracker)
                        .ThenInclude(t => t.Owner)
                .FirstOrDefaultAsync(i => i.Id == integrationId, ct);

            if (integration == null)
                return Result.Failure(ResultStatusCodes.NotFound, "Integration not found.");

            // Filtered in memory rather than in the query: an integration holds only a handful
            // of targets, and a partial Include here would leave the rest unloaded on a tracked
            // entity, which SaveChanges could then misread.
            var targets = integration.Targets
                .Where(t => t.IsEnabled && t.Mode == IntegrationMode.Pull)
                .ToList();

            if (targets.Count == 0)
                return Result.Success(EntryWriteResult.Empty);

            var totals = EntryWriteResult.Empty;
            var succeeded = 0;
            var failed = 0;

            // One fetch per resource type, not per target: several trackers fed the same kind
            // of data from one connection are the whole reason this method exists. Different
            // resource types still cost a call each -- they are genuinely different requests.
            foreach (var group in targets.GroupBy(t => t.ResourceType))
            {
                ct.ThrowIfCancellationRequested();

                var groupTargets = group.ToList();

                var prepared = Prepare(integration);
                if (prepared.IsFailure)
                {
                    foreach (var t in groupTargets)
                        await Fail(t, prepared.Messages.First(), ct);
                    failed += groupTargets.Count;
                    continue;
                }

                var (provider, connection) = prepared.Data;

                // The union of the group's windows: the earliest start any target needs,
                // through today. A target mid-backfill widens the shared window for this tick
                // only -- the rest just re-read a few days they already have, which the write
                // path absorbs as idempotent upserts. The cursor is left null because each
                // target filters the shared records against its own below.
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var from = groupTargets.Min(t => WindowStartOn(t, today));
                var window = new SyncWindow(from, today, null);

                List<SourceRecord> records = [];
                try
                {
                    await foreach (var record in provider.FetchAsync(connection, group.Key, window, ct))
                        records.Add(record);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Sync fetch failed for integration {IntegrationId}, resource {ResourceType}",
                        integration.Id, group.Key);
                    foreach (var t in groupTargets)
                        await Fail(t, Describe(ex), ct);
                    failed += groupTargets.Count;
                    continue;
                }

                foreach (var target in groupTargets)
                {
                    ct.ThrowIfCancellationRequested();

                    var result = await ApplyAsync(target, ToAsync(records, ct), fullResync: false, ct);
                    if (result.IsFailure)
                    {
                        failed++;
                    }
                    else
                    {
                        succeeded++;
                        totals = Combine(totals, result);
                    }
                }
            }

            // A partial failure still returns what did get written -- every target carries its
            // own status and error for the UI to show. Only a total washout is a failed Result.
            if (succeeded == 0 && failed > 0)
                return Result.Failure(ResultStatusCodes.BadRequest,
                    "No import for this integration could be synced. Check each one for the reason.");

            return Result.Success(totals);
        }

        /// <summary>
        /// The shared tail of both entry points: given a target and the records for it -- a
        /// live provider stream for a single sync, a buffered list for a grouped one -- project
        /// through this target's mappings, drop what the cursor already covers, write in
        /// batches, and record the outcome on the target.
        /// </summary>
        private async Task<Result<EntryWriteResult>> ApplyAsync(
            IntegrationTarget target,
            IAsyncEnumerable<SourceRecord> records,
            bool fullResync,
            CancellationToken ct)
        {
            // A connection may only write to a tracker its own user owns. Enforced when a
            // target is created too; repeated here because ownership can change afterwards.
            if (target.Integration.UserId != target.Tracker.OwnerId)
                return await Fail(target, "The tracker is no longer owned by the user who made this connection.", ct);

            var mappings = target.Mappings
                .Select(m => new FieldMapping(m.SourceKey, m.FieldId, m.SkipWhenNull))
                .ToList();

            var fields = await db.Fields.Where(f => f.TrackerId == target.TrackerId).ToListAsync(ct);
            var timeZone = TimeZoneResolver.FromId(target.Tracker.Owner?.TimeZone);

            var totals = EntryWriteResult.Empty;
            var newCursor = target.LastCursor;
            var batch = new List<EntryWriteRecord>(BatchSize);

            try
            {
                await foreach (var record in records.WithCancellation(ct))
                {
                    // Nothing has changed upstream since we last looked at this record, so
                    // there is no reason to write it again. A full resync re-applies every
                    // record regardless -- that is the point of it.
                    if (!fullResync && record.UpdatedAt != null && target.LastCursor != null && record.UpdatedAt <= target.LastCursor)
                        continue;

                    if (record.UpdatedAt != null && (newCursor == null || record.UpdatedAt > newCursor))
                        newCursor = record.UpdatedAt;

                    batch.Add(SourceRecordProjector.Project(record, mappings));

                    if (batch.Count >= BatchSize)
                    {
                        totals = Combine(totals, await Write(target, fields, batch, timeZone, ct));
                        batch.Clear();
                    }
                }

                if (batch.Count > 0)
                    totals = Combine(totals, await Write(target, fields, batch, timeZone, ct));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Shutting down, not a target failure -- leave the status alone so the next
                // tick picks up where this one stopped.
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sync failed for integration target {TargetId}", target.Id);
                return await Fail(target, Describe(ex), ct);
            }

            target.LastSyncedAt = DateTime.UtcNow;
            target.LastCursor = newCursor;
            target.LastSyncStatus = SyncStatus.Ok;
            target.LastSyncError = null;
            await db.SaveChangesAsync(ct);

            return Result.Success(totals);
        }

        /// <summary>
        /// The provider and a ready-to-use connection for an integration, or a failure saying
        /// why neither can be had: no provider installed for the key, or a credential that no
        /// longer decrypts.
        /// </summary>
        private Result<(IPullIntegrationProvider Provider, ProviderConnection Connection)> Prepare(Integration integration)
        {
            var provider = registry.GetPull(integration.Provider);
            if (provider == null)
                return Result.Failure(ResultStatusCodes.BadRequest,
                    $"No integration provider is installed for '{integration.Provider}'.");

            var credential = credentialProtector.Unprotect(integration.CredentialCiphertext);
            if (credential == null && integration.CredentialCiphertext != null)
                return Result.Failure(ResultStatusCodes.BadRequest,
                    "The stored credential could not be read. Reconnect this integration.");

            var connection = new ProviderConnection(integration.BaseUrl, credential, integration.ExternalAccountId);
            return Result.Success((provider, connection));
        }

        private SyncWindow WindowFor(IntegrationTarget target, bool fullResync)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // A full resync reaches back to the backfill date and drops the cursor, so the
            // provider is asked for -- and the write path re-applies -- the whole history.
            if (fullResync)
                return new SyncWindow(target.BackfillFrom, today, null);

            return new SyncWindow(WindowStartOn(target, today), today, target.LastCursor);
        }

        /// <summary>
        /// Where a target's window begins: the backfill date on a first run, the reconciliation
        /// window on every run after.
        /// </summary>
        private DateOnly WindowStartOn(IntegrationTarget target, DateOnly today) =>
            target.LastSyncedAt == null
                ? target.BackfillFrom
                : today.AddDays(-ReconciliationDays);

        /// <summary>
        /// A buffered group fetch as an async stream, so <see cref="ApplyAsync"/> consumes it
        /// with the same loop it uses for a live provider stream.
        /// </summary>
        private static async IAsyncEnumerable<SourceRecord> ToAsync(
            IReadOnlyList<SourceRecord> records,
            [EnumeratorCancellation] CancellationToken ct)
        {
            foreach (var record in records)
            {
                ct.ThrowIfCancellationRequested();
                yield return record;
            }

            await Task.CompletedTask;
        }

        private async Task<Result<EntryWriteResult>> Write(
            IntegrationTarget target,
            List<Field> fields,
            List<EntryWriteRecord> batch,
            TimeZoneInfo timeZone,
            CancellationToken ct) =>
            Result.Success(await entryWriter.ApplyAsync(
                target.TrackerId, target.Integration.Provider, batch, fields, timeZone, ct));

        private async Task<Result<EntryWriteResult>> Fail(IntegrationTarget target, string message, CancellationToken ct)
        {
            target.LastSyncedAt = DateTime.UtcNow;
            target.LastSyncStatus = SyncStatus.Error;
            target.LastSyncError = message;
            await db.SaveChangesAsync(ct);

            return Result.Failure(ResultStatusCodes.BadRequest, message);
        }

        /// <summary>
        /// What to show the user about a failed sync. Deliberately shallow: an exception
        /// message can carry a URL with a credential in it, so only the type and a short
        /// summary are kept, with the detail left in the log.
        /// </summary>
        private static string Describe(Exception ex) => ex switch
        {
            HttpRequestException http when http.StatusCode != null =>
                $"The provider returned {(int)http.StatusCode}.",
            HttpRequestException => "The provider could not be reached.",
            TaskCanceledException => "The provider took too long to respond.",
            _ => "The sync failed unexpectedly. See the server log for details.",
        };

        private static EntryWriteResult Combine(EntryWriteResult a, Result<EntryWriteResult> b)
        {
            var r = b.Data;
            return new EntryWriteResult(
                a.Created + r.Created,
                a.Updated + r.Updated,
                a.Deleted + r.Deleted,
                a.Skipped + r.Skipped,
                a.ErrorCount + r.ErrorCount,
                [.. a.Errors.Concat(r.Errors).Take(20)]);
        }
    }
}

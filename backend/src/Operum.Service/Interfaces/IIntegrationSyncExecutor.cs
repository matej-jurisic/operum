using Operum.Model.Common;

namespace Operum.Service.Interfaces
{
    /// <summary>
    /// Runs one pull target: fetch, project, write, record what happened. Separate from the
    /// background service so a "sync now" endpoint drives exactly the same code the scheduled
    /// tick does, rather than a second implementation that can drift from it.
    /// </summary>
    public interface IIntegrationSyncExecutor
    {
        /// <summary>
        /// Never throws for provider-side trouble: a revoked key or an unreachable host is
        /// recorded on the target and returned as a failed Result, so one bad target cannot
        /// end the tick for everyone else.
        /// <para>
        /// <paramref name="fullResync"/> ignores the cursor for one run: the window reaches
        /// back to the target's backfill date and every record is re-applied rather than
        /// skipped as unchanged. The mapped fields on entries already imported are overwritten
        /// with the provider's current values -- the way to pick up a mapping added after the
        /// first import, at the cost of discarding any hand edits to those fields.
        /// </para>
        /// </summary>
        Task<Result<EntryWriteResult>> SyncTargetAsync(string targetId, bool fullResync = false, CancellationToken ct = default);

        /// <summary>
        /// Syncs every enabled pull target on one connection, fetching once per resource type
        /// and feeding each linked tracker from that single response rather than calling the
        /// provider once per target. Records the outcome on each target and returns the
        /// combined write totals; a failed Result only when nothing at all could be synced.
        /// </summary>
        Task<Result<EntryWriteResult>> SyncIntegrationAsync(string integrationId, CancellationToken ct = default);
    }
}

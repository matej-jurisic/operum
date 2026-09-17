using Operum.Model.Common;

namespace Operum.Service.Interfaces
{
    // Separate from the background service so "sync now" drives the same code the scheduled tick does.
    public interface IIntegrationSyncExecutor
    {
        // Never throws for provider-side trouble: recorded on the target and returned as a
        // failed Result, so one bad target cannot end the tick for everyone else.
        // fullResync re-applies every record from the backfill date, overwriting mapped fields
        // on entries already imported and discarding any hand edits to them.
        Task<Result<EntryWriteResult>> SyncTargetAsync(string targetId, bool fullResync = false, CancellationToken ct = default);

        // Fetches once per resource type rather than once per target; failed Result only when nothing at all could be synced.
        Task<Result<EntryWriteResult>> SyncIntegrationAsync(string integrationId, CancellationToken ct = default);
    }
}

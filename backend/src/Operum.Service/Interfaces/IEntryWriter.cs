using Operum.Model.Common;
using Operum.Model.Models;

namespace Operum.Service.Interfaces
{
    // For callers with no HTTP context and no signed-in user (sync loop, webhook); EntriesService
    // can't serve that since it reads the current user from IHttpContextAccessor. Performs no
    // authorization of its own: callers must establish write access first.
    public interface IEntryWriter
    {
        // Keyed on (tracker, source, external id): existing key updates, new key creates, delete removes.
        // Not transactional; the key makes a batch idempotent so a partial failure is repaired by rerunning it.
        // timeZone is required (not defaulted): there is no ambient user to fall back to here.
        Task<EntryWriteResult> ApplyAsync(
            string trackerId,
            string source,
            IReadOnlyList<EntryWriteRecord> records,
            List<Field> fields,
            TimeZoneInfo timeZone,
            CancellationToken ct = default);
    }
}

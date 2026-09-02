using Operum.Model.Common;
using Operum.Model.Models;

namespace Operum.Service.Interfaces
{
    /// <summary>
    /// Writes entries on behalf of a caller that has no HTTP context and no signed-in user --
    /// a sync loop, a webhook. <c>EntriesService</c> cannot serve that: it opens by reading
    /// the current user out of <c>IHttpContextAccessor</c>.
    /// <para>
    /// This type performs no authorization of its own. Callers establish the right to write to
    /// the tracker first: the API layer from the request, the integrations layer from the
    /// connection's owning user.
    /// </para>
    /// </summary>
    public interface IEntryWriter
    {
        /// <summary>
        /// Applies a batch of records to one tracker, keyed on (tracker, source, external id):
        /// a record whose key is already present updates that entry, one whose key is new
        /// creates an entry, and a delete removes it.
        /// <para>
        /// Not transactional. The key makes the batch idempotent instead, so a partially
        /// applied batch is repaired by running it again rather than rolled back.
        /// </para>
        /// </summary>
        /// <param name="source">The provider key stamped onto every entry this writes.</param>
        /// <param name="fields">Every field on the tracker, calculated ones included.</param>
        /// <param name="timeZone">
        /// The tracker owner's zone, used when calculated fields resolve constants with
        /// date-based conditions. Not optional here -- there is no ambient user to fall back to.
        /// </param>
        Task<EntryWriteResult> ApplyAsync(
            string trackerId,
            string source,
            IReadOnlyList<EntryWriteRecord> records,
            List<Field> fields,
            TimeZoneInfo timeZone,
            CancellationToken ct = default);
    }
}

namespace Operum.Model.Integrations
{
    public enum SourceOperation
    {
        Upsert,
        Delete
    }

    /// <summary>
    /// One record as a provider hands it over: still in the provider's own vocabulary, but
    /// already coerced to the strings the write path consumes. This is the single type both
    /// ingest paths produce -- a pull loop yields these, a webhook parses into these -- so
    /// everything downstream of it is written once and shared.
    /// </summary>
    /// <param name="ExternalId">
    /// The provider's stable id for this record. Paired with the provider key it is the
    /// idempotency key a re-sync updates on, so it must identify the same thing across syncs:
    /// a wellness record's date, a transaction's journal id -- and for anything that splits,
    /// the id of the split rather than of the group it arrived in.
    /// </param>
    /// <param name="UpdatedAt">
    /// When the provider last revised this record, when it says. Used as the sync cursor;
    /// null simply means the record is always considered fresh.
    /// </param>
    /// <param name="ValuesBySourceKey">
    /// Keyed by <see cref="SourceField.Key"/>. A key the provider genuinely has no value for
    /// should be present with a null value rather than omitted -- that is the distinction a
    /// mapping's SkipWhenNull acts on. Omit a key only to say nothing about that field at all.
    /// </param>
    /// <param name="GroupId">
    /// The parent record this came from, for providers whose records nest -- a Firefly
    /// transaction group fanning out into its splits. Setting it promises that the batch
    /// carries every current child of that parent, which is what lets a child deleted upstream
    /// be removed here. Null for flat records.
    /// </param>
    public sealed record SourceRecord(
        string ExternalId,
        SourceOperation Operation,
        DateTime? UpdatedAt,
        IReadOnlyDictionary<string, string?> ValuesBySourceKey,
        string? GroupId = null)
    {
        public static SourceRecord Deleted(string externalId, DateTime? updatedAt = null) =>
            new(externalId, SourceOperation.Delete, updatedAt, new Dictionary<string, string?>());
    }
}

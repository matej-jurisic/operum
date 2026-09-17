namespace Operum.Model.Integrations
{
    public enum SourceOperation
    {
        Upsert,
        Delete
    }

    /// <summary>The single type both pull and webhook ingest paths produce.</summary>
    /// <param name="ExternalId">Paired with the provider key, forms the idempotency key; use
    /// the split's id, not the group's, for records that split.</param>
    /// <param name="UpdatedAt">Sync cursor; null means always fresh.</param>
    /// <param name="ValuesBySourceKey">A present key with null value means "no value"; an
    /// absent key says nothing about that field (see mapping's SkipWhenNull).</param>
    /// <param name="GroupId">Must include every current child of the parent, so omitted
    /// children get removed. Null for flat records.</param>
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

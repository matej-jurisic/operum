namespace Operum.Model.Common
{
    public enum EntryWriteOperation
    {
        Upsert,
        Delete
    }

    /// <summary>A tracker record, already projected to this tracker's field ids.</summary>
    /// <param name="ExternalId">Paired with the source, forms the idempotency key.</param>
    /// <param name="ValuesByFieldId">A present key is written (null clears the field); an absent key is left alone.</param>
    /// <param name="GroupId">Must include every current child of the parent, so omitted children get removed. Null for flat records or partial pages.</param>
    public sealed record EntryWriteRecord(
        string ExternalId,
        EntryWriteOperation Operation,
        IReadOnlyDictionary<string, string?> ValuesByFieldId,
        string? GroupId = null);

    /// <param name="Skipped">Declined records: over the entry cap, or invalid.</param>
    /// <param name="Errors">Capped list of reasons; <see cref="ErrorCount"/> is the true total.</param>
    public sealed record EntryWriteResult(
        int Created,
        int Updated,
        int Deleted,
        int Skipped,
        int ErrorCount,
        List<string> Errors)
    {
        public static EntryWriteResult Empty => new(0, 0, 0, 0, 0, []);
    }
}

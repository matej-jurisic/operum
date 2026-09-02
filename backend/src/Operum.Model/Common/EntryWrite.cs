namespace Operum.Model.Common
{
    public enum EntryWriteOperation
    {
        Upsert,
        Delete
    }

    /// <summary>
    /// One record on its way into a tracker, already projected from whatever the provider
    /// sent into this tracker's own field ids.
    /// </summary>
    /// <param name="ExternalId">
    /// The provider's stable id for this record. Paired with the source it forms the
    /// idempotency key, so re-sending a record updates the entry it wrote last time.
    /// </param>
    /// <param name="ValuesByFieldId">
    /// Presence is the instruction, not the value: a key that is present is written, and
    /// writing null clears that field. A key that is absent leaves the field alone. That is
    /// what lets a mapping's SkipWhenNull be resolved by the projector -- omit the key to
    /// skip, include it with null to clear -- without the writer knowing the rule.
    /// </param>
    /// <param name="GroupId">
    /// The parent record these came from, when the provider has that shape. Setting it is a
    /// promise that the batch carries <em>every</em> current child of that parent, which is
    /// what lets the writer remove children that have since been deleted upstream. Leave it
    /// null for flat records, and for any batch that is only a page of a parent's children.
    /// </param>
    public sealed record EntryWriteRecord(
        string ExternalId,
        EntryWriteOperation Operation,
        IReadOnlyDictionary<string, string?> ValuesByFieldId,
        string? GroupId = null);

    /// <param name="Skipped">Records the writer declined: over the entry cap, or invalid.</param>
    /// <param name="Errors">
    /// Human-readable reasons, capped so one bad batch cannot produce thousands of strings.
    /// <see cref="ErrorCount"/> is the true total.
    /// </param>
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

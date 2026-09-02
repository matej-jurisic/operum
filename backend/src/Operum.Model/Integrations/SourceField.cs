namespace Operum.Model.Integrations
{
    /// <summary>
    /// Which ingest paths a provider supports. A provider may do both: pull for backfill,
    /// push for everything after.
    /// </summary>
    [Flags]
    public enum IntegrationCapabilities
    {
        None = 0,
        Pull = 1,
        Push = 2
    }

    /// <summary>
    /// One value a provider can offer for mapping onto a tracker field.
    /// <para>
    /// <paramref name="Key"/> is an opaque, stable id -- it is what a saved mapping stores, so
    /// it must not change once a provider ships. How the provider actually reads that value
    /// out of its payload is its own business: a flat JSON key for intervals.icu wellness, a
    /// path into a nested split for a Firefly transaction. Nothing outside the provider needs
    /// to know which.
    /// </para>
    /// </summary>
    /// <param name="Type">A <c>DataTypes</c> value: what this reads as once coerced.</param>
    public sealed record SourceField(
        string Key,
        string Type,
        string Label,
        string? Description = null);
}

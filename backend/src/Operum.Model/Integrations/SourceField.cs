namespace Operum.Model.Integrations
{
    [Flags]
    public enum IntegrationCapabilities
    {
        None = 0,
        Pull = 1,
        Push = 2
    }

    /// <param name="Key">Opaque, stable id a saved mapping stores; must not change once a provider ships.</param>
    /// <param name="Type">A <c>DataTypes</c> value.</param>
    public sealed record SourceField(
        string Key,
        string Type,
        string Label,
        string? Description = null);
}

namespace Operum.Model.Integrations
{
    /// <param name="SkipWhenNull">True leaves the field's existing value alone; false clears it.</param>
    public sealed record FieldMapping(
        string SourceKey,
        string FieldId,
        bool SkipWhenNull = true);
}

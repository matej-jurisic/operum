namespace Operum.Model.Integrations
{
    /// <summary>
    /// One source value wired to one tracker field. The stored
    /// <c>IntegrationFieldMapping</c> projects onto this so the projector and the validator
    /// stay pure -- neither needs the entity, and both are testable without a database.
    /// </summary>
    /// <param name="SkipWhenNull">
    /// What to do when the provider reports no value. True leaves whatever the field already
    /// holds alone, which is almost always right: an unlogged metric is missing data, and
    /// writing it as empty would drag averages and charts around. False clears the field,
    /// for the rarer case where the provider going quiet genuinely means "no longer set".
    /// </param>
    public sealed record FieldMapping(
        string SourceKey,
        string FieldId,
        bool SkipWhenNull = true);
}

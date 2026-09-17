namespace Operum.Model.Models
{
    // No longer an EF entity: replaced by Widget/WidgetSource/WidgetSourceField. Survives only
    // as the transient carrier AnalyticResultBuilderRequest.Analytic expects, always
    // constructed with `new Analytic { ... }` and never persisted.
    public class Analytic
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        // Line/Bar only.
        public string? Grouping { get; set; }

        public string ResultType { get; set; } = string.Empty;

        // Goal widgets only, in the value field's own string format.
        public string? GoalTarget { get; set; }

        // Goal widgets only. Null behaves as HigherIsBetter.
        public string? GoalDirection { get; set; }
    }
}

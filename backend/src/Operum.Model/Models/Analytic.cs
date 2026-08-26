namespace Operum.Model.Models
{
    // No longer an EF entity -- the Widget Library refactor replaced the tracker-owned
    // Analytic this used to be with Widget/WidgetSource/WidgetSourceField. This class
    // survives only as the transient carrier AnalyticResultBuilderRequest.Analytic expects:
    // the calculation pipeline (AnalyticResultBuilder + builders/calculators/processors)
    // reads nothing but Id/Name/Description/Code/ResultType off it, and is fed a `new
    // Analytic { ... }` that is never persisted by every caller today (DashboardService,
    // ConditionAnalyticEvaluator).
    public class Analytic
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ResultType { get; set; } = string.Empty;
    }
}

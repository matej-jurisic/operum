namespace Operum.Model.DTOs.Widgets
{
    public class WidgetSourceFieldDto
    {
        public string Purpose { get; set; } = string.Empty;
        public string FieldId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
    }

    public class WidgetSourceDto
    {
        public string Id { get; set; } = string.Empty;
        // e.g. "Monthly Totals: Day, Amount".
        public string Name { get; set; } = string.Empty;
        public List<WidgetSourceFieldDto> Fields { get; set; } = [];
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    // Not scoped to any one dashboard; see DashboardWidgetDto for a placement's rendering.
    public class WidgetDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        // Line/Bar only.
        public string? Grouping { get; set; }
        public bool MatchedValuesOnly { get; set; }
        // Goal widgets only, in the value field's own string format.
        public string? GoalTarget { get; set; }
        // Goal widgets only. Null reads as HigherIsBetter.
        public string? GoalDirection { get; set; }
        public List<WidgetSourceDto> Sources { get; set; } = [];
    }
}

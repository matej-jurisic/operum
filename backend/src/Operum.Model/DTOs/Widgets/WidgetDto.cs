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
        // The widget's definition read through this source's fields, e.g. "Monthly Totals:
        // Day, Amount" -- the same display name a dashboard source computes for itself.
        public string Name { get; set; } = string.Empty;
        public List<WidgetSourceFieldDto> Fields { get; set; } = [];
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    // A reusable chart definition as the Widget Library reads and edits it. Not scoped to
    // any one dashboard -- see DashboardWidgetDto for how a placement of this renders on a
    // board.
    public class WidgetDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool MatchedValuesOnly { get; set; }
        public List<WidgetSourceDto> Sources { get; set; } = [];
    }
}

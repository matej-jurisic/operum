namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardItemSourceFieldDto
    {
        public string Purpose { get; set; } = string.Empty;
        public string FieldId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
    }

    public class DashboardItemSourceDto
    {
        public string Id { get; set; } = string.Empty;
        // The item's definition read through this source's fields, e.g. "Monthly Totals:
        // Day, Amount".
        public string Name { get; set; } = string.Empty;
        public List<DashboardItemSourceFieldDto> Fields { get; set; } = [];
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        // How the source is filtered: a fixed view of its own tracker, or the View widget
        // on the board whose selection it follows. Never both.
        public string? ViewId { get; set; }
        public string? LinkedViewWidgetId { get; set; }
        public string? Label { get; set; }
        public int Order { get; set; }
    }
}

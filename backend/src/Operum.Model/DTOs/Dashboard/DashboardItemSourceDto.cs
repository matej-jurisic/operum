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
        // The fixed tracker view this source reads through, if any. A view selector widget
        // on the board can layer further clauses on top of it.
        public string? ViewId { get; set; }
        public string? Label { get; set; }
        public int Order { get; set; }
    }
}

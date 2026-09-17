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
        // e.g. "Monthly Totals: Day, Amount".
        public string Name { get; set; } = string.Empty;
        public List<DashboardItemSourceFieldDto> Fields { get; set; } = [];
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        // Fixed view this source reads through, if any; a filter widget can layer clauses on top.
        public string? ViewId { get; set; }
        public string? Label { get; set; }
        public int Order { get; set; }
    }
}

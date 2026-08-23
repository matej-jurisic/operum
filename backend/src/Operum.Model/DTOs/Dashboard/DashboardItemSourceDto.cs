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
        // Null when the source carries its own ad hoc definition rather than pointing at
        // an analytic saved on the tracker.
        public string? AnalyticId { get; set; }
        public string AnalyticName { get; set; } = string.Empty;
        public string ResultType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsAdHoc { get; set; }
        public List<DashboardItemSourceFieldDto> Fields { get; set; } = [];
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        public List<string> ViewIds { get; set; } = [];
        public string? Label { get; set; }
        public int Order { get; set; }
    }
}

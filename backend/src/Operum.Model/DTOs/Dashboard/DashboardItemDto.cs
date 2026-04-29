namespace Operum.Model.DTOs.Dashboard
{
    public class DashboardItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string AnalyticId { get; set; } = string.Empty;
        public string AnalyticName { get; set; } = string.Empty;
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        public List<string> ViewIds { get; set; } = [];
        public int Order { get; set; }
    }
}

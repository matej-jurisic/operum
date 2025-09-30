namespace Operum.Model.DTOs.Trackers
{
    public class TrackerAnalyticDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<TrackerAnalyticFieldDto> TrackerAnalyticFields { get; set; } = [];
    }
}

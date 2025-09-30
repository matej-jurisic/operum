namespace Operum.Model.DTOs.Analytics
{
    public class TrackerAnalyticsResultDto
    {
        public string TrackerId { get; set; } = string.Empty;
        public List<AnalyticResultDto> Analytics { get; set; } = [];
    }

    public class AnalyticResultDto
    {
        public string AnalyticId { get; set; } = string.Empty;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Code { get; set; } = null!;
        public AnalyticResultType Type { get; set; }
    }

    public enum AnalyticResultType
    {
        SingleValue,
        Chart2D
    }

    public class SingleValueAnalyticResult(string result) : AnalyticResultDto
    {
        public string Value { get; set; } = result;
    }

    public class Chart2DAnalyticResultDto : AnalyticResultDto
    {
        public List<ChartPointDto> Points { get; set; } = [];
    }

    public class ChartPointDto
    {
        public string X { get; set; } = string.Empty;
        public double Y { get; set; }
    }
}

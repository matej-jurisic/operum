using Operum.Model.Constants;

namespace Operum.Model.DTOs.Analytics
{
    public class TrackerAnalyticsResultDto
    {
        public string TrackerId { get; set; } = string.Empty;
        public List<AnalyticResultDto> Analytics { get; set; } = [];
    }

    public abstract class AnalyticResultDto
    {
        public string AnalyticId { get; set; } = string.Empty;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string ResultType { get; set; } = string.Empty;
    }

    public class SingleValueAnalyticResult : AnalyticResultDto
    {
        public string Value { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? EntryId { get; set; }

        public SingleValueAnalyticResult()
        {
            ResultType = AnalyticResultTypes.SingleValue;
        }
    }

    public class NumericChartAnalyticResult : AnalyticResultDto
    {
        public List<ChartPointDto> Points { get; set; } = [];
        public string XFieldName { get; set; } = string.Empty;
        public string YFieldName { get; set; } = string.Empty;

        public NumericChartAnalyticResult()
        {
            ResultType = AnalyticResultTypes.NumericChart;
        }
    }

    public class ChartPointDto
    {
        public string? X { get; set; }
        public double? Y { get; set; }
    }
}

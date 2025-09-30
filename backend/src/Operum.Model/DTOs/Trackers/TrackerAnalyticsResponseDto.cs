using Operum.Model.DTOs.Analytics;

namespace Operum.Model.DTOs.Trackers
{
    public class TrackerAnalyticsResponseDto
    {
        public List<SingleValueAnalyticResult> SingleValueAnalytics { get; set; } = [];
        public List<NumericChartAnalyticResult> NumericChartAnalytics { get; set; } = [];
    }
}

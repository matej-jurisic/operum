using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics
{
    // Merges multiple DashboardItemSources into one chart; never a persisted Analytic.ResultType.
    public class ComposedChartAnalyticDto : AnalyticDto
    {
        public List<ComposedChartSeriesDto> Series { get; set; } = [];
        public List<string> Warnings { get; set; } = [];

        // Only meaningful when at least one line series is drawn.
        public bool YAxisFromZero { get; set; } = true;

        public ComposedChartAnalyticDto()
        {
            ResultType = AnalyticTypes.Composed;
        }
    }
}

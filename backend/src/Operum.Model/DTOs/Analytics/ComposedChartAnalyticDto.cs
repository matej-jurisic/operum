using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics
{
    // Result for a dashboard widget that combines multiple DashboardItemSources (each its
    // own tracker/analytic) into one chart. Built directly by DashboardService by merging
    // per-source LineChartAnalyticDto/BarChartAnalyticDto results — never produced by the
    // AnalyticResultBuilder pipeline and never a persisted Analytic.ResultType.
    public class ComposedChartAnalyticDto : AnalyticDto
    {
        public List<ComposedChartSeriesDto> Series { get; set; } = [];
        public List<string> Warnings { get; set; } = [];

        // Whether the y-axis starts at zero (default) or is fitted to the data's own range,
        // carried over from the placement the same way LineChartAnalyticDto.YAxisFromZero is.
        // Only meaningful when the combined chart draws at least one line series.
        public bool YAxisFromZero { get; set; } = true;

        public ComposedChartAnalyticDto()
        {
            ResultType = AnalyticTypes.Composed;
        }
    }
}

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

        public ComposedChartAnalyticDto()
        {
            ResultType = AnalyticTypes.Composed;
        }
    }
}

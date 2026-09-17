using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    public class ScatterPlotAnalyticDto : AnalyticDto
    {
        public List<ScatterChartPointDto> Points { get; set; } = [];
        public FieldDto XField { get; set; } = null!;
        public FieldDto YField { get; set; } = null!;

        // Set only by Correlation, when the join leaves little/nothing to plot.
        public List<string> Warnings { get; set; } = [];

        public ScatterPlotAnalyticDto()
        {
            ResultType = AnalyticTypes.ScatterChart;
        }
    }
}

using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    public class ScatterPlotAnalyticDto : AnalyticDto
    {
        public List<ScatterChartPointDto> Points { get; set; } = [];
        public FieldDto XField { get; set; } = null!;
        public FieldDto YField { get; set; } = null!;

        // Set only by the two-tracker Correlation calculation, when the join leaves nothing
        // (or little) to plot: the axes disagree on a value type, or the trackers share no
        // match key. Empty for an ordinary single-tracker scatter plot.
        public List<string> Warnings { get; set; } = [];

        public ScatterPlotAnalyticDto()
        {
            ResultType = AnalyticTypes.ScatterChart;
        }
    }
}

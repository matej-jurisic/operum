using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    public class LineChartAnalyticDto : AnalyticDto
    {
        public List<LineChartPointDto> Points { get; set; } = [];
        public FieldDto XField { get; set; } = null!;
        public FieldDto YField { get; set; } = null!;

        // Whether the y-axis starts at zero (default) or is fitted to the data's own range.
        // Set from the placement (DashboardItem.YAxisFromZero) when the chart is drawn on a
        // board; left at the default everywhere else.
        public bool YAxisFromZero { get; set; } = true;

        public LineChartAnalyticDto()
        {
            ResultType = AnalyticTypes.LineChart;
        }
    }
}

using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    public class LineChartAnalyticDto : AnalyticDto
    {
        public List<LineChartPointDto> Points { get; set; } = [];
        public FieldDto XField { get; set; } = null!;
        public FieldDto YField { get; set; } = null!;

        // Set from DashboardItem.YAxisFromZero when drawn on a board; default otherwise.
        public bool YAxisFromZero { get; set; } = true;

        public LineChartAnalyticDto()
        {
            ResultType = AnalyticTypes.LineChart;
        }
    }
}

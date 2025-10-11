using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    public class LineChartAnalyticDto : AnalyticDto
    {
        public List<LineChartPointDto> Points { get; set; } = [];
        public FieldDto XField { get; set; } = null!;
        public FieldDto YField { get; set; } = null!;

        public LineChartAnalyticDto()
        {
            ResultType = AnalyticTypes.LineChart;
        }
    }
}

using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics
{
    public class LineChartAnalyticDto : AnalyticDto
    {
        public List<LineChartPointDto> Points { get; set; } = [];
        public string XFieldName { get; set; } = string.Empty;
        public string XFieldType { get; set; } = string.Empty;
        public string YFieldName { get; set; } = string.Empty;
        public string YFieldType { get; set; } = string.Empty;

        public LineChartAnalyticDto()
        {
            ResultType = AnalyticTypes.NumericChart;
        }
    }
}

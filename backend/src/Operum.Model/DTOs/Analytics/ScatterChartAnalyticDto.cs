using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics
{
    public class ScatterPlotAnalyticDto : AnalyticDto
    {
        public List<ScatterChartPointDto> Points { get; set; } = [];
        public string XFieldName { get; set; } = string.Empty;
        public string YFieldName { get; set; } = string.Empty;

        public string XFieldType { get; set; } = string.Empty;
        public string YFieldType { get; set; } = string.Empty;

        public ScatterPlotAnalyticDto()
        {
            ResultType = AnalyticTypes.ScatterPlot;
        }
    }
}

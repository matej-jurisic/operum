using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    public class DonutChartAnalyticDto : AnalyticDto
    {
        public List<DonutChartPointDto> Points { get; set; } = [];
        public FieldDto NameField { get; set; } = null!;
        public FieldDto ValueField { get; set; } = null!;

        public DonutChartAnalyticDto()
        {
            ResultType = AnalyticTypes.Donut;
        }
    }
}

using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    public class LineChartProcessor : ILineChartProcessor
    {
        public List<LineChartPointDto> Process(List<LineChartPointDto> dataPoints)
        {
            return dataPoints;
        }
    }
}

using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    public class DonutChartProcessor : IDonutChartProcessor
    {
        public List<DonutChartPointDto> Process(List<DonutChartPointDto> dataPoints)
        {
            return dataPoints;
        }
    }
}

using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    // Raw values (no grouping): pass-through, since the builder already mapped/filtered points.
    public class BarChartProcessor : IBarChartProcessor
    {
        public List<DonutChartPointDto> Process(List<DonutChartPointDto> dataPoints)
        {
            return dataPoints;
        }
    }
}

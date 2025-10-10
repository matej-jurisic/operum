using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    public class AggregatedSumLineChartProcessor : ILineChartProcessor
    {
        public List<LineChartPointDto> Process(List<LineChartPointDto> dataPoints)
        {
            return [.. dataPoints
                .GroupBy(e => e.X)
                .Select(g => new LineChartPointDto
                {
                    X = g.Key,
                    Y = Math.Round(g.Sum(e => e.Y ?? 0), 2)
                })];
        }
    }
}

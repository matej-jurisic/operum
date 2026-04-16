using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    public class DonutChartProcessor : IDonutChartProcessor
    {
        public List<DonutChartPointDto> Process(List<DonutChartPointDto> dataPoints)
        {
            return [.. dataPoints
                .GroupBy(x => x.Name)
                .Select(g => new DonutChartPointDto
                {
                    Name = g.Key,
                    Value = Math.Round(g.Sum(e => e.Value ?? 0), 2)
                })
                ];
        }
    }
}

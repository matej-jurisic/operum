using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    public class CountBarChartProcessor : IBarChartProcessor
    {
        public List<DonutChartPointDto> Process(List<DonutChartPointDto> dataPoints)
        {
            return [.. dataPoints
                .GroupBy(x => x.Name)
                .Select(g => new DonutChartPointDto
                {
                    Name = g.Key,
                    Value = g.Count()
                })];
        }
    }

    public class SumBarChartProcessor : IBarChartProcessor
    {
        public List<DonutChartPointDto> Process(List<DonutChartPointDto> dataPoints)
        {
            return [.. dataPoints
                .GroupBy(x => x.Name)
                .Select(g => new DonutChartPointDto
                {
                    Name = g.Key,
                    Value = Math.Round(g.Sum(e => e.Value ?? 0), 2)
                })];
        }
    }

    public class AverageBarChartProcessor : IBarChartProcessor
    {
        public List<DonutChartPointDto> Process(List<DonutChartPointDto> dataPoints)
        {
            return [.. dataPoints
                .GroupBy(x => x.Name)
                .Select(g => new DonutChartPointDto
                {
                    Name = g.Key,
                    Value = Math.Round(g.Average(e => e.Value ?? 0), 2)
                })];
        }
    }
}

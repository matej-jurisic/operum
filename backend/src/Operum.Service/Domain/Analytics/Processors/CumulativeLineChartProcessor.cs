using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    public class CumulativeLineChartProcessor : ILineChartProcessor
    {
        public List<LineChartPointDto> Process(List<LineChartPointDto> dataPoints)
        {
            var running = 0.0;
            return [.. dataPoints.Select(p =>
            {
                running += p.Y ?? 0;
                return new LineChartPointDto { X = p.X, Y = Math.Round(running, 2) };
            })];
        }
    }
}

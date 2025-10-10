using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    public class CumulativeLineChartProcessor : ILineChartProcessor
    {
        public List<LineChartPointDto> Process(List<LineChartPointDto> dataPoints)
        {
            var grouped = dataPoints
                .GroupBy(e => e.X)
                .Select((g, index) => new { Group = g, OriginalIndex = dataPoints.FindIndex(p => p.X == g.Key) })
                .OrderBy(x => x.OriginalIndex)
                .Select(x => x.Group)
                .ToList();

            var points = new List<LineChartPointDto>();
            var runningTotal = 0.0;

            foreach (var g in grouped)
            {
                runningTotal += g.Sum(e => e.Y ?? 0);
                points.Add(new LineChartPointDto
                {
                    X = g.Key,
                    Y = Math.Round(runningTotal, 2)
                });
            }

            return points;
        }
    }
}

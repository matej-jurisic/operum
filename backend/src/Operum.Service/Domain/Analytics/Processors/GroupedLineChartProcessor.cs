using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    // Buckets a line chart's points by the chosen grouping and reduces each bucket to one point.
    public class GroupedLineChartProcessor(string grouping, string aggregation) : ILineChartProcessor
    {
        public List<LineChartPointDto> Process(List<LineChartPointDto> dataPoints)
        {
            var isDateBucket = AnalyticGroupings.DateBuckets.Contains(grouping);

            var keyed = dataPoints
                .Select((p, i) =>
                {
                    if (isDateBucket)
                    {
                        var dk = ChartBuckets.DateKey(p.X, grouping);
                        return dk == null ? null : new Keyed(dk.Value.Key, dk.Value.Instant.Ticks, p);
                    }

                    return p.X == null ? null : new Keyed(p.X, i, p);
                })
                .OfType<Keyed>()
                .ToList();

            // Points arrive already ordered along the x-axis; ordering groups by first member preserves that.
            var groups = keyed
                .GroupBy(k => k.Key)
                .OrderBy(g => g.Min(k => k.Sort))
                .ToList();

            if (aggregation == AnalyticCodes.CumulativeSum)
            {
                var running = 0.0;
                var points = new List<LineChartPointDto>();
                foreach (var g in groups)
                {
                    running += g.Sum(k => k.Point.Y ?? 0);
                    points.Add(new LineChartPointDto { X = g.Key, Y = Math.Round(running, 2) });
                }
                return points;
            }

            return [.. groups.Select(g => new LineChartPointDto
            {
                X = g.Key,
                Y = ChartBuckets.Reduce(aggregation, [.. g.Select(k => k.Point.Y ?? 0)])
            })];
        }

        private sealed record Keyed(string Key, long Sort, LineChartPointDto Point);
    }
}

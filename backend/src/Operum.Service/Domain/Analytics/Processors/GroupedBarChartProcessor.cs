using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    // Buckets a bar chart's points by the chosen grouping and reduces each bucket to one bar.
    public class GroupedBarChartProcessor(string grouping, string aggregation) : IBarChartProcessor
    {
        public List<DonutChartPointDto> Process(List<DonutChartPointDto> dataPoints)
        {
            var isDateBucket = AnalyticGroupings.DateBuckets.Contains(grouping);

            var keyed = dataPoints
                .Select((p, i) =>
                {
                    if (isDateBucket)
                    {
                        var dk = ChartBuckets.DateKey(p.Name, grouping);
                        return dk == null ? null : new Keyed(dk.Value.Key, dk.Value.Instant.Ticks, p);
                    }

                    return p.Name == null ? null : new Keyed(p.Name, i, p);
                })
                .OfType<Keyed>()
                .ToList();

            return [.. keyed
                .GroupBy(k => k.Key)
                .OrderBy(g => g.Min(k => k.Sort))
                .Select(g => new DonutChartPointDto
                {
                    Name = g.Key,
                    Value = ChartBuckets.Reduce(aggregation, [.. g.Select(k => k.Point.Value ?? 0)])
                })];
        }

        private sealed record Keyed(string Key, long Sort, DonutChartPointDto Point);
    }
}

using System.Globalization;
using Operum.Model.Constants.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    // Shared bucketing and reduction for the grouped Line and Bar processors.
    public static class ChartBuckets
    {
        // Null for a non-date grouping or a value that doesn't parse as a round-trip date.
        public static (string Key, DateTime Instant)? DateKey(string? raw, string grouping)
        {
            if (raw == null || !AnalyticGroupings.DateBuckets.Contains(grouping))
                return null;

            if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return null;

            var key = grouping switch
            {
                AnalyticGroupings.Daily => dt.ToString("yyyy-MM-dd"),
                // Monday as first day of week.
                AnalyticGroupings.Weekly => dt.AddDays(-(((int)dt.DayOfWeek + 6) % 7)).ToString("yyyy-MM-dd"),
                AnalyticGroupings.Monthly => dt.ToString("yyyy-MM"),
                AnalyticGroupings.Yearly => dt.ToString("yyyy"),
                _ => dt.ToString("yyyy-MM-dd")
            };

            return (key, dt);
        }

        // Cumulative Sum is a running fold across buckets; callers handle it themselves and never reach here.
        public static double Reduce(string aggregation, IReadOnlyCollection<double> values) => aggregation switch
        {
            AnalyticCodes.Count => values.Count,
            AnalyticCodes.Average => values.Count == 0 ? 0 : Math.Round(values.Average(), 2),
            AnalyticCodes.Min => values.Count == 0 ? 0 : Math.Round(values.Min(), 2),
            AnalyticCodes.Max => values.Count == 0 ? 0 : Math.Round(values.Max(), 2),
            _ => Math.Round(values.Sum(), 2)
        };
    }
}

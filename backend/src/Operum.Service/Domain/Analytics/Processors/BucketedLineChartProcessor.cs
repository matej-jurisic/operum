using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Processors
{
    public abstract class BucketedLineChartProcessor : ILineChartProcessor
    {
        protected abstract string TruncateDate(DateTime dt);

        public List<LineChartPointDto> Process(List<LineChartPointDto> dataPoints)
        {
            var parsed = dataPoints
                .Where(p => p.X != null && DateTime.TryParse(p.X, null, System.Globalization.DateTimeStyles.RoundtripKind, out _))
                .Select(p =>
                {
                    DateTime.TryParse(p.X, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt);
                    return new { Point = p, Dt = dt, Key = TruncateDate(dt) };
                })
                .ToList();

            return [.. parsed
                .GroupBy(x => x.Key)
                .OrderBy(g => g.Min(x => x.Dt))
                .Select(g => new LineChartPointDto
                {
                    X = g.Key,
                    Y = Math.Round(g.Sum(x => x.Point.Y ?? 0), 2)
                })];
        }
    }

    public class DailyLineChartProcessor : BucketedLineChartProcessor
    {
        protected override string TruncateDate(DateTime dt) => dt.ToString("yyyy-MM-dd");
    }

    public class WeeklyLineChartProcessor : BucketedLineChartProcessor
    {
        protected override string TruncateDate(DateTime dt)
        {
            // Monday as first day of week
            var offset = ((int)dt.DayOfWeek + 6) % 7;
            return dt.AddDays(-offset).ToString("yyyy-MM-dd");
        }
    }

    public class MonthlyLineChartProcessor : BucketedLineChartProcessor
    {
        protected override string TruncateDate(DateTime dt) => dt.ToString("yyyy-MM");
    }

    public class YearlyLineChartProcessor : BucketedLineChartProcessor
    {
        protected override string TruncateDate(DateTime dt) => dt.ToString("yyyy");
    }
}

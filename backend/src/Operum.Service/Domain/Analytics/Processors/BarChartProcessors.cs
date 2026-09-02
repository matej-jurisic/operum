using System.Globalization;
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

    // Bars whose categories are date periods: the Name field is a date, truncated to the
    // start of its day/week/month/year, and each bar is the summed value over that period.
    // Bars are ordered chronologically rather than by category text. Mirrors
    // BucketedLineChartProcessor.
    public abstract class BucketedBarChartProcessor : IBarChartProcessor
    {
        protected abstract string TruncateDate(DateTime dt);

        public List<DonutChartPointDto> Process(List<DonutChartPointDto> dataPoints)
        {
            var parsed = dataPoints
                .Where(p => p.Name != null && DateTime.TryParse(p.Name, null, DateTimeStyles.RoundtripKind, out _))
                .Select(p =>
                {
                    DateTime.TryParse(p.Name, null, DateTimeStyles.RoundtripKind, out var dt);
                    return new { Point = p, Dt = dt, Key = TruncateDate(dt) };
                })
                .ToList();

            return [.. parsed
                .GroupBy(x => x.Key)
                .OrderBy(g => g.Min(x => x.Dt))
                .Select(g => new DonutChartPointDto
                {
                    Name = g.Key,
                    Value = Math.Round(g.Sum(x => x.Point.Value ?? 0), 2)
                })];
        }
    }

    public class DailyBarChartProcessor : BucketedBarChartProcessor
    {
        protected override string TruncateDate(DateTime dt) => dt.ToString("yyyy-MM-dd");
    }

    public class WeeklyBarChartProcessor : BucketedBarChartProcessor
    {
        protected override string TruncateDate(DateTime dt)
        {
            // Monday as first day of week
            var offset = ((int)dt.DayOfWeek + 6) % 7;
            return dt.AddDays(-offset).ToString("yyyy-MM-dd");
        }
    }

    public class MonthlyBarChartProcessor : BucketedBarChartProcessor
    {
        protected override string TruncateDate(DateTime dt) => dt.ToString("yyyy-MM");
    }

    public class YearlyBarChartProcessor : BucketedBarChartProcessor
    {
        protected override string TruncateDate(DateTime dt) => dt.ToString("yyyy");
    }
}

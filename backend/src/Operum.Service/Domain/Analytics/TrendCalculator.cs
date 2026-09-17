using System.Globalization;
using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Models;
using Operum.Service.Domain.Views;

namespace Operum.Service.Domain.Analytics
{
    // Computed only when the placement follows a date-bounded filter clause (a Filter widget
    // linking both a >= and a <= clause on the same Date/DateTime field). No connected range,
    // no trend. Reuses SingleValueCalculators so a trend always agrees with the number it's trending.
    public static class TrendCalculator
    {
        public static bool IsDateField(string fieldType) =>
            fieldType == DataTypes.Date || fieldType == DataTypes.DateTime;

        // Resolves the first Date/DateTime field carrying both a >= and a <= clause, from
        // either a literal ISO instant or a dynamic token ("start_of_month"), to concrete UTC instants.
        public static (string FieldId, string FieldType, DateTime Start, DateTime End)? TryResolveDateRange(
            IReadOnlyList<ResolvedClause> followFilters, TimeZoneInfo tz)
        {
            var dateClauses = followFilters.Where(c => IsDateField(c.FieldType)).ToList();

            foreach (var fieldId in dateClauses.Select(c => c.FieldId).Distinct())
            {
                var lower = dateClauses.FirstOrDefault(c => c.FieldId == fieldId && c.Operator == OperatorTypes.GreaterThanOrEqual);
                var upper = dateClauses.FirstOrDefault(c => c.FieldId == fieldId && c.Operator == OperatorTypes.LessThanOrEqual);
                if (lower.FieldId == null || upper.FieldId == null)
                    continue;

                var start = DynamicDateTokens.ResolveValue(lower.Value, tz);
                var end = DynamicDateTokens.ResolveValue(upper.Value, tz);
                if (start == null || end == null || end <= start)
                    continue;

                return (fieldId, lower.FieldType, start.Value, end.Value);
            }

            return null;
        }

        // Replaces the given field's date bound pair with the immediately preceding, equal-length period.
        public static List<ResolvedClause> WithPreviousRange(
            IReadOnlyList<ResolvedClause> followFilters, string fieldId, string fieldType, DateTime currentStart, DateTime currentEnd)
        {
            var span = currentEnd - currentStart;
            var previousStart = currentStart - span;
            // Strictly less than currentStart: LessThanOrEqual would double-count a boundary entry.
            var previousEnd = currentStart;

            var shifted = followFilters
                .Where(c => c.FieldId != fieldId || (c.Operator != OperatorTypes.GreaterThanOrEqual && c.Operator != OperatorTypes.LessThanOrEqual))
                .ToList();

            shifted.Add(new ResolvedClause(fieldId, fieldType, OperatorTypes.GreaterThanOrEqual, Iso(previousStart), false));
            shifted.Add(new ResolvedClause(fieldId, fieldType, OperatorTypes.LessThan, Iso(previousEnd), false));
            return shifted;
        }

        // A bucket with no entries is omitted rather than plotted as zero: some calculators
        // (Average, Min/Max, StdDev) have nothing meaningful to say about an empty set.
        public static List<TrendPointDto> BuildSparkline(
            List<Entry> entries, string dateFieldId, DateTime start, DateTime end, string code, Field valueField)
        {
            var calculator = SingleValueCalculators.Get(code);
            if (calculator == null)
                return [];

            var grouping = PickGrouping(end - start);

            var byBucket = entries
                .Select(e => new
                {
                    Entry = e,
                    Date = e.FieldValues.FirstOrDefault(fv => fv.FieldId == dateFieldId)?.DateTimeValue
                })
                // Inclusive of `end`, matching the current window's DB query (both bounds inclusive).
                .Where(x => x.Date.HasValue && x.Date.Value >= start && x.Date.Value <= end)
                .GroupBy(x => BucketKey(x.Date!.Value, grouping))
                .OrderBy(g => g.Key.Instant);

            var points = new List<TrendPointDto>();
            foreach (var bucket in byBucket)
            {
                var values = bucket
                    .SelectMany(x => x.Entry.FieldValues.Where(fv => fv.FieldId == valueField.Id))
                    .ToList();
                if (values.Count == 0)
                    continue;

                var result = calculator.Calculate(values);
                if (!result.IsSuccess)
                    continue;

                var magnitude = ParseMagnitude(valueField.Type, result.Data.Value);
                if (magnitude == null)
                    continue;

                points.Add(new TrendPointDto { X = bucket.Key.Key, Y = magnitude.Value });
            }

            return points;
        }

        public static string? CalculatePreviousValue(List<Entry> entries, string code, Field valueField)
        {
            var calculator = SingleValueCalculators.Get(code);
            if (calculator == null)
                return null;

            var values = entries.SelectMany(e => e.FieldValues.Where(fv => fv.FieldId == valueField.Id)).ToList();
            if (values.Count == 0)
                return null;

            var result = calculator.Calculate(values);
            return result.IsSuccess ? result.Data.Value : null;
        }

        private static string Iso(DateTime d) => d.ToString("o", CultureInfo.InvariantCulture);

        private static string PickGrouping(TimeSpan span) => span.TotalDays switch
        {
            <= 45 => AnalyticGroupings.Daily,
            <= 730 => AnalyticGroupings.Weekly,
            _ => AnalyticGroupings.Monthly
        };

        private static (string Key, DateTime Instant) BucketKey(DateTime dt, string grouping) => grouping switch
        {
            AnalyticGroupings.Weekly => Weekly(dt),
            AnalyticGroupings.Monthly => (dt.ToString("yyyy-MM"), new DateTime(dt.Year, dt.Month, 1)),
            _ => (dt.ToString("yyyy-MM-dd"), dt.Date)
        };

        // Monday as first day of week, matching ChartBuckets' own Line/Bar bucketing.
        private static (string Key, DateTime Instant) Weekly(DateTime dt)
        {
            var weekStart = dt.Date.AddDays(-(((int)dt.DayOfWeek + 6) % 7));
            return (weekStart.ToString("yyyy-MM-dd"), weekStart);
        }

        // Mirrors GoalAnalyticBuilder's own magnitude parsing, kept local deliberately.
        private static double? ParseMagnitude(string? type, string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (type == DataTypes.TimeSpan)
                return TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out var ts) ? ts.TotalSeconds : null;

            return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var magnitude) ? magnitude : null;
        }
    }
}

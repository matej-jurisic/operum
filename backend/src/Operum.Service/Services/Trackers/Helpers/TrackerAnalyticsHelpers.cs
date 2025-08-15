using Operum.Model.DTOs.Analytics;
using Operum.Model.Models;

namespace Operum.Service.Services.Trackers.Helpers
{
    public static class TrackerAnalyticsHelpers
    {
        public static FieldAnalyticsDto GetNumericAnalytics(IEnumerable<FieldValue> values, string fieldName)
        {
            List<double> numericValues = [.. values
                .Where(v => v.NumberValue.HasValue)
                .Select(v => v.NumberValue!.Value)];

            if (numericValues.Count == 0)
            {
                return new FieldAnalyticsDto { FieldName = fieldName };
            }

            var count = numericValues.Count;
            var min = numericValues.Min();
            var max = numericValues.Max();
            var sum = numericValues.Sum();
            var avg = numericValues.Average();
            var sumOfSquares = numericValues.Sum(x => x * x);
            var variance = sumOfSquares / count - (avg * avg);
            var stdDev = Math.Sqrt(Math.Max(0, (double)variance));

            return new FieldAnalyticsDto
            {
                FieldName = fieldName,
                Count = count,
                Min = min,
                Max = max,
                Sum = sum,
                Average = Math.Round(avg, 2),
                StdDev = Math.Round(stdDev, 2),
            };
        }

        public static FieldAnalyticsDto GetTimeSpanAnalytics(IEnumerable<FieldValue> values, string fieldName)
        {
            var timeSpanValues = values
                .Where(v => v.TimeSpanValue.HasValue)
                .Select(v => v.TimeSpanValue!.Value)
                .ToList();

            if (timeSpanValues.Count == 0)
            {
                return new FieldAnalyticsDto { FieldName = fieldName };
            }

            var count = timeSpanValues.Count;
            var minTicks = timeSpanValues.Min(x => x.Ticks);
            var maxTicks = timeSpanValues.Max(x => x.Ticks);
            var avgTicks = timeSpanValues.Average(x => (double)x.Ticks);

            return new FieldAnalyticsDto
            {
                FieldName = fieldName,
                Count = count,
                MinTimeSpan = TimeSpan.FromTicks(minTicks),
                MaxTimeSpan = TimeSpan.FromTicks(maxTicks),
                AverageTimeSpan = TimeSpan.FromTicks((long)Math.Round(avgTicks))
            };
        }

        public static FieldAnalyticsDto GetDateTimeAnalytics(IEnumerable<FieldValue> values, string fieldName)
        {
            var dateTimeValues = values
                .Where(v => v.DateTimeValue.HasValue)
                .Select(v => v.DateTimeValue!.Value)
                .ToList();

            if (dateTimeValues.Count == 0)
            {
                return new FieldAnalyticsDto { FieldName = fieldName };
            }

            return new FieldAnalyticsDto
            {
                FieldName = fieldName,
                Count = dateTimeValues.Count,
                MinDateTime = dateTimeValues.Min(),
                MaxDateTime = dateTimeValues.Max()
            };
        }

        public static FieldAnalyticsDto GetDateAnalytics(IEnumerable<FieldValue> values, string fieldName)
        {
            var dateValues = values
                .Where(v => v.DateTimeValue.HasValue)
                .Select(v => v.DateTimeValue!.Value)
                .ToList();

            if (dateValues.Count == 0)
            {
                return new FieldAnalyticsDto { FieldName = fieldName };
            }

            return new FieldAnalyticsDto
            {
                FieldName = fieldName,
                Count = dateValues.Count,
                MinDate = dateValues.Min(),
                MaxDate = dateValues.Max()
            };
        }

        public static FieldAnalyticsDto GetBooleanAnalytics(IEnumerable<FieldValue> values, string fieldName)
        {
            var booleanValues = values
                .Where(v => v.BooleanValue.HasValue)
                .Select(v => v.BooleanValue!.Value)
                .ToList();

            if (booleanValues.Count == 0)
            {
                return new FieldAnalyticsDto { FieldName = fieldName };
            }

            var count = booleanValues.Count;
            var trueCount = booleanValues.Count(v => v);
            var falseCount = count - trueCount;
            var truePercentage = (double)trueCount / count;

            return new FieldAnalyticsDto
            {
                FieldName = fieldName,
                Count = count,
                TrueCount = trueCount,
                FalseCount = falseCount,
                TruePercentage = Math.Round(truePercentage, 2)
            };
        }
    }
}

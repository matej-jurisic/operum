using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
using Operum.Model.Models;

namespace Operum.Service.Helpers
{
    public static class TrackerAnalyticsHelpers
    {
        public static Result<SingleValueAnalyticResult> GetSingleValueAnalyticResult(string code, IEnumerable<FieldValue> values)
        {
            switch (code)
            {
                case AnalyticCodes.BoolCount:
                    var boolValues = values.Where(v => v.BooleanValue.HasValue).ToList();
                    return Result.Success(new(boolValues.Count.ToString()));
                case AnalyticCodes.StringCount:
                    var stringValues = values.Where(v => v.StringValue != null).ToList();
                    return Result.Success(new(stringValues.Count.ToString()));
                case AnalyticCodes.NumberCount:
                    var numberValues = values.Where(v => v.NumberValue.HasValue).ToList();
                    return Result.Success(new(numberValues.Count.ToString()));
                case AnalyticCodes.TimespanCount:
                    var timespanValues = values.Where(v => v.TimeSpanValue.HasValue).ToList();
                    return Result.Success(new(timespanValues.Count.ToString()));
                case AnalyticCodes.DatetimeCount:
                case AnalyticCodes.DateCount:
                    var datetimeValues = values.Where(v => v.DateTimeValue.HasValue).ToList();
                    return Result.Success(new(datetimeValues.Count.ToString()));
                default:
                    return Result.Failure(StatusCodeEnum.BadRequest);
            }
        }

        public static FieldAnalyticsDto GetNumericAnalytics(IEnumerable<FieldValue> values)
        {
            var numericValues = values
                .Where(v => v.NumberValue.HasValue)
                .ToList();

            if (numericValues.Count == 0)
            {
                return new FieldAnalyticsDto();
            }

            var count = numericValues.Count;
            var minValue = numericValues.OrderBy(v => v.NumberValue!.Value).First();
            var maxValue = numericValues.OrderByDescending(v => v.NumberValue!.Value).First();

            var sum = numericValues.Sum(v => v.NumberValue!.Value);
            var sumOfSquares = numericValues.Sum(v => v.NumberValue!.Value * v.NumberValue!.Value);

            var avg = sum / count;
            var variance = sumOfSquares / count - avg * avg;
            var stdDev = Math.Sqrt(Math.Max(0, variance));

            return new FieldAnalyticsDto
            {
                Count = count,
                Min = minValue.NumberValue!.Value,
                MinEntryId = minValue.EntryId,
                Max = maxValue.NumberValue!.Value,
                MaxEntryId = maxValue.EntryId,
                Sum = sum,
                Average = Math.Round(avg, 2),
                StdDev = Math.Round(stdDev, 2)
            };
        }

        public static FieldAnalyticsDto GetTimeSpanAnalytics(IEnumerable<FieldValue> values)
        {
            var timeSpanValues = values
                .Where(v => v.TimeSpanValue.HasValue)
                .ToList();

            if (timeSpanValues.Count == 0)
            {
                return new FieldAnalyticsDto();
            }

            var count = timeSpanValues.Count;
            var minValue = timeSpanValues.OrderBy(v => v.TimeSpanValue!.Value.Ticks).First();
            var maxValue = timeSpanValues.OrderByDescending(v => v.TimeSpanValue!.Value.Ticks).First();
            var avgTicks = timeSpanValues.Average(v => (double)v.TimeSpanValue!.Value.Ticks);
            var sum = timeSpanValues.Sum(v => v.TimeSpanValue!.Value.Ticks);

            return new FieldAnalyticsDto
            {
                Count = count,
                MinTimeSpan = minValue.TimeSpanValue!.Value,
                MinTimeSpanEntryId = minValue.EntryId,
                MaxTimeSpan = maxValue.TimeSpanValue!.Value,
                MaxTimeSpanEntryId = maxValue.EntryId,
                AverageTimeSpan = TimeSpan.FromTicks((long)Math.Round(avgTicks)),
                SumTimeSpan = TimeSpan.FromTicks(sum)
            };
        }

        public static FieldAnalyticsDto GetDateTimeAnalytics(IEnumerable<FieldValue> values)
        {
            var dateTimeValues = values
                .Where(v => v.DateTimeValue.HasValue)
                .ToList();

            if (dateTimeValues.Count == 0)
            {
                return new FieldAnalyticsDto();
            }

            var minValue = dateTimeValues.OrderBy(v => v.DateTimeValue!.Value).First();
            var maxValue = dateTimeValues.OrderByDescending(v => v.DateTimeValue!.Value).First();

            return new FieldAnalyticsDto
            {
                Count = dateTimeValues.Count,
                MinDateTime = minValue.DateTimeValue!.Value,
                MinDateTimeEntryId = minValue.EntryId,
                MaxDateTime = maxValue.DateTimeValue!.Value,
                MaxDateTimeEntryId = maxValue.EntryId
            };
        }

        public static FieldAnalyticsDto GetDateAnalytics(IEnumerable<FieldValue> values)
        {
            var dateValues = values
                .Where(v => v.DateTimeValue.HasValue)
                .ToList();

            if (dateValues.Count == 0)
            {
                return new FieldAnalyticsDto();
            }

            var minValue = dateValues.OrderBy(v => v.DateTimeValue!.Value).First();
            var maxValue = dateValues.OrderByDescending(v => v.DateTimeValue!.Value).First();

            return new FieldAnalyticsDto
            {
                Count = dateValues.Count,
                MinDate = minValue.DateTimeValue!.Value,
                MinDateEntryId = minValue.EntryId,
                MaxDate = maxValue.DateTimeValue!.Value,
                MaxDateEntryId = maxValue.EntryId
            };
        }

        public static FieldAnalyticsDto GetBooleanAnalytics(IEnumerable<FieldValue> values)
        {
            var booleanValues = values
                .Where(v => v.BooleanValue.HasValue)
                .Select(v => v.BooleanValue!.Value)
                .ToList();

            if (booleanValues.Count == 0)
            {
                return new FieldAnalyticsDto();
            }

            var count = booleanValues.Count;
            var trueCount = booleanValues.Count(v => v);
            var falseCount = count - trueCount;
            var truePercentage = (double)trueCount / count;

            return new FieldAnalyticsDto
            {
                Count = count,
                TrueCount = trueCount,
                FalseCount = falseCount,
                TruePercentage = Math.Round(truePercentage, 4)
            };
        }
    }
}

using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Helpers.Analytics
{
    public static class GenericAnalyticHelpers
    {
        public static Result<(string Value, string? EntryId)> Count(IEnumerable<object?> items)
        {
            var count = items.Count(x => x != null);
            return Result.Success((count.ToString(), (string?)null));
        }

        public static Result<(string Value, string? EntryId)> CountWhere(IEnumerable<object?> items, Func<object?, bool> predicate)
        {
            var count = items.Count(predicate);
            return Result.Success((count.ToString(), (string?)null));
        }

        public static Result<(string Value, string? EntryId)> Percentage(
            IEnumerable<object?> items,
            Func<object?, bool> hasValuePredicate,
            Func<object?, bool> matchPredicate)
        {
            var all = items.Where(hasValuePredicate).ToList();
            if (all.Count == 0)
                return Result.Failure(StatusCodeEnum.NotFound, "No values found.");

            var percentage = (double)all.Count(matchPredicate) / all.Count * 100;
            return Result.Success((AnalyticFormatters.FormatNumber(percentage) + "%", (string?)null));
        }

        public static Result<(string Value, string? EntryId)> MinWithEntry(IEnumerable<FieldValue> values)
        {
            FieldValue? minField = null;
            IComparable? minValue = null;

            foreach (var fv in values)
            {
                if (fv.GetFieldValue() is not IComparable comparable) continue;
                if (minValue == null || comparable.CompareTo(minValue) < 0)
                {
                    minValue = comparable;
                    minField = fv;
                }
            }

            if (minField == null)
                return Result.Failure(StatusCodeEnum.NotFound, "No values found.");

            return Result.Success((AnalyticFormatters.FormatValue(minField), (string?)minField.EntryId));
        }

        public static Result<(string Value, string? EntryId)> MaxWithEntry(IEnumerable<FieldValue> values)
        {
            FieldValue? maxField = null;
            IComparable? maxValue = null;

            foreach (var fv in values)
            {
                if (fv.GetFieldValue() is not IComparable comparable) continue;
                if (maxValue == null || comparable.CompareTo(maxValue) > 0)
                {
                    maxValue = comparable;
                    maxField = fv;
                }
            }

            if (maxField == null)
                return Result.Failure(StatusCodeEnum.NotFound, "No values found.");

            return Result.Success((AnalyticFormatters.FormatValue(maxField), (string?)maxField.EntryId));
        }

        public static Result<(string Value, string? EntryId)> Average(IEnumerable<object?> items)
        {
            var timeSpans = items.OfType<TimeSpan>().ToList();
            if (timeSpans.Count != 0)
            {
                var avgTicks = timeSpans.Average(ts => ts.Ticks);
                var avgTimeSpan = TimeSpan.FromTicks((long)avgTicks);
                return Result.Success((AnalyticFormatters.FormatTimeSpan(avgTimeSpan), (string?)null));
            }

            var numbers = items.Where(x => x is double or int or long or decimal or float)
                .Select(Convert.ToDouble)
                .ToList();

            if (numbers.Count == 0)
                return Result.Failure(StatusCodeEnum.NotFound, "No numeric values found.");

            return Result.Success((AnalyticFormatters.FormatNumber(numbers.Average()), (string?)null));
        }

        public static Result<(string Value, string? EntryId)> Sum(IEnumerable<object?> items)
        {
            var timeSpans = items.OfType<TimeSpan>().ToList();
            if (timeSpans.Count != 0)
            {
                var sumTicks = timeSpans.Sum(ts => ts.Ticks);
                var total = TimeSpan.FromTicks(sumTicks);
                return Result.Success((AnalyticFormatters.FormatTimeSpan(total), (string?)null));
            }

            var numbers = items.Where(x => x is double or int or long or decimal or float)
                .Select(Convert.ToDouble)
                .ToList();

            if (numbers.Count == 0)
                return Result.Failure(StatusCodeEnum.NotFound, "No numeric values found.");

            return Result.Success((AnalyticFormatters.FormatNumber(numbers.Sum()), (string?)null));
        }

        public static Result<(string Value, string? EntryId)> StdDev(IEnumerable<object?> items)
        {
            var numbers = items.Where(x => x is double or int or long or decimal or float)
                .Select(Convert.ToDouble)
                .ToList();

            if (numbers.Count == 0)
                return Result.Failure(StatusCodeEnum.NotFound, "No numeric values found.");

            var avg = numbers.Average();
            var variance = numbers.Sum(x => Math.Pow(x - avg, 2)) / numbers.Count;
            var stdDev = Math.Sqrt(variance);

            return Result.Success((Math.Round(stdDev, 2).ToString(), (string?)null));
        }

        public static List<ChartPointDto> CalculateCumulativePoints(List<ChartPointDto> dataPoints)
        {
            var grouped = dataPoints
                .GroupBy(e => e.X)
                .OrderBy(g => g.Key)
                .ToList();

            var points = new List<ChartPointDto>();
            var runningTotal = 0.0;

            foreach (var g in grouped)
            {
                runningTotal += g.Sum(e => e.Y ?? 0);
                points.Add(new ChartPointDto
                {
                    X = g.Key,
                    Y = Math.Round(runningTotal, 2)
                });
            }

            return points;
        }
    }
}

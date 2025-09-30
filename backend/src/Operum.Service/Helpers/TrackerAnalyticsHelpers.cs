using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Helpers
{
    public static class TrackerAnalyticsHelpers
    {
        public static Result<SingleValueAnalyticResult> GetSingleValueAnalyticResult(
            Analytic analytic,
            IEnumerable<FieldValue> values,
            Field valueField)
        {
            var result = new SingleValueAnalyticResult
            {
                FieldName = valueField.Name,
                Name = analytic.Name,
                Description = analytic.Description,
                AnalyticId = analytic.Id
            };

            var v = values.Select(x => x.GetFieldValue()).ToList();

            var opResult = analytic.Code switch
            {
                AnalyticCodes.Count => Count(v),
                AnalyticCodes.TrueCount => CountWhere(v, x => x is bool b && b),
                AnalyticCodes.FalseCount => CountWhere(v, x => x is bool b && !b),
                AnalyticCodes.TruePercentage => Percentage(v, x => x is bool, x => x is bool b && b),
                AnalyticCodes.Min => MinWithEntry(values),
                AnalyticCodes.Max => MaxWithEntry(values),
                AnalyticCodes.Average => Average(v),
                AnalyticCodes.Sum => Sum(v),
                AnalyticCodes.StdDev => StdDev(v),
                _ => Result.Failure(
                    StatusCodeEnum.BadRequest,
                    $"Unsupported analytic code: {analytic.Code}")
            };

            if (!opResult.IsSuccess)
                return Result.Failure(opResult.StatusCode, opResult.Messages);

            result.Value = opResult.Data.Value;
            result.EntryId = opResult.Data.EntryId;
            return Result.Success(result);
        }

        // Generic operations that work on IEnumerable<object?>
        private static Result<(string Value, string? EntryId)> Count(IEnumerable<object?> items)
        {
            var count = items.Count(x => x != null);
            return Result.Success((count.ToString(), (string?)null));
        }

        private static Result<(string Value, string? EntryId)> CountWhere(
            IEnumerable<object?> items,
            Func<object?, bool> predicate)
        {
            var count = items.Count(predicate);
            return Result.Success((count.ToString(), (string?)null));
        }

        private static Result<(string Value, string? EntryId)> Percentage(
            IEnumerable<object?> items,
            Func<object?, bool> hasValuePredicate,
            Func<object?, bool> matchPredicate)
        {
            var all = items.Where(hasValuePredicate).ToList();
            if (all.Count == 0)
                return Result.Failure(StatusCodeEnum.NotFound, "No values found.");

            var percentage = (double)all.Count(matchPredicate) / all.Count * 100;
            return Result.Success((Math.Round(percentage, 2) + "%", (string?)null));
        }

        private static Result<(string Value, string? EntryId)> MinWithEntry(IEnumerable<FieldValue> values)
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

            return Result.Success((FormatValue(minField), (string?)minField.EntryId));
        }

        private static Result<(string Value, string? EntryId)> MaxWithEntry(IEnumerable<FieldValue> values)
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

            return Result.Success((FormatValue(maxField), (string?)maxField.EntryId));
        }

        private static Result<(string Value, string? EntryId)> Average(IEnumerable<object?> items)
        {
            var timeSpans = items.OfType<TimeSpan>().ToList();
            if (timeSpans.Count != 0)
            {
                var avgTicks = timeSpans.Average(ts => ts.Ticks);
                return Result.Success((TimeSpan.FromTicks((long)avgTicks).ToString(), (string?)null));
            }

            var numbers = items.Where(x => x is double or int or long or decimal or float)
                .Select(Convert.ToDouble)
                .ToList();

            if (numbers.Count == 0)
                return Result.Failure(StatusCodeEnum.NotFound, "No numeric values found.");

            return Result.Success((Math.Round(numbers.Average(), 2).ToString(), (string?)null));
        }

        private static Result<(string Value, string? EntryId)> Sum(IEnumerable<object?> items)
        {
            var timeSpans = items.OfType<TimeSpan>().ToList();
            if (timeSpans.Count != 0)
            {
                var sumTicks = timeSpans.Sum(ts => ts.Ticks);
                return Result.Success((TimeSpan.FromTicks(sumTicks).ToString(), (string?)null));
            }

            var numbers = items.Where(x => x is double or int or long or decimal or float)
                .Select(Convert.ToDouble)
                .ToList();

            if (numbers.Count == 0)
                return Result.Failure(StatusCodeEnum.NotFound, "No numeric values found.");

            return Result.Success((numbers.Sum().ToString(), (string?)null));
        }

        private static Result<(string Value, string? EntryId)> StdDev(IEnumerable<object?> items)
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

        // Helper to format value based on its type
        private static string FormatValue(FieldValue fv)
        {
            return fv.GetValueAsString() ?? string.Empty;
        }

        public static Result<NumericChartAnalyticResult> GetNumericChartAnalyticResult(
            Analytic analytic,
            List<Entry> entries,
            Field xAxisField,
            Field yAxisField)
        {
            var result = new NumericChartAnalyticResult
            {
                XFieldName = xAxisField.Name,
                YFieldName = yAxisField.Name,
                Name = analytic.Name,
                Description = analytic.Description,
                AnalyticId = analytic.Id,
                YFieldType = yAxisField.Type
            };

            if (!AnalyticCodes.IsValid(analytic.Code))
            {
                return Result.Success(result);
            }

            var dataPoints = entries
                .Select(x => new ChartPointDto
                {
                    X = x.FieldValues.FirstOrDefault(f => f.FieldId == xAxisField.Id)?.GetValueAsString(),
                    Y = ConvertToNumeric(x.FieldValues.FirstOrDefault(f => f.FieldId == yAxisField.Id))
                })
                .Where(p => p.X != null)
                .ToList();

            var points = analytic.Code switch
            {
                AnalyticCodes.AggregatedLineChart => [.. dataPoints
                    .GroupBy(e => e.X)
                    .Select(g => new ChartPointDto
                    {
                        X = g.Key,
                        Y = Math.Round(g.Sum(e => e.Y ?? 0), 2)
                    })],
                AnalyticCodes.LineChart => dataPoints,
                AnalyticCodes.CumulativeLineChart => CalculateCumulativePoints(dataPoints),

                _ => []
            };

            result.Points = points;
            return Result.Success(result);
        }

        private static List<ChartPointDto> CalculateCumulativePoints(List<ChartPointDto> dataPoints)
        {
            var grouped = dataPoints
                .GroupBy(e => e.X)
                .OrderBy(g => g.Key)
                .ToList();

            var points = new List<ChartPointDto>();
            var runningTotal = 0.0;

            foreach (var group in grouped)
            {
                runningTotal += group.Sum(e => e.Y ?? 0);
                points.Add(new ChartPointDto
                {
                    X = group.Key,
                    Y = Math.Round(runningTotal, 2)
                });
            }

            return points;
        }

        // Convert any field value to a numeric representation for charting
        private static double ConvertToNumeric(FieldValue? fieldValue)
        {
            if (fieldValue == null) return 0;

            var value = fieldValue.GetFieldValue();

            return value switch
            {
                double d => d,
                TimeSpan ts => ts.TotalMinutes,
                bool b => b ? 1.0 : 0.0,
                _ => 0
            };
        }
    }
}

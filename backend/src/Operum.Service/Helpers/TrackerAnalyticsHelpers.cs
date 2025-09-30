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

            switch (analytic.Code)
            {
                // -------------------
                // BOOLEAN
                // -------------------
                case AnalyticCodes.BoolCount:
                    {
                        var count = values.Count(v => v.BooleanValue.HasValue);
                        result.Value = count.ToString();
                        return Result.Success(result);
                    }

                case AnalyticCodes.BoolTrueCount:
                    {
                        var count = values.Count(v => v.BooleanValue == true);
                        result.Value = count.ToString();
                        return Result.Success(result);
                    }

                case AnalyticCodes.BoolFalseCount:
                    {
                        var count = values.Count(v => v.BooleanValue == false);
                        result.Value = count.ToString();
                        return Result.Success(result);
                    }

                case AnalyticCodes.BoolTruePercentage:
                    {
                        var all = values.Where(v => v.BooleanValue.HasValue).ToList();
                        if (all.Count == 0)
                            return Result.Failure(StatusCodeEnum.NotFound, "No boolean values found.");

                        var percentage = (double)all.Count(v => v.BooleanValue == true) / all.Count * 100;
                        result.Value = Math.Round(percentage, 2) + "%";
                        return Result.Success(result);
                    }

                // -------------------
                // STRING
                // -------------------
                case AnalyticCodes.StringCount:
                    {
                        var count = values.Count(v => v.StringValue != null);
                        result.Value = count.ToString();
                        return Result.Success(result);
                    }

                // -------------------
                // NUMBER
                // -------------------
                case AnalyticCodes.NumberCount:
                    {
                        var count = values.Count(v => v.NumberValue.HasValue);
                        result.Value = count.ToString();
                        return Result.Success(result);
                    }

                case AnalyticCodes.NumberMin:
                    {
                        var minField = values
                            .Where(v => v.NumberValue.HasValue)
                            .OrderBy(v => v.NumberValue!.Value)
                            .FirstOrDefault();

                        if (minField == null)
                            return Result.Failure(StatusCodeEnum.NotFound, "No numeric values found.");

                        result.Value = minField.NumberValue!.Value.ToString();
                        result.EntryId = minField.EntryId;
                        return Result.Success(result);
                    }

                case AnalyticCodes.NumberMax:
                    {
                        var maxField = values
                            .Where(v => v.NumberValue.HasValue)
                            .OrderByDescending(v => v.NumberValue!.Value)
                            .FirstOrDefault();

                        if (maxField == null)
                            return Result.Failure(StatusCodeEnum.NotFound, "No numeric values found.");

                        result.Value = maxField.NumberValue!.Value.ToString();
                        result.EntryId = maxField.EntryId;
                        return Result.Success(result);
                    }

                case AnalyticCodes.NumberAverage:
                    {
                        var nums = values.Where(v => v.NumberValue.HasValue).Select(v => v.NumberValue!.Value).ToList();
                        if (nums.Count == 0)
                            return Result.Failure(StatusCodeEnum.NotFound, "No numeric values found.");

                        result.Value = Math.Round(nums.Average(), 2).ToString();
                        return Result.Success(result);
                    }

                case AnalyticCodes.NumberSum:
                    {
                        var nums = values.Where(v => v.NumberValue.HasValue).Select(v => v.NumberValue!.Value).ToList();
                        if (nums.Count == 0)
                            return Result.Failure(StatusCodeEnum.NotFound, "No numeric values found.");

                        result.Value = nums.Sum().ToString();
                        return Result.Success(result);
                    }

                case AnalyticCodes.NumberStdDev:
                    {
                        var nums = values.Where(v => v.NumberValue.HasValue).Select(v => v.NumberValue!.Value).ToList();
                        if (nums.Count == 0)
                            return Result.Failure(StatusCodeEnum.NotFound, "No numeric values found.");

                        var count = nums.Count;
                        var avg = nums.Average();
                        var variance = nums.Sum(x => Math.Pow(x - avg, 2)) / count;
                        var stdDev = Math.Sqrt(variance);

                        result.Value = Math.Round(stdDev, 2).ToString();
                        return Result.Success(result);
                    }

                // -------------------
                // TIMESPAN
                // -------------------
                case AnalyticCodes.TimespanCount:
                    {
                        var count = values.Count(v => v.TimeSpanValue.HasValue);
                        result.Value = count.ToString();
                        return Result.Success(result);
                    }

                case AnalyticCodes.TimespanMin:
                    {
                        var minField = values
                            .Where(v => v.TimeSpanValue.HasValue)
                            .OrderBy(v => v.TimeSpanValue!.Value)
                            .FirstOrDefault();

                        if (minField == null)
                            return Result.Failure(StatusCodeEnum.NotFound, "No timespan values found.");

                        result.Value = minField.TimeSpanValue!.Value.ToString();
                        result.EntryId = minField.EntryId;
                        return Result.Success(result);
                    }

                case AnalyticCodes.TimespanMax:
                    {
                        var maxField = values
                            .Where(v => v.TimeSpanValue.HasValue)
                            .OrderByDescending(v => v.TimeSpanValue!.Value)
                            .FirstOrDefault();

                        if (maxField == null)
                            return Result.Failure(StatusCodeEnum.NotFound, "No timespan values found.");

                        result.Value = maxField.TimeSpanValue!.Value.ToString();
                        result.EntryId = maxField.EntryId;
                        return Result.Success(result);
                    }

                case AnalyticCodes.TimespanAverage:
                    {
                        var spans = values.Where(v => v.TimeSpanValue.HasValue).Select(v => v.TimeSpanValue!.Value).ToList();
                        if (spans.Count == 0)
                            return Result.Failure(StatusCodeEnum.NotFound, "No timespan values found.");

                        var avgTicks = spans.Average(s => s.Ticks);
                        result.Value = TimeSpan.FromTicks((long)avgTicks).ToString();
                        return Result.Success(result);
                    }

                case AnalyticCodes.TimespanSum:
                    {
                        var spans = values.Where(v => v.TimeSpanValue.HasValue).Select(v => v.TimeSpanValue!.Value).ToList();
                        if (spans.Count == 0)
                            return Result.Failure(StatusCodeEnum.NotFound, "No timespan values found.");

                        var sumTicks = spans.Sum(s => s.Ticks);
                        result.Value = TimeSpan.FromTicks(sumTicks).ToString();
                        return Result.Success(result);
                    }

                // -------------------
                // DATETIME
                // -------------------
                case AnalyticCodes.DatetimeCount:
                    {
                        var count = values.Count(v => v.DateTimeValue.HasValue);
                        result.Value = count.ToString();
                        return Result.Success(result);
                    }

                case AnalyticCodes.DatetimeMin:
                    {
                        var minField = values
                            .Where(v => v.DateTimeValue.HasValue)
                            .OrderBy(v => v.DateTimeValue!.Value)
                            .FirstOrDefault();

                        if (minField == null)
                            return Result.Failure(StatusCodeEnum.NotFound, "No datetime values found.");

                        result.Value = minField.DateTimeValue!.Value.ToString("u");
                        result.EntryId = minField.EntryId;
                        return Result.Success(result);
                    }

                case AnalyticCodes.DatetimeMax:
                    {
                        var maxField = values
                            .Where(v => v.DateTimeValue.HasValue)
                            .OrderByDescending(v => v.DateTimeValue!.Value)
                            .FirstOrDefault();

                        if (maxField == null)
                            return Result.Failure(StatusCodeEnum.NotFound, "No datetime values found.");

                        result.Value = maxField.DateTimeValue!.Value.ToString("u");
                        result.EntryId = maxField.EntryId;
                        return Result.Success(result);
                    }

                // -------------------
                // DATE
                // -------------------
                case AnalyticCodes.DateCount:
                    {
                        var count = values.Count(v => v.DateTimeValue.HasValue);
                        result.Value = count.ToString();
                        return Result.Success(result);
                    }

                case AnalyticCodes.DateMin:
                    {
                        var minField = values
                            .Where(v => v.DateTimeValue.HasValue)
                            .OrderBy(v => v.DateTimeValue!.Value.Date)
                            .FirstOrDefault();

                        if (minField == null)
                            return Result.Failure(StatusCodeEnum.NotFound, "No date values found.");

                        result.Value = minField.DateTimeValue!.Value.Date.ToShortDateString();
                        result.EntryId = minField.EntryId;
                        return Result.Success(result);
                    }

                case AnalyticCodes.DateMax:
                    {
                        var maxField = values
                            .Where(v => v.DateTimeValue.HasValue)
                            .OrderByDescending(v => v.DateTimeValue!.Value.Date)
                            .FirstOrDefault();

                        if (maxField == null)
                            return Result.Failure(StatusCodeEnum.NotFound, "No date values found.");

                        result.Value = maxField.DateTimeValue!.Value.Date.ToShortDateString();
                        result.EntryId = maxField.EntryId;
                        return Result.Success(result);
                    }

                default:
                    return Result.Failure(StatusCodeEnum.BadRequest, $"Unsupported analytic code: {analytic.Code}");
            }
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
                AnalyticId = analytic.Id
            };

            switch (analytic.Code)
            {
                case AnalyticCodes.DateNumberLineChart:
                    {
                        var points = entries
                            .Select(x => new
                            {
                                X = x.FieldValues.FirstOrDefault(f => f.FieldId == xAxisField.Id)?.GetValueAsString(),
                                Y = x.FieldValues.FirstOrDefault(f => f.FieldId == yAxisField.Id)?.NumberValue ?? 0
                            })
                            .GroupBy(e => e.X)
                            .Select(g => new ChartPointDto
                            {
                                X = g.Key,
                                Y = Math.Round(g.Sum(e => e.Y), 2)
                            })
                            .ToList();

                        result.Points = points;
                        return Result.Success(result);
                    }

                case AnalyticCodes.DatetimeNumberLineChart:
                    {
                        var points = entries
                            .Select(x => new
                            {
                                X = x.FieldValues.FirstOrDefault(f => f.FieldId == xAxisField.Id)?.GetValueAsString(),
                                Y = x.FieldValues.FirstOrDefault(f => f.FieldId == yAxisField.Id)?.NumberValue ?? 0
                            })
                            .GroupBy(e => e.X)
                            .Select(g => new ChartPointDto
                            {
                                X = g.Key,
                                Y = Math.Round(g.Sum(e => e.Y), 2)
                            })
                            .ToList();
                        result.Points = points;
                        return Result.Success(result);
                    }

                case AnalyticCodes.DateTimespanLineChart:
                    {
                        var points = entries
                            .Select(x => new
                            {
                                X = x.FieldValues.FirstOrDefault(f => f.FieldId == xAxisField.Id)?.GetValueAsString(),
                                Y = x.FieldValues.FirstOrDefault(f => f.FieldId == yAxisField.Id)?.TimeSpanValue?.TotalMinutes ?? 0
                            })
                            .GroupBy(e => e.X)
                            .Select(g => new ChartPointDto
                            {
                                X = g.Key,
                                Y = Math.Round(g.Sum(e => e.Y), 2)
                            })
                            .ToList();
                        result.Points = points;
                        return Result.Success(result);
                    }

                case AnalyticCodes.DatetimeTimespanLineChart:
                    {
                        var points = entries
                            .Select(x => new
                            {
                                X = x.FieldValues.FirstOrDefault(f => f.FieldId == xAxisField.Id)?.GetValueAsString(),
                                Y = x.FieldValues.FirstOrDefault(f => f.FieldId == yAxisField.Id)?.TimeSpanValue?.TotalMinutes ?? 0
                            })
                            .GroupBy(e => e.X)
                            .Select(g => new ChartPointDto
                            {
                                X = g.Key,
                                Y = Math.Round(g.Sum(e => e.Y), 2)
                            })
                            .ToList();
                        result.Points = points;
                        return Result.Success(result);
                    }

                case AnalyticCodes.DateBoolTimeChart:
                    {
                        var points = entries
                            .Select(x => new
                            {
                                X = x.FieldValues.FirstOrDefault(f => f.FieldId == xAxisField.Id)?.GetValueAsString(),
                                Y = x.FieldValues.FirstOrDefault(f => f.FieldId == yAxisField.Id)?.BooleanValue == true ? 1.0 : 0.0
                            })
                            .GroupBy(e => e.X)
                            .Select(g => new ChartPointDto
                            {
                                X = g.Key,
                                Y = Math.Round(g.Sum(e => e.Y), 2)
                            })
                            .ToList();
                        result.Points = points;
                        return Result.Success(result);
                    }

                case AnalyticCodes.DatetimeBoolLineChart:
                    {
                        var points = entries
                            .Select(x => new
                            {
                                X = x.FieldValues.FirstOrDefault(f => f.FieldId == xAxisField.Id)?.GetValueAsString(),
                                Y = x.FieldValues.FirstOrDefault(f => f.FieldId == yAxisField.Id)?.BooleanValue == true ? 1.0 : 0.0
                            })
                            .GroupBy(e => e.X)
                            .Select(g => new ChartPointDto
                            {
                                X = g.Key,
                                Y = Math.Round(g.Sum(e => e.Y), 2)
                            })
                            .ToList();
                        result.Points = points;
                        return Result.Success(result);
                    }

                case AnalyticCodes.StringNumberLineChart:
                    {
                        var points = entries
                            .Select(x => new
                            {
                                X = x.FieldValues.FirstOrDefault(f => f.FieldId == xAxisField.Id)?.GetValueAsString(),
                                Y = x.FieldValues.FirstOrDefault(f => f.FieldId == yAxisField.Id)?.NumberValue ?? 0
                            })
                            .GroupBy(e => e.X)
                            .Select(g => new ChartPointDto
                            {
                                X = g.Key,
                                Y = Math.Round(g.Sum(e => e.Y), 2)
                            })
                            .ToList();
                        result.Points = points;
                        return Result.Success(result);
                    }

                case AnalyticCodes.StringBoolLineChart:
                    {
                        var points = entries
                            .Select(x => new
                            {
                                X = x.FieldValues.FirstOrDefault(f => f.FieldId == xAxisField.Id)?.GetValueAsString(),
                                Y = x.FieldValues.FirstOrDefault(f => f.FieldId == yAxisField.Id)?.BooleanValue == true ? 1.0 : 0.0
                            })
                            .GroupBy(e => e.X)
                            .Select(g => new ChartPointDto
                            {
                                X = g.Key,
                                Y = Math.Round(g.Sum(e => e.Y), 2)
                            })
                            .ToList();
                        result.Points = points;
                        return Result.Success(result);
                    }
                default:
                    return Result.Failure(StatusCodeEnum.BadRequest, $"Unsupported analytic code: {analytic.Code}");
            }
        }
    }
}

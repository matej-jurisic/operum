using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Helpers.Analytics
{
    public static class AnalyticResultGetters
    {
        public static SingleValueAnalyticResult GetSingleValueAnalyticResult(
            Analytic analytic,
            IEnumerable<Entry> entries,
            Dictionary<string, Field> fields)
        {
            var result = new SingleValueAnalyticResult
            {
                Name = analytic.Code,
                Description = analytic.Description,
                AnalyticId = analytic.Id
            };

            var valueField = fields.GetValueOrDefault(AnalyticDataTypePurposes.Value);
            if (valueField == null)
                return result;


            var allValues = entries
                .SelectMany(e => e.FieldValues.Where(fv => fv.FieldId == valueField.Id))
                .ToList();

            var rawValues = allValues.Select(x => x.GetFieldValue()).ToList();

            var opResult = analytic.Code switch
            {
                AnalyticCodes.Count => GenericAnalyticHelpers.Count(rawValues),
                AnalyticCodes.TrueCount => GenericAnalyticHelpers.CountWhere(rawValues, x => x is bool b && b),
                AnalyticCodes.FalseCount => GenericAnalyticHelpers.CountWhere(rawValues, x => x is bool b && !b),
                AnalyticCodes.TruePercentage => GenericAnalyticHelpers.Percentage(rawValues, x => x is bool, x => x is bool b && b),
                AnalyticCodes.Min => GenericAnalyticHelpers.MinWithEntry(allValues),
                AnalyticCodes.Max => GenericAnalyticHelpers.MaxWithEntry(allValues),
                AnalyticCodes.Average => GenericAnalyticHelpers.Average(rawValues),
                AnalyticCodes.Sum => GenericAnalyticHelpers.Sum(rawValues),
                AnalyticCodes.StdDev => GenericAnalyticHelpers.StdDev(rawValues),
                _ => Result.Failure(StatusCodeEnum.BadRequest, $"Unsupported analytic code: {analytic.Code}")
            };

            if (!opResult.IsSuccess)
                return result;

            result.Value = opResult.Data.Value;
            result.EntryId = opResult.Data.EntryId;
            result.FieldName = valueField.Name;

            return result;
        }

        public static NumericChartAnalyticResult GetNumericChartAnalyticResult(
            Analytic analytic,
            IEnumerable<Entry> entries,
            Dictionary<string, Field> fields)
        {

            var result = new NumericChartAnalyticResult
            {
                Name = analytic.Code,
                Description = analytic.Description,
                AnalyticId = analytic.Id
            };

            var xField = fields.GetValueOrDefault(AnalyticDataTypePurposes.Xaxis);
            var yField = fields.GetValueOrDefault(AnalyticDataTypePurposes.Yaxis);

            if (xField == null || yField == null)
                return result;

            var dataPoints = entries
                .Select(e => new ChartPointDto
                {
                    X = e.FieldValues.FirstOrDefault(f => f.FieldId == xField.Id)?.GetValueAsString(),
                    Y = AnalyticFormatters.ConvertToNumeric(e.FieldValues.FirstOrDefault(f => f.FieldId == yField.Id))
                })
                .Where(p => p.X != null)
                .ToList();

            var points = analytic.Code switch
            {
                AnalyticCodes.LineChart => dataPoints,
                AnalyticCodes.AggregatedLineChart => [.. dataPoints
                    .GroupBy(e => e.X)
                    .Select(g => new ChartPointDto
                    {
                        X = g.Key,
                        Y = Math.Round(g.Sum(e => e.Y ?? 0), 2)
                    })],
                AnalyticCodes.CumulativeLineChart => GenericAnalyticHelpers.CalculateCumulativePoints(dataPoints),
                _ => []
            };

            result.Points = points;
            result.XFieldName = xField.Name;
            result.YFieldName = yField.Name;
            result.YFieldType = yField.Type;
            return result;
        }

        public static ScatterPlotAnalyticResult GetScatterPlotAnalyticResult(
            Analytic analytic,
            IEnumerable<Entry> entries,
            Dictionary<string, Field> fields)
        {
            var result = new ScatterPlotAnalyticResult
            {
                Name = analytic.Code,
                Description = analytic.Description,
                AnalyticId = analytic.Id
            };

            var xField = fields.GetValueOrDefault(AnalyticDataTypePurposes.Xaxis);
            var yField = fields.GetValueOrDefault(AnalyticDataTypePurposes.Yaxis);

            if (xField == null || yField == null)
                return result;


            var dataPoints = entries
                .Select(e => new ScatterPointDto
                {
                    X = AnalyticFormatters.ConvertToNumeric(e.FieldValues.FirstOrDefault(f => f.FieldId == xField.Id)),
                    Y = AnalyticFormatters.ConvertToNumeric(e.FieldValues.FirstOrDefault(f => f.FieldId == yField.Id))
                })
                .Where(p => p.X.HasValue && p.Y.HasValue)
                .ToList();

            result.Points = dataPoints;
            result.YFieldType = yField.Type;
            result.YFieldName = yField.Name;
            result.XFieldName = xField.Name;
            result.XFieldType = xField.Type;
            return result;
        }

        public static CalendarAnalyticResult GetCalendarAnalyticResult(
            Analytic analytic,
            IEnumerable<Entry> entries,
            Dictionary<string, Field> fields)
        {
            var result = new CalendarAnalyticResult
            {
                Name = analytic.Code,
                Description = analytic.Description,
                AnalyticId = analytic.Id
            };

            var whenField = fields.GetValueOrDefault(AnalyticDataTypePurposes.When);
            var whatField = fields.GetValueOrDefault(AnalyticDataTypePurposes.What);

            if (whenField == null || whatField == null)
                return result;

            var calendarPoints = entries
                .Select(e => new CalendarPointDto()
                {
                    EntryId = e.Id,
                    Date = e.FieldValues.FirstOrDefault(f => f.FieldId == whenField.Id)?.DateTimeValue,
                    Name = e.FieldValues.FirstOrDefault(f => f.FieldId == whatField.Id)?.GetValueAsString(),
                })
                .Where(p => p.Date != null && p.Name != null)
                .ToList();

            result.Points = calendarPoints;
            result.DateFieldName = whenField.Name;
            result.EventFieldName = whatField.Name;
            return result;
        }
    }
}

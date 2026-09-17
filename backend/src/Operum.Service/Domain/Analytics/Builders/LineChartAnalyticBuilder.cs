using System.Globalization;
using Operum.Model.Common;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.Constants.Fields;
using Operum.Model.Converters;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Service.Domain.Analytics.Processors;

namespace Operum.Service.Domain.Analytics.Builders
{
    public class LineChartAnalyticBuilder : AnalyticResultBuilderBase
    {
        public override string SupportedType => AnalyticTypes.LineChart;

        protected override Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request)
        {
            var result = new LineChartAnalyticDto
            {
                Name = AnalyticDefinitionList.GetLabel(SupportedType, request.Analytic.Code, request.Analytic.Grouping),
                Description = request.Analytic.Description,
                Id = request.Analytic.Id
            };

            var code = request.Analytic.Code;
            var grouping = request.Analytic.Grouping ?? AnalyticGroupings.None;

            var xField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Xaxis);
            var yField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Yaxis);

            // Count is the only aggregation that reads no value field.
            var countsRows = code == AnalyticCodes.Count;
            if (xField == null || (yField == null && !countsRows))
                return Result.Success<AnalyticDto>(result);

            var dataPoints = request.Entries
                .Select(e => new LineChartPointDto
                {
                    X = e.FieldValues.FirstOrDefault(f => f.FieldId == xField.Id)?.GetValueAsString(),
                    Y = yField == null
                        ? null
                        : DataFormatters.FieldValueToNullableDouble(e.FieldValues.FirstOrDefault(f => f.FieldId == yField.Id))
                })
                .Where(p => p.X != null && (countsRows || p.Y != null))
                .ToList();

            // Points must be sorted by x here: entries arrive in whatever order the source
            // query produced, and connecting them unsorted draws a meaningless zig-zag.
            dataPoints = OrderByX(dataPoints, xField.Type);

            ILineChartProcessor processor = code == AnalyticCodes.RawValues
                ? new LineChartProcessor()
                : new GroupedLineChartProcessor(grouping, code);

            result.Points = processor.Process(dataPoints);

            if (yField != null)
                result.YField = new()
                {
                    Id = yField.Id,
                    Type = yField.Type,
                    Required = yField.Required,
                    Description = yField.Description,
                    Name = yField.Name,
                };

            result.XField = new()
            {
                Id = xField.Id,
                Type = xField.Type,
                Required = xField.Required,
                Description = xField.Description,
                Name = xField.Name,
            };

            return Result.Success<AnalyticDto>(result);
        }

        // Dates/datetimes are round-trip ("o") formatted so they already sort as text;
        // numbers and timespans don't, so ordering is type-aware.
        private static List<LineChartPointDto> OrderByX(List<LineChartPointDto> points, string xFieldType) =>
            xFieldType.ToLowerInvariant() switch
            {
                DataTypes.Number => [.. points.OrderBy(p => TryParseNumber(p.X))],
                DataTypes.TimeSpan => [.. points.OrderBy(p => TryParseTimeSpan(p.X))],
                DataTypes.Date or DataTypes.DateTime => [.. points.OrderBy(p => DataFormatters.StringToDateTime(p.X!))],
                DataTypes.Bool => [.. points.OrderBy(p => TryParseBool(p.X))],
                _ => [.. points.OrderBy(p => p.X!, StringComparer.Ordinal)],
            };

        private static double? TryParseNumber(string? value) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : null;

        private static TimeSpan? TryParseTimeSpan(string? value) =>
            TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var t) ? t : null;

        private static bool? TryParseBool(string? value) =>
            bool.TryParse(value, out var b) ? b : null;
    }
}

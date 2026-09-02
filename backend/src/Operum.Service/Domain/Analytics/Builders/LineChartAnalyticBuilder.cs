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
        private readonly Dictionary<string, ILineChartProcessor> _processors;

        public override string SupportedType => AnalyticTypes.LineChart;

        public LineChartAnalyticBuilder()
        {
            _processors = new Dictionary<string, ILineChartProcessor>
            {
                [AnalyticCodes.LineChart] = new LineChartProcessor(),
                [AnalyticCodes.AggregatedSumLineChart] = new AggregatedSumLineChartProcessor(),
                [AnalyticCodes.CumulativeLineChart] = new CumulativeLineChartProcessor(),
                [AnalyticCodes.DailyLineChart] = new DailyLineChartProcessor(),
                [AnalyticCodes.WeeklyLineChart] = new WeeklyLineChartProcessor(),
                [AnalyticCodes.MonthlyLineChart] = new MonthlyLineChartProcessor(),
                [AnalyticCodes.YearlyLineChart] = new YearlyLineChartProcessor()
            };
        }

        protected override Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request)
        {
            var result = new LineChartAnalyticDto
            {
                Name = AnalyticDefinitionList.GetLabel(SupportedType, request.Analytic.Code),
                Description = request.Analytic.Description,
                Id = request.Analytic.Id
            };

            var xField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Xaxis);
            var yField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Yaxis);

            if (xField == null || yField == null)
                return Result.Success<AnalyticDto>(result);

            var dataPoints = request.Entries
                .Select(e => new LineChartPointDto
                {
                    X = e.FieldValues.FirstOrDefault(f => f.FieldId == xField.Id)?.GetValueAsString(),
                    Y = DataFormatters.FieldValueToNullableDouble(e.FieldValues.FirstOrDefault(f => f.FieldId == yField.Id))
                })
                .Where(p => p.X != null && p.Y != null)
                .ToList();

            // A line chart is read left-to-right along its x-axis, so the points are ordered
            // by x here rather than left in entry order. Entries arrive in whatever order the
            // query produced them: a linked view can sort on any field, or on none, and
            // connecting the line in that order draws a meaningless zig-zag. The analytic
            // query has no row limit, so a view's sort only ever changed the draw order, not
            // which entries are plotted, and its filters still apply as before. The date
            // buckets (daily/weekly/...) re-sort their own output, so this is a no-op for
            // them; for the cumulative variant it also makes the running total correct
            // instead of dependent on insertion order.
            dataPoints = OrderByX(dataPoints, xField.Type);

            if (!_processors.TryGetValue(request.Analytic.Code, out var processor))
                return Result.Failure(ResultStatusCodes.BadRequest,
                    $"Unsupported analytic code: {request.Analytic.Code}");

            result.Points = processor.Process(dataPoints);
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

        // X reaches us as the field value's display string. Dates and datetimes are
        // round-trip ("o") formatted so they already sort chronologically as text, but
        // numbers ("9.00" vs "100.00") and timespans do not, so the ordering is type-aware.
        // A value that fails to parse sorts first; null X is already filtered out upstream.
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

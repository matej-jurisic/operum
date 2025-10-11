using Operum.Model.Common;
using Operum.Model.Constants.Analytics;
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
                [AnalyticCodes.CumulativeLineChart] = new CumulativeLineChartProcessor()
            };
        }

        protected override Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request)
        {
            var result = new LineChartAnalyticDto
            {
                Name = request.Analytic.Code,
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
                    Y = DataFormatters.FieldValueToDouble(e.FieldValues.FirstOrDefault(f => f.FieldId == yField.Id))
                })
                .Where(p => p.X != null)
                .ToList();

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
    }
}

using Operum.Model.Common;
using Operum.Model.Constants.Analytics;
using Operum.Model.Converters;
using Operum.Model.DTOs.Analytics;

namespace Operum.Service.Domain.Analytics.Builders
{
    public class ScatterChartAnalyticBuilder : AnalyticResultBuilderBase
    {
        public override string SupportedType => AnalyticTypes.ScatterChart;

        protected override Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request)
        {
            var result = new ScatterPlotAnalyticDto
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
                .Select(e => new ScatterChartPointDto
                {
                    X = DataFormatters.FieldValueToDouble(e.FieldValues.FirstOrDefault(f => f.FieldId == xField.Id)),
                    Y = DataFormatters.FieldValueToDouble(e.FieldValues.FirstOrDefault(f => f.FieldId == yField.Id))
                })
                .Where(p => p.X.HasValue && p.Y.HasValue)
                .ToList();

            result.Points = dataPoints;
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

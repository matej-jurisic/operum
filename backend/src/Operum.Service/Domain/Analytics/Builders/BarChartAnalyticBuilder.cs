using Operum.Model.Common;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.Converters;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Service.Domain.Analytics.Processors;

namespace Operum.Service.Domain.Analytics.Builders
{
    public class BarChartAnalyticBuilder : AnalyticResultBuilderBase
    {
        private readonly Dictionary<string, IBarChartProcessor> _processors;

        public override string SupportedType => AnalyticTypes.BarChart;

        public BarChartAnalyticBuilder()
        {
            _processors = new Dictionary<string, IBarChartProcessor>
            {
                [AnalyticCodes.CountBarChart] = new CountBarChartProcessor(),
                [AnalyticCodes.SumBarChart] = new SumBarChartProcessor(),
                [AnalyticCodes.AverageBarChart] = new AverageBarChartProcessor()
            };
        }

        protected override Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request)
        {
            var result = new BarChartAnalyticDto
            {
                Name = AnalyticDefinitionList.GetLabel(SupportedType, request.Analytic.Code),
                Description = request.Analytic.Description,
                Id = request.Analytic.Id,
            };

            var nameField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Name);
            if (nameField == null)
                return Result.Success<AnalyticDto>(result);

            if (!_processors.TryGetValue(request.Analytic.Code, out var processor))
                return Result.Failure(ResultStatusCodes.BadRequest,
                    $"Unsupported analytic code: {request.Analytic.Code}");

            List<DonutChartPointDto> dataPoints;

            if (request.Analytic.Code == AnalyticCodes.CountBarChart)
            {
                dataPoints = request.Entries
                    .Select(e => new DonutChartPointDto
                    {
                        Name = e.FieldValues.FirstOrDefault(f => f.FieldId == nameField.Id)?.GetValueAsString(),
                        Value = 1
                    })
                    .Where(p => p.Name != null)
                    .ToList();
            }
            else
            {
                var valueField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Value);
                if (valueField == null)
                    return Result.Success<AnalyticDto>(result);

                dataPoints = request.Entries
                    .Select(e => new DonutChartPointDto
                    {
                        Name = e.FieldValues.FirstOrDefault(f => f.FieldId == nameField.Id)?.GetValueAsString(),
                        Value = DataFormatters.FieldValueToDouble(e.FieldValues.FirstOrDefault(f => f.FieldId == valueField.Id))
                    })
                    .Where(p => p.Name != null)
                    .ToList();

                result.ValueField = new()
                {
                    Id = valueField.Id,
                    Type = valueField.Type,
                    Required = valueField.Required,
                    Description = valueField.Description,
                    Name = valueField.Name,
                };
            }

            result.Points = processor.Process(dataPoints);
            result.NameField = new()
            {
                Id = nameField.Id,
                Type = nameField.Type,
                Required = nameField.Required,
                Description = nameField.Description,
                Name = nameField.Name,
            };

            return Result.Success<AnalyticDto>(result);
        }
    }
}

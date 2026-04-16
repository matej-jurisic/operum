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
    public class DonutChartAnalyticBuilder : AnalyticResultBuilderBase
    {
        private readonly Dictionary<string, IDonutChartProcessor> _processors;

        public override string SupportedType => AnalyticTypes.Donut;

        public DonutChartAnalyticBuilder()
        {
            _processors = new Dictionary<string, IDonutChartProcessor>
            {
                [AnalyticCodes.DonutChart] = new DonutChartProcessor()
            };
        }

        protected override Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request)
        {
            var result = new DonutChartAnalyticDto
            {
                Name = AnalyticDefinitionList.GetLabel(SupportedType, request.Analytic.Code),
                Description = request.Analytic.Description,
                Id = request.Analytic.Id,
            };

            var nameField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Name);
            var valueField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Value);

            if (nameField == null || valueField == null)
                return Result.Success<AnalyticDto>(result);

            var dataPoints = request.Entries
                .Select(e => new DonutChartPointDto
                {
                    Name = e.FieldValues.FirstOrDefault(f => f.FieldId == nameField.Id)?.GetValueAsString(),
                    Value = DataFormatters.FieldValueToDouble(e.FieldValues.FirstOrDefault(f => f.FieldId == valueField.Id))
                })
                .Where(p => p.Name != null && p.Value != null)
                .ToList();

            if (!_processors.TryGetValue(request.Analytic.Code, out var processor))
                return Result.Failure(ResultStatusCodes.BadRequest,
                    $"Unsupported analytic code: {request.Analytic.Code}");

            result.Points = processor.Process(dataPoints);
            result.ValueField = new()
            {
                Id = valueField.Id,
                Type = valueField.Type,
                Required = valueField.Required,
                Description = valueField.Description,
                Name = valueField.Name,
            };
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

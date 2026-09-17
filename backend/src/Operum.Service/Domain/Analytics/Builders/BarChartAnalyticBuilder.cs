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
        public override string SupportedType => AnalyticTypes.BarChart;

        protected override Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request)
        {
            var result = new BarChartAnalyticDto
            {
                Name = AnalyticDefinitionList.GetLabel(SupportedType, request.Analytic.Code, request.Analytic.Grouping),
                Description = request.Analytic.Description,
                Id = request.Analytic.Id,
            };

            var code = request.Analytic.Code;
            var grouping = request.Analytic.Grouping ?? AnalyticGroupings.Exact;

            var nameField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Name);
            if (nameField == null)
                return Result.Success<AnalyticDto>(result);

            // Count is the only aggregation that reads no value field.
            var countsRows = code == AnalyticCodes.Count;
            var valueField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Value);
            if (valueField == null && !countsRows)
                return Result.Success<AnalyticDto>(result);

            var dataPoints = request.Entries
                .Select(e => new DonutChartPointDto
                {
                    Name = e.FieldValues.FirstOrDefault(f => f.FieldId == nameField.Id)?.GetValueAsString(),
                    Value = valueField == null
                        ? null
                        : DataFormatters.FieldValueToNullableDouble(e.FieldValues.FirstOrDefault(f => f.FieldId == valueField.Id))
                })
                .Where(p => p.Name != null && (countsRows || p.Value != null))
                .ToList();

            IBarChartProcessor processor = code == AnalyticCodes.RawValues
                ? new BarChartProcessor()
                : new GroupedBarChartProcessor(grouping, code);

            result.Points = processor.Process(dataPoints);

            if (valueField != null)
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

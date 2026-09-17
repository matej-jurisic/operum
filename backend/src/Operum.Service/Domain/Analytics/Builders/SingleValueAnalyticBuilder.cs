using Operum.Model.Common;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Fields;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Domain.Analytics;

namespace Operum.Service.Domain.Analytics.Builders
{
    public class SingleValueAnalyticBuilder : AnalyticResultBuilderBase
    {
        public override string SupportedType => AnalyticTypes.SingleValue;

        protected override Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request)
        {
            var result = new SingleValueAnalyticDto
            {
                Name = AnalyticDefinitionList.GetLabel(SupportedType, request.Analytic.Code),
                Description = request.Analytic.Description,
                Id = request.Analytic.Id
            };

            var valueField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Value);
            if (valueField == null)
                return Result.Success<AnalyticDto>(result);

            var allValues = request.Entries
                .SelectMany(e => e.FieldValues.Where(fv => fv.FieldId == valueField.Id))
                .ToList();

            var calculator = SingleValueCalculators.Get(request.Analytic.Code);
            if (calculator == null)
                return Result.Failure(ResultStatusCodes.BadRequest,
                    $"Unsupported analytic code: {request.Analytic.Code}");

            var opResult = calculator.Calculate(allValues);

            if (!opResult.IsSuccess)
                return Result.Failure(opResult.StatusCode, opResult.Messages);

            result.Value = opResult.Data.Value;
            result.EntryId = opResult.Data.EntryId;

            // Min/Max pick an entry, so they can show a different field than the one compared.
            var displayField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Display);
            if (displayField != null && result.EntryId != null)
            {
                var displayValue = request.Entries
                    .FirstOrDefault(e => e.Id == result.EntryId)?.FieldValues
                    .FirstOrDefault(fv => fv.FieldId == displayField.Id)
                    ?.GetValueAsString();

                result.SecondaryValue = result.Value;
                result.SecondaryValueField = MapField(valueField, valueField.Type);

                result.Value = displayValue ?? string.Empty;
                result.ValueField = MapField(displayField, displayField.Type);

                return Result.Success<AnalyticDto>(result);
            }

            // Override type for count-based codes so the frontend doesn't format them as the original field type.
            var syntheticNumberCodes = new HashSet<string>
            {
                AnalyticCodes.Count,
                AnalyticCodes.CountDistinct,
                AnalyticCodes.TrueCount,
                AnalyticCodes.FalseCount,
                AnalyticCodes.TruePercentage,
            };
            var displayType = syntheticNumberCodes.Contains(request.Analytic.Code)
                ? DataTypes.Number
                : valueField.Type;

            result.ValueField = MapField(valueField, displayType);

            return Result.Success<AnalyticDto>(result);
        }

        // Type is passed in separately since it isn't always the field's own (e.g. a count reads as a plain number).
        private static FieldDto MapField(Field field, string type) => new()
        {
            Description = field.Description,
            Id = field.Id,
            Name = field.Name,
            Required = field.Required,
            Type = type,
        };
    }
}

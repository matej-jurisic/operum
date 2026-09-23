using System.Globalization;
using Operum.Model.Common;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics.Builders
{
    // Delegates the calculation to SingleValueAnalyticBuilder, then attaches the target
    // (sourced from Widget.GoalTarget) and the progress ratio.
    public class GoalAnalyticBuilder : AnalyticResultBuilderBase
    {
        private readonly SingleValueAnalyticBuilder _singleValue = new();

        public override string SupportedType => AnalyticTypes.Goal;

        protected override Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request)
        {
            var inner = _singleValue.Build(new AnalyticResultBuilderRequest
            {
                Analytic = new Analytic
                {
                    Id = request.Analytic.Id,
                    Name = request.Analytic.Name,
                    Description = request.Analytic.Description,
                    Code = request.Analytic.Code,
                    ResultType = AnalyticTypes.SingleValue
                },
                Entries = request.Entries,
                FieldMap = request.FieldMap
            });

            if (!inner.IsSuccess)
                return inner;

            if (inner.Data is not SingleValueAnalyticDto single)
                return Result.Failure(ResultStatusCodes.BadRequest, "Goal calculation did not produce a single value.");

            var target = request.Analytic.GoalTarget;
            var direction = GoalDirections.IsValid(request.Analytic.GoalDirection ?? string.Empty)
                ? request.Analytic.GoalDirection!
                : GoalDirections.HigherIsBetter;

            return Result.Success<AnalyticDto>(new GoalAnalyticDto
            {
                Id = request.Analytic.Id,
                Name = AnalyticDefinitionList.GetLabel(SupportedType, request.Analytic.Code),
                Description = request.Analytic.Description,
                Value = single.Value,
                Target = target ?? string.Empty,
                ValueField = single.ValueField,
                Direction = direction,
                Progress = ComputeProgress(single.ValueField?.Type, single.Value, target)
            });
        }

        // Both value and target are reduced to magnitudes (absolute values) so goals
        // expressed with negative numbers (e.g. a sum of negative expense entries) work
        // the same as positive ones. Progress is always value/target: for LowerIsBetter
        // goals (e.g. a spending cap) that reads as "percent of the cap used".
        private static double? ComputeProgress(string? type, string? value, string? target)
        {
            if (!TryParseMagnitude(type, value, out var current) ||
                !TryParseMagnitude(type, target, out var goal) ||
                goal == 0)
                return null;

            return current / goal;
        }

        private static bool TryParseMagnitude(string? type, string? raw, out double magnitude)
        {
            magnitude = 0;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            if (type == DataTypes.TimeSpan)
            {
                if (TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out var ts))
                {
                    magnitude = Math.Abs(ts.TotalSeconds);
                    return true;
                }
                return false;
            }

            if (!double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return false;

            magnitude = Math.Abs(parsed);
            return true;
        }
    }
}

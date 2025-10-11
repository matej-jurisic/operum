using Operum.Model.Common;
using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
using Operum.Service.Domain.Analytics.Calculators;

namespace Operum.Service.Domain.Analytics.Builders
{
    public class SingleValueAnalyticBuilder : AnalyticResultBuilderBase
    {
        private readonly Dictionary<string, ISingleValueCalculator> _calculators;

        public override string SupportedType => AnalyticTypes.SingleValue;

        public SingleValueAnalyticBuilder()
        {
            _calculators = new Dictionary<string, ISingleValueCalculator>
            {
                [AnalyticCodes.Count] = new CountCalculator(),
                [AnalyticCodes.TrueCount] = new TrueCountCalculator(),
                [AnalyticCodes.FalseCount] = new FalseCountCalculator(),
                [AnalyticCodes.TruePercentage] = new TruePercentageCalculator(),
                [AnalyticCodes.Min] = new MinCalculator(),
                [AnalyticCodes.Max] = new MaxCalculator(),
                [AnalyticCodes.Average] = new AverageCalculator(),
                [AnalyticCodes.Sum] = new SumCalculator(),
                [AnalyticCodes.StdDev] = new StdDevCalculator()
            };
        }

        protected override Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request)
        {
            var result = new SingleValueAnalyticDto
            {
                Name = request.Analytic.Code,
                Description = request.Analytic.Description,
                Id = request.Analytic.Id
            };

            var valueField = request.FieldMap.GetValueOrDefault(AnalyticPurposes.Value);
            if (valueField == null)
                return Result.Success<AnalyticDto>(result);

            var allValues = request.Entries
                .SelectMany(e => e.FieldValues.Where(fv => fv.FieldId == valueField.Id))
                .ToList();

            if (!_calculators.TryGetValue(request.Analytic.Code, out var calculator))
                return Result.Failure(ResultStatusCodes.BadRequest,
                    $"Unsupported analytic code: {request.Analytic.Code}");

            var opResult = calculator.Calculate(allValues);

            if (!opResult.IsSuccess)
                return Result.Failure(opResult.StatusCode, opResult.Messages);

            result.Value = opResult.Data.Value;
            result.EntryId = opResult.Data.EntryId;
            result.ValueField = new()
            {
                Description = valueField.Description,
                Id = valueField.Id,
                Name = valueField.Name,
                Required = valueField.Required,
                Type = valueField.Type,
            };

            return Result.Success<AnalyticDto>(result);
        }
    }
}

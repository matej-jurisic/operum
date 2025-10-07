using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Helpers.Analytics
{
    public static class AnalyticsHelpers
    {
        public static Result<AnalyticResultDto> GetAnalyticResult(
           Analytic analytic,
           IEnumerable<Entry> entries)
        {
            var resultType = analytic.ResultType;
            var code = analytic.Code;

            if (!AnalyticDefinitions.IsValidForType(resultType, code))
                return Result.Failure(StatusCodeEnum.BadRequest,
                    $"Code '{code}' not allowed for {resultType}");

            var def = AnalyticDefinitions.ByResultType[resultType];

            var fieldMap = analytic.AnalyticFields.MapByPurpose();

            foreach (var (purpose, field) in fieldMap)
            {
                if (!def.Purposes.Contains(purpose))
                    return Result.Failure(StatusCodeEnum.BadRequest,
                        $"Purpose '{purpose}' not supported for {resultType}");

                var allowedTypes = def.Codes[code].AllowedDataTypes.GetValueOrDefault(purpose);
                if (allowedTypes != null && !allowedTypes.Contains(field.Type))
                    return Result.Failure(StatusCodeEnum.BadRequest,
                        $"Field '{field.Name}' of type '{field.Type}' is not allowed for purpose '{purpose}' in code '{code}'");
            }

            AnalyticResultDto? result = resultType switch
            {
                AnalyticResultTypes.SingleValue =>
                    AnalyticResultGetters.GetSingleValueAnalyticResult(analytic, entries, fieldMap),

                AnalyticResultTypes.NumericChart =>
                    AnalyticResultGetters.GetNumericChartAnalyticResult(analytic, entries, fieldMap),

                AnalyticResultTypes.ScatterPlot =>
                    AnalyticResultGetters.GetScatterPlotAnalyticResult(analytic, entries, fieldMap),

                AnalyticResultTypes.Calendar =>
                    AnalyticResultGetters.GetCalendarAnalyticResult(analytic, entries, fieldMap),

                _ => null
            };

            if (result != null) return Result.Success(result);
            return Result.Failure(StatusCodeEnum.BadRequest);
        }
    }
}

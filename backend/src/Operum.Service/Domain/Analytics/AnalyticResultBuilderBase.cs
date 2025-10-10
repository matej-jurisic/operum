using Operum.Model.Common;
using Operum.Model.Constants.Analytics.Definitions;
using Operum.Model.DTOs.Analytics;
using Operum.Model.Enums;

namespace Operum.Service.Domain.Analytics
{
    public abstract class AnalyticResultBuilderBase : IAnalyticResultBuilder
    {
        public abstract string SupportedType { get; }

        public Result<AnalyticDto> Build(AnalyticResultBuilderRequest request)
        {
            var validationResult = ValidateRequest(request);
            if (!validationResult.IsSuccess)
                return validationResult;

            return BuildResult(request);
        }

        protected virtual Result ValidateRequest(AnalyticResultBuilderRequest request)
        {
            var resultType = request.Analytic.ResultType;
            var code = request.Analytic.Code;

            if (!AnalyticDefinitionList.IsValidForType(resultType, code))
                return Result.Failure(ResultStatus.BadRequest,
                    $"Code '{code}' not allowed for {resultType}");

            var def = AnalyticDefinitionList.ByResultType[resultType];
            var fieldMap = request.FieldMap;

            foreach (var (purpose, field) in fieldMap)
            {
                if (!def.Purposes.Contains(purpose))
                    return Result.Failure(ResultStatus.BadRequest,
                        $"Purpose '{purpose}' not supported for {resultType}");

                var allowedTypes = def.Codes[code].AllowedDataTypes.GetValueOrDefault(purpose);
                if (allowedTypes != null && !allowedTypes.Contains(field.Type))
                    return Result.Failure(ResultStatus.BadRequest,
                        $"Field '{field.Name}' of type '{field.Type}' is not allowed for purpose '{purpose}' in code '{code}'");
            }

            return Result.Success();
        }

        protected abstract Result<AnalyticDto> BuildResult(AnalyticResultBuilderRequest request);
    }
}

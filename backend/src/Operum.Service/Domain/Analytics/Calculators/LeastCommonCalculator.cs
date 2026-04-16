using Operum.Model.Common;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics.Calculators
{
    public class LeastCommonCalculator : ISingleValueCalculator
    {
        public Result<(string Value, string? EntryId)> Calculate(List<FieldValue> fieldValues)
        {
            var leastCommon = fieldValues
                .Select(x => x.GetValueAsString())
                .Where(x => x != null)
                .GroupBy(x => x)
                .OrderBy(g => g.Count())
                .FirstOrDefault();

            if (leastCommon == null)
                return Result.Failure(ResultStatusCodes.NotFound, "No values found.");

            return Result.Success((leastCommon.Key!, (string?)null));
        }
    }
}

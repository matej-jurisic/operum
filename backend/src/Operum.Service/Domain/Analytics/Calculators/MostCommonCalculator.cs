using Operum.Model.Common;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics.Calculators
{
    public class MostCommonCalculator : ISingleValueCalculator
    {
        public Result<(string Value, string? EntryId)> Calculate(List<FieldValue> fieldValues)
        {
            var mostCommon = fieldValues
                .Select(x => x.GetValueAsString())
                .Where(x => x != null)
                .GroupBy(x => x)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (mostCommon == null)
                return Result.Failure(ResultStatusCodes.NotFound, "No values found.");

            return Result.Success((mostCommon.Key!, (string?)null));
        }
    }
}

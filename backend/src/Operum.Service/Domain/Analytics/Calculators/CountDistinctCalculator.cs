using Operum.Model.Common;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics.Calculators
{
    public class CountDistinctCalculator : ISingleValueCalculator
    {
        public Result<(string Value, string? EntryId)> Calculate(List<FieldValue> fieldValues)
        {
            var count = fieldValues
                .Select(x => x.GetValueAsString())
                .Where(x => x != null)
                .Distinct()
                .Count();

            return Result.Success((count.ToString(), (string?)null));
        }
    }
}

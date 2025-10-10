using Operum.Model.Common;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics.Calculators
{
    public class FalseCountCalculator : ISingleValueCalculator
    {
        public Result<(string Value, string? EntryId)> Calculate(List<FieldValue> fieldValues)
        {
            var count = fieldValues.Select(x => x.GetFieldValue()).Count(x => x is bool b && !b);
            return Result.Success((count.ToString(), (string?)null));
        }
    }
}

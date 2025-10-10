using Operum.Model.Common;
using Operum.Model.Converters;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics.Calculators
{
    public class TruePercentageCalculator : ISingleValueCalculator
    {
        public Result<(string Value, string? EntryId)> Calculate(List<FieldValue> fieldValues)
        {
            var all = fieldValues.Select(x => x.GetFieldValue()).Where(x => x is bool).ToList();
            if (all.Count == 0)
                return Result.Failure(ResultStatus.NotFound, "No values found.");

            var percentage = (double)all.Count(x => x is bool b && b) / all.Count * 100;
            return Result.Success((DataFormatters.NumberToString(percentage) + "%", (string?)null));
        }
    }
}

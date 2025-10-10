using Operum.Model.Common;
using Operum.Model.Converters;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics.Calculators
{
    public class SumCalculator : ISingleValueCalculator
    {
        public Result<(string Value, string? EntryId)> Calculate(List<FieldValue> fieldValues)
        {
            var items = fieldValues.Select(x => x.GetFieldValue()).ToList();
            var timeSpans = items.OfType<TimeSpan>().ToList();
            if (timeSpans.Count != 0)
            {
                var sumTicks = timeSpans.Sum(ts => ts.Ticks);
                var total = TimeSpan.FromTicks(sumTicks);
                return Result.Success((DataFormatters.TimeSpanToString(total), (string?)null));
            }

            var numbers = items.OfType<double>().ToList();

            if (numbers.Count == 0)
                return Result.Failure(ResultStatus.NotFound, "No numeric values found.");

            return Result.Success((DataFormatters.NumberToString(numbers.Sum()), (string?)null));
        }
    }
}

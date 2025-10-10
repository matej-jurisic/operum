using Operum.Model.Common;
using Operum.Model.Converters;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics.Calculators
{
    public class StdDevCalculator : ISingleValueCalculator
    {
        public Result<(string Value, string? EntryId)> Calculate(List<FieldValue> fieldValues)
        {
            var items = fieldValues.Select(x => x.GetFieldValue()).ToList();
            var numbers = items.OfType<double>().ToList();

            if (numbers.Count == 0)
                return Result.Failure(ResultStatusCodes.NotFound, "No numeric values found.");

            var avg = numbers.Average();
            var variance = numbers.Sum(x => Math.Pow(x - avg, 2)) / numbers.Count;
            var stdDev = Math.Sqrt(variance);

            return Result.Success((DataFormatters.NumberToString(stdDev), (string?)null));
        }
    }
}

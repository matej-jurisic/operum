using Operum.Model.Common;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics.Calculators
{
    public class MinCalculator : ISingleValueCalculator
    {
        public Result<(string Value, string? EntryId)> Calculate(List<FieldValue> fieldValues)
        {
            FieldValue? minField = null;
            IComparable? minValue = null;

            foreach (var fv in fieldValues)
            {
                if (fv.GetFieldValue() is not IComparable comparable) continue;
                if (minValue == null || comparable.CompareTo(minValue) < 0)
                {
                    minValue = comparable;
                    minField = fv;
                }
            }

            if (minField == null)
                return Result.Failure(ResultStatusCodes.NotFound, "No values found.");

            var stringValue = minField.GetValueAsString();

            if (stringValue == null)
                return Result.Failure(ResultStatusCodes.BadRequest, "Error getting value.");

            return Result.Success((stringValue, (string?)minField.EntryId));
        }
    }
}

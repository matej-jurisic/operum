using Operum.Model.Common;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Domain.Analytics.Calculators
{
    public class MaxCalculator : ISingleValueCalculator
    {
        public Result<(string Value, string? EntryId)> Calculate(List<FieldValue> fieldValues)
        {
            FieldValue? maxField = null;
            IComparable? maxValue = null;

            foreach (var fv in fieldValues)
            {
                if (fv.GetFieldValue() is not IComparable comparable) continue;
                if (maxValue == null || comparable.CompareTo(maxValue) > 0)
                {
                    maxValue = comparable;
                    maxField = fv;
                }
            }

            if (maxField == null)
                return Result.Failure(ResultStatus.NotFound, "No values found.");

            var stringValue = maxField.GetValueAsString();

            if (stringValue == null)
                return Result.Failure(ResultStatus.BadRequest, "Error getting value.");

            return Result.Success((stringValue, (string?)maxField.EntryId));
        }
    }
}

using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.Models;
using System.Globalization;

namespace Operum.Service.Domain.Constants
{
    public static class EntryFilterMatcher
    {
        /// <summary>
        /// Returns true if all filters match the given field values.
        /// fieldValues maps fieldId → FieldValue (with typed properties).
        /// fields maps fieldId → Field (for type lookup).
        /// </summary>
        public static bool Matches(
            IEnumerable<TrackerConstantValueFilter> filters,
            Dictionary<string, FieldValue> fieldValues,
            Dictionary<string, Field> fieldsById)
        {
            foreach (var filter in filters)
            {
                if (!fieldValues.TryGetValue(filter.FieldId, out var fv))
                    return false;
                if (!fieldsById.TryGetValue(filter.FieldId, out var field))
                    return false;
                if (!MatchesFilter(field.Type.ToLowerInvariant(), fv, filter.Operator, filter.Value))
                    return false;
            }
            return true;
        }

        private static bool MatchesFilter(string fieldType, FieldValue fv, string operatorType, string? filterValue)
        {
            return fieldType switch
            {
                DataTypes.String => MatchesString(fv.StringValue, operatorType, filterValue),
                DataTypes.Number => MatchesNumber(fv.NumberValue, operatorType, filterValue),
                DataTypes.Date or DataTypes.DateTime => MatchesDateTime(fv.DateTimeValue, operatorType, filterValue),
                DataTypes.TimeSpan => MatchesTimeSpan(fv.TimeSpanValue, operatorType, filterValue),
                DataTypes.Bool => MatchesBool(fv.BooleanValue, operatorType, filterValue),
                _ => false
            };
        }

        private static bool MatchesString(string? fieldValue, string operatorType, string? filterValue)
        {
            if (filterValue != null)
            {
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => fieldValue == filterValue,
                    OperatorTypes.NotEquals => fieldValue != filterValue,
                    OperatorTypes.Contains => fieldValue != null && fieldValue.Contains(filterValue),
                    OperatorTypes.StartsWith => fieldValue != null && fieldValue.StartsWith(filterValue),
                    OperatorTypes.EndsWith => fieldValue != null && fieldValue.EndsWith(filterValue),
                    _ => false
                };
            }
            else
            {
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => fieldValue == null,
                    OperatorTypes.NotEquals => fieldValue != null,
                    _ => false
                };
            }
        }

        private static bool MatchesNumber(double? fieldValue, string operatorType, string? filterValue)
        {
            if (filterValue != null)
            {
                if (!double.TryParse(filterValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var filterNum))
                    return false;
                if (fieldValue == null)
                    return operatorType == OperatorTypes.NotEquals;
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => fieldValue == filterNum,
                    OperatorTypes.NotEquals => fieldValue != filterNum,
                    OperatorTypes.GreaterThan => fieldValue > filterNum,
                    OperatorTypes.GreaterThanOrEqual => fieldValue >= filterNum,
                    OperatorTypes.LessThan => fieldValue < filterNum,
                    OperatorTypes.LessThanOrEqual => fieldValue <= filterNum,
                    _ => false
                };
            }
            else
            {
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => fieldValue == null,
                    OperatorTypes.NotEquals => fieldValue != null,
                    _ => false
                };
            }
        }

        private static bool MatchesDateTime(DateTime? fieldValue, string operatorType, string? filterValue)
        {
            if (filterValue != null)
            {
                var resolved = DynamicDateTokens.Resolve(filterValue);
                DateTime utcFilter;
                if (resolved.HasValue)
                {
                    utcFilter = resolved.Value;
                }
                else
                {
                    if (!DateTime.TryParse(filterValue, null, DateTimeStyles.RoundtripKind, out var parsed))
                        return false;
                    utcFilter = parsed.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                        : parsed.ToUniversalTime();
                }

                if (fieldValue == null)
                    return operatorType == OperatorTypes.NotEquals;

                var utcField = fieldValue.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(fieldValue.Value, DateTimeKind.Utc)
                    : fieldValue.Value.ToUniversalTime();

                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => utcField.Date == utcFilter.Date,
                    OperatorTypes.NotEquals => utcField.Date != utcFilter.Date,
                    OperatorTypes.GreaterThan => utcField > utcFilter,
                    OperatorTypes.GreaterThanOrEqual => utcField >= utcFilter,
                    OperatorTypes.LessThan => utcField < utcFilter,
                    OperatorTypes.LessThanOrEqual => utcField <= utcFilter,
                    _ => false
                };
            }
            else
            {
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => fieldValue == null,
                    OperatorTypes.NotEquals => fieldValue != null,
                    _ => false
                };
            }
        }

        private static bool MatchesTimeSpan(TimeSpan? fieldValue, string operatorType, string? filterValue)
        {
            if (filterValue != null)
            {
                if (!TimeSpan.TryParse(filterValue, out var filterTs))
                    return false;
                if (fieldValue == null)
                    return operatorType == OperatorTypes.NotEquals;
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => fieldValue == filterTs,
                    OperatorTypes.NotEquals => fieldValue != filterTs,
                    OperatorTypes.GreaterThan => fieldValue > filterTs,
                    OperatorTypes.GreaterThanOrEqual => fieldValue >= filterTs,
                    OperatorTypes.LessThan => fieldValue < filterTs,
                    OperatorTypes.LessThanOrEqual => fieldValue <= filterTs,
                    _ => false
                };
            }
            else
            {
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => fieldValue == null,
                    OperatorTypes.NotEquals => fieldValue != null,
                    _ => false
                };
            }
        }

        private static bool MatchesBool(bool? fieldValue, string operatorType, string? filterValue)
        {
            var filterStr = filterValue ?? "false";
            if (!bool.TryParse(filterStr, out var filterBool))
                return false;
            if (fieldValue == null)
                return false;
            return operatorType switch
            {
                OperatorTypes.EqualsOperator => fieldValue == filterBool,
                OperatorTypes.NotEquals => fieldValue != filterBool,
                _ => false
            };
        }
    }
}

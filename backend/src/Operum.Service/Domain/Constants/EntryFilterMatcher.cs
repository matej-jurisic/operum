using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.Extensions;
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
            Dictionary<string, Field> fieldsById,
            TimeZoneInfo tz)
        {
            foreach (var filter in filters)
            {
                if (!fieldsById.TryGetValue(filter.FieldId, out var field))
                    return false;

                // A missing row (field added after this entry, its value cleared, an import
                // that never mapped it) isn't a mismatch by itself: MatchesFilter treats it the
                // same as a row holding null, so "not equals" can still match it, the same as
                // ViewQueryBuilder's view filters do.
                fieldValues.TryGetValue(filter.FieldId, out var fv);
                if (!MatchesFilter(field.Type.ToLowerInvariant(), fv, filter.Operator, filter.Value, tz))
                    return false;
            }
            return true;
        }

        private static bool MatchesFilter(string fieldType, FieldValue? fv, string operatorType, string? filterValue, TimeZoneInfo tz)
        {
            return fieldType switch
            {
                DataTypes.String => MatchesString(fv?.StringValue, operatorType, filterValue),
                DataTypes.Number => MatchesNumber(fv?.NumberValue, operatorType, filterValue),
                DataTypes.Date or DataTypes.DateTime => MatchesDateTime(fv?.DateTimeValue, operatorType, filterValue, tz),
                DataTypes.TimeSpan => MatchesTimeSpan(fv?.TimeSpanValue, operatorType, filterValue),
                DataTypes.Bool => MatchesBool(fv?.BooleanValue, operatorType, filterValue),
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

        private static bool MatchesDateTime(DateTime? fieldValue, string operatorType, string? filterValue, TimeZoneInfo tz)
        {
            if (filterValue != null)
            {
                var resolved = DynamicDateTokens.Resolve(filterValue, tz);
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

                // Equality on a date means "the same day the user sees on a calendar", which is a
                // window in UTC terms rather than a single instant.
                var (dayStart, dayEnd) = TimeZoneResolver.LocalDayWindow(utcFilter, tz);

                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => utcField >= dayStart && utcField < dayEnd,
                    OperatorTypes.NotEquals => utcField < dayStart || utcField >= dayEnd,
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
                if (!TimeSpan.TryParse(filterValue, CultureInfo.InvariantCulture, out var filterTs))
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
            // A missing/null value isn't filterBool either, so "not equals" matches it, the
            // same as the other field types above.
            if (fieldValue == null)
                return operatorType == OperatorTypes.NotEquals;
            return operatorType switch
            {
                OperatorTypes.EqualsOperator => fieldValue == filterBool,
                OperatorTypes.NotEquals => fieldValue != filterBool,
                _ => false
            };
        }
    }
}

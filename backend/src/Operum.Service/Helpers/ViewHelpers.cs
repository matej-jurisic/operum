using Operum.Model.Constants;
using Operum.Model.Models;

namespace Operum.Service.Helpers
{
    public static class ViewHelpers
    {
        public static IQueryable<Entry> ApplyViewSorting(IQueryable<Entry> query, List<ViewSort> sorts)
        {
            if (sorts.Count == 0)
                return query.OrderByDescending(x => x.CreatedAt);

            IOrderedQueryable<Entry>? orderedQuery = null;

            foreach (var sort in sorts.OrderBy(s => s.Order))
            {
                var fieldId = sort.FieldId;
                var descending = sort.Descending;
                var fieldType = sort.Field.Type.ToLowerInvariant();

                if (orderedQuery == null)
                {
                    orderedQuery = fieldType switch
                    {
                        DataTypes.String => descending
                            ? query.OrderByDescending(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.StringValue)
                            : query.OrderBy(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.StringValue),

                        DataTypes.Number => descending
                            ? query.OrderByDescending(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.NumberValue)
                            : query.OrderBy(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.NumberValue),

                        DataTypes.Date or DataTypes.DateTime => descending
                            ? query.OrderByDescending(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.DateTimeValue)
                            : query.OrderBy(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.DateTimeValue),

                        DataTypes.TimeSpan => descending
                            ? query.OrderByDescending(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.TimeSpanValue)
                            : query.OrderBy(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.TimeSpanValue),

                        DataTypes.Bool => descending
                            ? query.OrderByDescending(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.BooleanValue)
                            : query.OrderBy(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.BooleanValue),

                        _ => descending
                            ? query.OrderByDescending(x => x.CreatedAt)
                            : query.OrderBy(x => x.CreatedAt)
                    };
                }
                else
                {
                    orderedQuery = fieldType switch
                    {
                        DataTypes.String => descending
                            ? orderedQuery.ThenByDescending(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.StringValue)
                            : orderedQuery.ThenBy(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.StringValue),

                        DataTypes.Number => descending
                            ? orderedQuery.ThenByDescending(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.NumberValue)
                            : orderedQuery.ThenBy(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.NumberValue),

                        DataTypes.Date or DataTypes.DateTime => descending
                            ? orderedQuery.ThenByDescending(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.DateTimeValue)
                            : orderedQuery.ThenBy(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.DateTimeValue),

                        DataTypes.TimeSpan => descending
                            ? orderedQuery.ThenByDescending(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.TimeSpanValue)
                            : orderedQuery.ThenBy(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.TimeSpanValue),

                        DataTypes.Bool => descending
                            ? orderedQuery.ThenByDescending(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.BooleanValue)
                            : orderedQuery.ThenBy(e => e.FieldValues.FirstOrDefault(fv => fv.FieldId == fieldId)!.BooleanValue),

                        _ => descending
                            ? orderedQuery.ThenByDescending(x => x.CreatedAt)
                            : orderedQuery.ThenBy(x => x.CreatedAt)
                    };
                }
            }

            return orderedQuery ?? query.OrderByDescending(x => x.CreatedAt);
        }

        public static IQueryable<Entry> ApplyViewFilters(IQueryable<Entry> query, List<ViewFilter> filters)
        {
            if (filters.Count == 0)
                return query;

            foreach (var filter in filters)
            {
                var fieldId = filter.FieldId;
                var operatorType = filter.Operator;
                var value = filter.Value;
                var fieldType = filter.Field.Type.ToLowerInvariant();

                query = fieldType switch
                {
                    DataTypes.Number => ApplyNumberFilter(query, fieldId, operatorType, value),
                    DataTypes.String => ApplyStringFilter(query, fieldId, operatorType, value),
                    DataTypes.Date or DataTypes.DateTime => ApplyDateTimeFilter(query, fieldId, operatorType, value),
                    DataTypes.TimeSpan => ApplyTimeSpanFilter(query, fieldId, operatorType, value),
                    DataTypes.Bool => ApplyBooleanFilter(query, fieldId, operatorType, value),
                    _ => query
                };
            }

            return query;
        }

        private static IQueryable<Entry> ApplyStringFilter(IQueryable<Entry> query, string fieldId, string operatorType, string? value)
        {
            if (value != null)
            {
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue == value)),
                    OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue != value)),
                    OperatorTypes.Contains => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue != null && fv.StringValue.Contains(value))),
                    OperatorTypes.StartsWith => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue != null && fv.StringValue.StartsWith(value))),
                    OperatorTypes.EndsWith => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue != null && fv.StringValue.EndsWith(value))),
                    _ => query
                };
            }
            else
            {
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue == null)),
                    OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue != null)),
                    _ => query
                };
            }
        }

        private static IQueryable<Entry> ApplyNumberFilter(IQueryable<Entry> query, string fieldId, string operatorType, string? value)
        {
            if (value != null)
            {
                if (!double.TryParse(value, out var numericValue))
                    return query;

                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue == numericValue)),
                    OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue != numericValue)),
                    OperatorTypes.GreaterThan => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue > numericValue)),
                    OperatorTypes.GreaterThanOrEqual => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue >= numericValue)),
                    OperatorTypes.LessThan => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue < numericValue)),
                    OperatorTypes.LessThanOrEqual => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue <= numericValue)),
                    _ => query
                };
            }
            else
            {
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue == null)),
                    OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue != null)),
                    _ => query
                };
            }
        }

        private static IQueryable<Entry> ApplyDateTimeFilter(IQueryable<Entry> query, string fieldId, string operatorType, string? value)
        {
            if (value != null)
            {
                if (!DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dateValue))
                    return query;

                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue != null && fv.DateTimeValue.Value.Date == dateValue.Date)),
                    OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && (fv.DateTimeValue == null || fv.DateTimeValue.Value.Date != dateValue.Date))),
                    OperatorTypes.GreaterThan => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue > dateValue)),
                    OperatorTypes.GreaterThanOrEqual => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue >= dateValue)),
                    OperatorTypes.LessThan => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue < dateValue)),
                    OperatorTypes.LessThanOrEqual => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue <= dateValue)),
                    _ => query
                };
            }
            else
            {
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue == null)),
                    OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue != null)),
                    _ => query
                };
            }
        }

        private static IQueryable<Entry> ApplyTimeSpanFilter(IQueryable<Entry> query, string fieldId, string operatorType, string? value)
        {
            if (value != null)
            {
                if (!TimeSpan.TryParse(value, out var timeSpanValue))
                    return query;

                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.TimeSpanValue == timeSpanValue)),
                    OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.TimeSpanValue != timeSpanValue)),
                    OperatorTypes.GreaterThan => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.TimeSpanValue > timeSpanValue)),
                    OperatorTypes.GreaterThanOrEqual => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.TimeSpanValue >= timeSpanValue)),
                    OperatorTypes.LessThan => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.TimeSpanValue < timeSpanValue)),
                    OperatorTypes.LessThanOrEqual => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.TimeSpanValue <= timeSpanValue)),
                    _ => query
                };
            }
            else
            {
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.TimeSpanValue == null)),
                    OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.TimeSpanValue != null)),
                    _ => query
                };
            }
        }

        private static IQueryable<Entry> ApplyBooleanFilter(IQueryable<Entry> query, string fieldId, string operatorType, string? value)
        {
            if (value == null) value = "false";
            if (!bool.TryParse(value, out var boolValue))
                return query;

            return operatorType switch
            {
                OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.BooleanValue == boolValue)),
                OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.BooleanValue != boolValue)),
                _ => query
            };
        }

        public static bool IsValidFieldValue(string value, string fieldType)
        {
            try
            {
                return fieldType.ToLowerInvariant() switch
                {
                    DataTypes.String => true,
                    DataTypes.Number => double.TryParse(value, out _),
                    DataTypes.Date or DataTypes.DateTime => DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out _),
                    DataTypes.TimeSpan => TimeSpan.TryParse(value, out _),
                    DataTypes.Bool => bool.TryParse(value, out _),
                    _ => false,
                };
            }
            catch
            {
                return false;
            }
        }

        // Helper method to validate filter operators for each field type
        public static bool IsValidOperatorForFieldType(string operatorType, string fieldType)
        {
            // First check if it's a valid operator at all
            if (!OperatorTypes.IsValid(operatorType))
                return false;

            var lowerFieldType = fieldType.ToLowerInvariant();

            return lowerFieldType switch
            {
                DataTypes.String => operatorType is OperatorTypes.EqualsOperator or OperatorTypes.NotEquals
                    or OperatorTypes.Contains or OperatorTypes.StartsWith or OperatorTypes.EndsWith,

                DataTypes.Number => operatorType is OperatorTypes.EqualsOperator or OperatorTypes.NotEquals
                    or OperatorTypes.GreaterThan or OperatorTypes.GreaterThanOrEqual
                    or OperatorTypes.LessThan or OperatorTypes.LessThanOrEqual,

                DataTypes.Date or DataTypes.DateTime => operatorType is OperatorTypes.EqualsOperator
                    or OperatorTypes.NotEquals or OperatorTypes.GreaterThan or OperatorTypes.GreaterThanOrEqual
                    or OperatorTypes.LessThan or OperatorTypes.LessThanOrEqual,

                DataTypes.TimeSpan => operatorType is OperatorTypes.EqualsOperator or OperatorTypes.NotEquals
                    or OperatorTypes.GreaterThan or OperatorTypes.GreaterThanOrEqual
                    or OperatorTypes.LessThan or OperatorTypes.LessThanOrEqual,

                DataTypes.Bool => operatorType is OperatorTypes.EqualsOperator or OperatorTypes.NotEquals,

                _ => false
            };
        }
    }
}
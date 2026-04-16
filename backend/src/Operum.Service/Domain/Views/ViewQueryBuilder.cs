using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.Models;


namespace Operum.Service.Domain.Views
{
    public static class ViewQueryBuilder
    {
        /// <summary>
        /// Merges sorts from multiple views using first-view-wins: if two views sort
        /// the same field, the first view's sort takes priority and the later one is skipped.
        /// </summary>
        public static List<ViewSort> MergeViewSorts(List<View> views)
        {
            var seenFieldIds = new HashSet<string>();
            var merged = new List<ViewSort>();

            foreach (var view in views)
            {
                foreach (var sort in view.Sorts.OrderBy(s => s.Order))
                {
                    if (seenFieldIds.Add(sort.FieldId))
                        merged.Add(sort);
                }
            }

            return merged;
        }

        /// <summary>
        /// Merges filters from multiple views by ANDing them all together.
        /// </summary>
        public static List<ViewFilter> MergeViewFilters(List<View> views)
        {
            return views.SelectMany(v => v.Filters).ToList();
        }

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
                // Resolve dynamic tokens (e.g. "today", "start_of_month") to concrete UTC DateTimes
                var resolved = DynamicDateTokens.Resolve(value);
                DateTime utcDateValue;
                if (resolved.HasValue)
                {
                    utcDateValue = resolved.Value;
                }
                else
                {
                    if (!DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dateValue))
                        return query;
                    utcDateValue = dateValue.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dateValue, DateTimeKind.Utc)
                        : dateValue.ToUniversalTime();
                }

                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue != null && fv.DateTimeValue.Value.Date == utcDateValue.Date)),
                    OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && (fv.DateTimeValue == null || fv.DateTimeValue.Value.Date != utcDateValue.Date))),
                    OperatorTypes.GreaterThan => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue > utcDateValue)),
                    OperatorTypes.GreaterThanOrEqual => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue >= utcDateValue)),
                    OperatorTypes.LessThan => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue < utcDateValue)),
                    OperatorTypes.LessThanOrEqual => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue <= utcDateValue)),
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
    }
}

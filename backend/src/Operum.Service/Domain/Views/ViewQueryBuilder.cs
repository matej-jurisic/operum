using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.Extensions;
using Operum.Model.Models;
using System.Globalization;


namespace Operum.Service.Domain.Views
{
    public static class ViewQueryBuilder
    {
        /// <summary>
        /// Picks a view's sort queries out, in view order, using first-query-wins: if two
        /// of them sort the same field, the earlier one takes priority and the later one
        /// is skipped.
        /// </summary>
        public static List<Query> ResolveSorts(View view)
        {
            var seenFieldIds = new HashSet<string>();
            var merged = new List<Query>();

            foreach (var viewQuery in view.ViewQueries.OrderBy(vq => vq.Order))
            {
                var query = viewQuery.Query;
                if (query.Kind != QueryKinds.Sort)
                    continue;

                if (seenFieldIds.Add(query.FieldId))
                    merged.Add(query);
            }

            return merged;
        }

        /// <summary>
        /// Picks a view's filter queries out, ANDing them all together.
        /// </summary>
        public static List<Query> ResolveFilters(View view)
        {
            return view.ViewQueries
                .OrderBy(vq => vq.Order)
                .Select(vq => vq.Query)
                .Where(q => q.Kind == QueryKinds.Filter)
                .ToList();
        }

        // A view's columns have no resolver of their own: they are stored on the view
        // already deduped and in order (ViewColumn), and are deliberately never applied to
        // the entries query. Columns are the last step, decided by whatever renders the
        // entries, so a filter or a sort over a hidden field keeps working exactly as it did.

        public static IQueryable<Entry> ApplyViewSorting(IQueryable<Entry> query, List<Query> sorts)
        {
            if (sorts.Count == 0)
                return query.OrderByDescending(x => x.CreatedAt);

            IOrderedQueryable<Entry>? orderedQuery = null;

            // The list is already in the order the view puts its sorts in.
            foreach (var sort in sorts)
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

        public static IQueryable<Entry> ApplyViewFilters(IQueryable<Entry> query, List<Query> filters, TimeZoneInfo tz)
        {
            if (filters.Count == 0)
                return query;

            foreach (var filter in filters)
            {
                var fieldId = filter.FieldId;
                var operatorType = filter.Operator ?? string.Empty;
                var value = filter.Value;
                var fieldType = filter.Field.Type.ToLowerInvariant();

                query = fieldType switch
                {
                    DataTypes.Number => ApplyNumberFilter(query, fieldId, operatorType, value),
                    DataTypes.String => ApplyStringFilter(query, fieldId, operatorType, value),
                    DataTypes.Date or DataTypes.DateTime => ApplyDateTimeFilter(query, fieldId, operatorType, value, tz),
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
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numericValue))
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

        private static IQueryable<Entry> ApplyDateTimeFilter(IQueryable<Entry> query, string fieldId, string operatorType, string? value, TimeZoneInfo tz)
        {
            if (value != null)
            {
                // Resolve dynamic tokens (e.g. "today", "start_of_month:-1") to concrete UTC DateTimes
                var resolved = DynamicDateTokens.Resolve(value, tz);
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

                // Equality on a date means "the same day the user sees on a calendar", which is a
                // window in UTC terms rather than a single instant.
                var (dayStart, dayEnd) = TimeZoneResolver.LocalDayWindow(utcDateValue, tz);

                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue >= dayStart && fv.DateTimeValue < dayEnd)),
                    OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && (fv.DateTimeValue == null || fv.DateTimeValue < dayStart || fv.DateTimeValue >= dayEnd))),
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
                if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var timeSpanValue))
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

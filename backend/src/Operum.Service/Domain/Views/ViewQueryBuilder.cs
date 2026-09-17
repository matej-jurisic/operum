using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.Extensions;
using Operum.Model.Models;
using System.Globalization;


namespace Operum.Service.Domain.Views
{
    // One clause resolved to the concrete field it runs against; the currency ApplyViewFilters / ApplyViewSorting speak.
    public readonly record struct ResolvedClause(
        string FieldId,
        string FieldType,
        string? Operator,
        string? Value,
        bool Descending);

    public static class ViewQueryBuilder
    {
        // First-query-wins: if two sorts target the same field, the earlier one wins and the later is skipped.
        public static List<ResolvedClause> ResolveSorts(View view)
        {
            var seenFieldIds = new HashSet<string>();
            var merged = new List<ResolvedClause>();

            foreach (var viewQuery in view.ViewQueries.OrderBy(vq => vq.Order))
            {
                var query = viewQuery.Query;
                if (query.Kind != QueryKinds.Sort)
                    continue;

                if (seenFieldIds.Add(viewQuery.FieldId))
                    merged.Add(new ResolvedClause(viewQuery.FieldId, viewQuery.Field.Type, null, null, query.Descending));
            }

            return merged;
        }

        // ANDs all of the view's filter queries together.
        public static List<ResolvedClause> ResolveFilters(View view)
        {
            return view.ViewQueries
                .OrderBy(vq => vq.Order)
                .Where(vq => vq.Query.Kind == QueryKinds.Filter)
                .Select(vq => new ResolvedClause(vq.FieldId, vq.Field.Type, vq.Query.Operator, vq.Query.Value, false))
                .ToList();
        }

        // A view's columns (ViewColumn) are deliberately never applied to the entries query;
        // a filter/sort over a hidden field must keep working.
        public static IQueryable<Entry> ApplyViewSorting(IQueryable<Entry> query, List<ResolvedClause> sorts)
        {
            if (sorts.Count == 0)
                return query.OrderByDescending(x => x.CreatedAt);

            IOrderedQueryable<Entry>? orderedQuery = null;

            foreach (var sort in sorts)
            {
                var fieldId = sort.FieldId;
                var descending = sort.Descending;
                var fieldType = sort.FieldType.ToLowerInvariant();

                if (orderedQuery == null)
                {
                    orderedQuery = fieldType switch
                    {
                        DataTypes.String or DataTypes.Reference => descending
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
                        DataTypes.String or DataTypes.Reference => descending
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

        public static IQueryable<Entry> ApplyViewFilters(IQueryable<Entry> query, List<ResolvedClause> filters, TimeZoneInfo tz)
        {
            if (filters.Count == 0)
                return query;

            foreach (var filter in filters)
            {
                var fieldId = filter.FieldId;
                var operatorType = filter.Operator ?? string.Empty;
                var value = filter.Value;
                var fieldType = filter.FieldType.ToLowerInvariant();

                query = fieldType switch
                {
                    DataTypes.Number => ApplyNumberFilter(query, fieldId, operatorType, value),
                    // Reference matches on the cached link label in StringValue.
                    DataTypes.String or DataTypes.Reference => ApplyStringFilter(query, fieldId, operatorType, value),
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
                    // A missing row also matches NotEquals.
                    OperatorTypes.NotEquals => query.Where(e => !e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue == value)),
                    OperatorTypes.Contains => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue != null && fv.StringValue.Contains(value))),
                    OperatorTypes.StartsWith => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue != null && fv.StringValue.StartsWith(value))),
                    OperatorTypes.EndsWith => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue != null && fv.StringValue.EndsWith(value))),
                    _ => query
                };
            }
            else
            {
                // "is empty" must also catch entries with no row for this field at all, not just a null value.
                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => !e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.StringValue != null)),
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
                    // A missing row also matches NotEquals.
                    OperatorTypes.NotEquals => query.Where(e => !e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue == numericValue)),
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
                    OperatorTypes.EqualsOperator => query.Where(e => !e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue != null)),
                    OperatorTypes.NotEquals => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.NumberValue != null)),
                    _ => query
                };
            }
        }

        private static IQueryable<Entry> ApplyDateTimeFilter(IQueryable<Entry> query, string fieldId, string operatorType, string? value, TimeZoneInfo tz)
        {
            if (value != null)
            {
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

                // Equality on a date means the same calendar day, a UTC window rather than a single instant.
                var (dayStart, dayEnd) = TimeZoneResolver.LocalDayWindow(utcDateValue, tz);

                return operatorType switch
                {
                    OperatorTypes.EqualsOperator => query.Where(e => e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue >= dayStart && fv.DateTimeValue < dayEnd)),
                    // A missing row also matches NotEquals.
                    OperatorTypes.NotEquals => query.Where(e => !e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue >= dayStart && fv.DateTimeValue < dayEnd)),
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
                    OperatorTypes.EqualsOperator => query.Where(e => !e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.DateTimeValue != null)),
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
                    // A missing row also matches NotEquals.
                    OperatorTypes.NotEquals => query.Where(e => !e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.TimeSpanValue == timeSpanValue)),
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
                    OperatorTypes.EqualsOperator => query.Where(e => !e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.TimeSpanValue != null)),
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
                // A missing row also matches NotEquals.
                OperatorTypes.NotEquals => query.Where(e => !e.FieldValues.Any(fv => fv.FieldId == fieldId && fv.BooleanValue == boolValue)),
                _ => query
            };
        }
    }
}

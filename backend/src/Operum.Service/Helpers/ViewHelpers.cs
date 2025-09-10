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
    }
}

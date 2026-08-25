using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Enums;
using Operum.Model.DTOs.Queries.Requests;
using Operum.Model.Models;
using Operum.Service.Domain.Views;

namespace Operum.Service.Domain.Queries
{
    // Shared logic for validating and building a Query (a single filter or sort clause)
    // from a request DTO, used both by QueriesService (standalone Query CRUD) and
    // ViewsService (ad-hoc Query creation while authoring a View).
    public static class QueryBuilder
    {
        public static async Task<Result> ValidateClause(OperumContext db, string trackerId, string kind, string fieldId, string? op, string? value)
        {
            var field = await db.Fields.FindAsync(fieldId);
            if (field == null || field.TrackerId != trackerId)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound(
                    kind == QueryKinds.Sort ? "sort field" : "filter field"));

            // A sort is just the field and a direction, so there is nothing else to check.
            if (kind != QueryKinds.Filter)
                return Result.Success();

            if (!ViewFilterValidator.IsValidOperatorForFieldType(op!, field.Type))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"operator '{op}' for field type '{field.Type}'"));

            if (value != null && !ViewFilterValidator.IsValidFieldValue(value, field.Type))
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"value '{value}' for field type '{field.Type}'"));

            return Result.Success();
        }

        public static Task<Result> ValidateClause(OperumContext db, string trackerId, CreateQueryDto dto) =>
            ValidateClause(db, trackerId, dto.Kind, dto.FieldId, dto.Operator, dto.Value);

        public static Query BuildQueryEntity(string trackerId, CreateQueryDto dto)
        {
            var query = new Query { TrackerId = trackerId };
            ApplyClause(query, dto.Kind, dto.FieldId, dto.Operator, dto.Value, dto.Descending);
            return query;
        }

        // Whatever the kind doesn't use is blanked rather than kept, so a query that used
        // to be a filter never drags a stale operator along as a sort.
        public static void ApplyClause(Query query, string kind, string fieldId, string? op, string? value, bool descending)
        {
            query.Kind = kind;
            query.FieldId = fieldId;
            query.Operator = kind == QueryKinds.Filter ? op : null;
            query.Value = kind == QueryKinds.Filter ? value : null;
            query.Descending = kind == QueryKinds.Sort && descending;
        }
    }
}

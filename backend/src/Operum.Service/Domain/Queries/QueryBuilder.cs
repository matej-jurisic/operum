using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Enums;
using Operum.Model.DTOs.Queries.Requests;
using Operum.Model.Models;
using Operum.Service.Domain.Views;
using Microsoft.EntityFrameworkCore;

namespace Operum.Service.Domain.Queries
{
    // Shared logic for validating and building a Query (and its Filters/Sorts) from a
    // request DTO, used both by QueriesService (standalone Query CRUD) and ViewsService
    // (ad-hoc Query creation while authoring a View).
    public static class QueryBuilder
    {
        public static async Task<Result> ValidateSortsAndFilters(
            OperumContext db,
            string trackerId,
            List<CreateQuerySortDto> sorts,
            List<CreateQueryFilterDto> filters)
        {
            foreach (var sort in sorts)
            {
                var field = await db.Fields.FindAsync(sort.FieldId);
                if (field == null || field.TrackerId != trackerId)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("sort field"));
            }

            foreach (var filter in filters)
            {
                var field = await db.Fields.FindAsync(filter.FieldId);
                if (field == null || field.TrackerId != trackerId)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("filter field"));

                if (!ViewFilterValidator.IsValidOperatorForFieldType(filter.Operator, field.Type))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"operator '{filter.Operator}' for field type '{field.Type}'"));

                if (filter.Value != null && !ViewFilterValidator.IsValidFieldValue(filter.Value, field.Type))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"value '{filter.Value}' for field type '{field.Type}'"));
            }

            return Result.Success();
        }

        public static Query BuildQueryEntity(string trackerId, CreateQueryDto dto)
        {
            return new Query
            {
                TrackerId = trackerId,
                Name = dto.Name,
                Description = dto.Description,
                Sorts = dto.Sorts.Select((s, i) => new QuerySort
                {
                    FieldId = s.FieldId,
                    Descending = s.Descending,
                    Order = i,
                }).ToList(),
                Filters = dto.Filters.Select(f => new QueryFilter
                {
                    FieldId = f.FieldId,
                    Operator = f.Operator,
                    Value = f.Value,
                }).ToList(),
            };
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Enums;
using Operum.Model.DTOs.Queries;
using Operum.Model.DTOs.Queries.Requests;
using Operum.Model.Models;
using Operum.Service.Domain.Queries;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Queries
{
    public class QueriesService(ICurrentUserService currentUserService, OperumContext db, IMapper mapper) : IQueriesService
    {
        public async Task<Result<QueryDto>> CreateQuery(string trackerId, CreateQueryDto query)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);
            var isOwner = tracker?.OwnerId == user.Id;
            var userTracker = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (tracker == null || (!isOwner && userTracker?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            var queryCount = await db.Queries.CountAsync(q => q.TrackerId == trackerId);
            if (queryCount >= DataLimits.MaxQueryCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("queries", DataLimits.MaxQueryCount));

            var validation = await QueryBuilder.ValidateSortsAndFilters(db, trackerId, query.Sorts, query.Filters);
            if (validation.IsFailure)
                return Result.Failure(validation.StatusCode, validation.Messages);

            var newQuery = QueryBuilder.BuildQueryEntity(trackerId, query);

            await db.Queries.AddAsync(newQuery);
            await db.SaveChangesAsync();
            var created = await GetQuery(trackerId, newQuery.Id);
            return Result.Success(created.Data);
        }

        public async Task<Result<QueryDto>> UpdateQuery(string trackerId, string queryId, UpdateQueryDto query)
        {
            var user = currentUserService.GetCurrentUser();
            var existingQuery = await db.Queries
                .Include(x => x.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(x => x.Id == queryId && x.TrackerId == trackerId);

            var isOwnerQuery = existingQuery?.Tracker.OwnerId == user.Id;
            var utQuery = existingQuery?.Tracker.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (existingQuery == null || (!isOwnerQuery && utQuery?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            var validation = await QueryBuilder.ValidateSortsAndFilters(db, trackerId, query.Sorts, query.Filters);
            if (validation.IsFailure)
                return Result.Failure(validation.StatusCode, validation.Messages);

            existingQuery.Name = query.Name;
            existingQuery.Description = query.Description;

            await db.QuerySorts.Where(x => x.QueryId == queryId).ExecuteDeleteAsync();
            await db.QueryFilters.Where(x => x.QueryId == queryId).ExecuteDeleteAsync();

            var newSorts = query.Sorts.Select((s, i) => new QuerySort
            {
                QueryId = queryId,
                FieldId = s.FieldId,
                Descending = s.Descending,
                Order = i,
            }).ToList();

            var newFilters = query.Filters.Select(f => new QueryFilter
            {
                QueryId = queryId,
                FieldId = f.FieldId,
                Operator = f.Operator,
                Value = f.Value,
            }).ToList();

            await db.QuerySorts.AddRangeAsync(newSorts);
            await db.QueryFilters.AddRangeAsync(newFilters);
            db.Queries.Update(existingQuery);
            await db.SaveChangesAsync();

            var updated = await GetQuery(trackerId, queryId);
            return Result.Success(updated.Data);
        }

        public async Task<Result> DeleteQuery(string trackerId, string queryId)
        {
            var user = currentUserService.GetCurrentUser();

            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);
            var isOwnerDel = tracker?.OwnerId == user.Id;
            var utDel = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (tracker == null || (!isOwnerDel && utDel?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            // DB-level cascade (ViewQuery -> Query) drops this query from any views that
            // reference it; those views keep their remaining queries.
            await db.Queries.Where(x => x.Id == queryId && x.TrackerId == trackerId).ExecuteDeleteAsync();

            return Result.Success();
        }

        public async Task<Result<QueryDto>> GetQuery(string trackerId, string queryId)
        {
            var user = currentUserService.GetCurrentUser();

            var query = await db.Queries
                .Include(x => x.Tracker)
                    .ThenInclude(x => x.ApplicationUserTrackers)
                .Include(x => x.Sorts.OrderBy(s => s.Order))
                    .ThenInclude(x => x.Field)
                .Include(x => x.Filters)
                    .ThenInclude(x => x.Field)
                .FirstOrDefaultAsync(x => x.Id == queryId && x.TrackerId == trackerId);

            var hasAccess = query != null && (query.Tracker.OwnerId == user.Id || query.Tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (query == null || !hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
            }

            return Result.Success(mapper.Map<Query, QueryDto>(query));
        }

        public async Task<Result<List<QueryDto>>> GetQueryList(string trackerId)
        {
            var user = currentUserService.GetCurrentUser();

            var tracker = await db.Trackers
                .Include(x => x.ApplicationUserTrackers)
                .FirstOrDefaultAsync(x => x.Id == trackerId);

            var hasAccess = tracker != null && (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
            }

            var queries = await db.Queries
                .Include(x => x.Filters)
                    .ThenInclude(x => x.Field)
                .Include(x => x.Sorts.OrderBy(s => s.Order))
                    .ThenInclude(x => x.Field)
                .Where(x => x.TrackerId == trackerId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return Result.Success(mapper.Map<List<Query>, List<QueryDto>>(queries));
        }
    }
}

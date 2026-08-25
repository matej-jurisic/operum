using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Enums;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Queries;
using Operum.Model.DTOs.Views;
using Operum.Model.DTOs.Views.Requests;
using Operum.Model.Models;
using Operum.Service.Domain.Queries;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Views
{
    public class ViewsService(ICurrentUserService currentUserService, OperumContext db, IMapper mapper, ILogger<ViewsService> logger) : IViewsService
    {
        public async Task<Result<ViewDto>> CreateView(string trackerId, CreateViewDto view)
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

            var viewCount = await db.Views.CountAsync(v => v.TrackerId == trackerId);
            if (viewCount >= DataLimits.MaxViewCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("views", DataLimits.MaxViewCount));

            var resolvedQueries = await ResolveViewQueries(trackerId, view.Queries);
            if (resolvedQueries.IsFailure)
                return Result.Failure(resolvedQueries.StatusCode, resolvedQueries.Messages);

            var userView = new View
            {
                TrackerId = trackerId,
                Name = view.Name,
                Description = view.Description,
            };

            var maxOrder = await db.Views
                .Where(x => x.TrackerId == trackerId)
                .MaxAsync(x => (int?)x.Order) ?? 0;
            userView.Order = maxOrder + 1;

            await db.Views.AddAsync(userView);
            await AttachViewQueries(userView.Id, resolvedQueries.Data!);

            await db.SaveChangesAsync();
            var created = await GetView(trackerId, userView.Id);
            return Result.Success(created.Data);
        }

        public async Task<Result<ViewDto>> UpdateView(string trackerId, string viewId, UpdateViewDto view)
        {
            var user = currentUserService.GetCurrentUser();
            var userView = await db.Views
                .Include(x => x.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(x => x.Id == viewId && x.TrackerId == trackerId);

            var isOwnerView = userView?.Tracker.OwnerId == user.Id;
            var utView = userView?.Tracker.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (userView == null || (!isOwnerView && utView?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            var resolvedQueries = await ResolveViewQueries(trackerId, view.Queries);
            if (resolvedQueries.IsFailure)
                return Result.Failure(resolvedQueries.StatusCode, resolvedQueries.Messages);

            userView.Name = view.Name;
            userView.Description = view.Description;

            // Only the join rows go away — the underlying Queries are independent and may
            // still be attached to other views, so they're never deleted here.
            await db.ViewQueries.Where(x => x.ViewId == viewId).ExecuteDeleteAsync();
            await AttachViewQueries(viewId, resolvedQueries.Data!);

            db.Views.Update(userView);
            await db.SaveChangesAsync();

            var updated = await GetView(trackerId, viewId);
            return Result.Success(updated.Data);
        }

        public async Task<Result> DeleteView(string trackerId, string viewId)
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

            // Deleting a View only removes its ViewQuery links (DB cascade); the Queries
            // it referenced are independent and keep existing.
            await db.Views.Where(x => x.Id == viewId && x.TrackerId == trackerId).ExecuteDeleteAsync();

            return Result.Success();
        }

        public async Task<Result<ViewDto>> GetView(string trackerId, string viewId)
        {
            var user = currentUserService.GetCurrentUser();

            var userView = await db.Views
                .Include(x => x.Tracker)
                    .ThenInclude(x => x.ApplicationUserTrackers)
                .Include(x => x.ViewQueries.OrderBy(vq => vq.Order))
                    .ThenInclude(vq => vq.Query)
                        .ThenInclude(q => q.Sorts.OrderBy(s => s.Order))
                            .ThenInclude(s => s.Field)
                .Include(x => x.ViewQueries)
                    .ThenInclude(vq => vq.Query)
                        .ThenInclude(q => q.Filters)
                            .ThenInclude(f => f.Field)
                .FirstOrDefaultAsync(x => x.Id == viewId && x.TrackerId == trackerId);

            var hasAccess = userView != null && (userView.Tracker.OwnerId == user.Id || userView.Tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (userView == null || !hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
            }

            return Result.Success(MapViewToDto(userView));
        }

        public async Task<Result<List<ViewDto>>> GetViewList(string trackerId)
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

            var userViews = await db.Views
                .Include(x => x.ViewQueries.OrderBy(vq => vq.Order))
                    .ThenInclude(vq => vq.Query)
                        .ThenInclude(q => q.Filters)
                            .ThenInclude(f => f.Field)
                .Include(x => x.ViewQueries)
                    .ThenInclude(vq => vq.Query)
                        .ThenInclude(q => q.Sorts.OrderBy(s => s.Order))
                            .ThenInclude(s => s.Field)
                .Where(x => x.TrackerId == trackerId)
                .OrderBy(x => x.Order)
                .ToListAsync();

            return Result.Success(userViews.Select(MapViewToDto).ToList());
        }

        public async Task<Result> ReorderViews(string trackerId, ReorderViewsDto reorderViews)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);
            var isOwnerReorder = tracker?.OwnerId == user.Id;
            var utReorder = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);

            if (tracker == null || (!isOwnerReorder && utReorder?.CanEditSchema != true))
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            var existingViewIds = await db.Views
                .Where(x => x.TrackerId == trackerId)
                .Select(x => x.Id)
                .ToListAsync();

            if (!reorderViews.ViewIds.ToHashSet().SetEquals(existingViewIds.ToHashSet()))
            {
                return Result.Failure(ResultStatusCodes.BadRequest);
            }

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < reorderViews.ViewIds.Count; i++)
                {
                    var viewId = reorderViews.ViewIds[i];
                    var view = await db.Views.FindAsync(viewId);

                    if (view != null && view.TrackerId == trackerId)
                    {
                        view.Order = i + 1;
                        db.Views.Update(view);
                    }
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Exception occurred while reordering views.");
                return Result.Failure(ResultStatusCodes.Error);
            }
        }

        private ViewDto MapViewToDto(View view)
        {
            return new ViewDto
            {
                Id = view.Id,
                Name = view.Name,
                Description = view.Description,
                Queries = view.ViewQueries
                    .OrderBy(vq => vq.Order)
                    .Select(vq => MapQueryToDto(vq.Query))
                    .ToList(),
            };
        }

        private QueryDto MapQueryToDto(Query query)
        {
            return new QueryDto
            {
                Id = query.Id,
                Name = query.Name,
                Description = query.Description,
                Sorts = query.Sorts.OrderBy(s => s.Order).Select(s => new QuerySortDto
                {
                    Id = s.Id,
                    Field = mapper.Map<Field, FieldDto>(s.Field),
                    Order = s.Order,
                    Descending = s.Descending,
                }).ToList(),
                Filters = query.Filters.Select(f => new QueryFilterDto
                {
                    Id = f.Id,
                    Field = mapper.Map<Field, FieldDto>(f.Field),
                    Operator = f.Operator,
                    Value = f.Value,
                }).ToList(),
            };
        }

        // Resolves each ordered ViewQueryRefDto to a concrete Query id, validating and
        // building brand-new Query entities for ad-hoc refs along the way (not yet saved).
        private async Task<Result<List<(string QueryId, Query? NewQuery)>>> ResolveViewQueries(string trackerId, List<ViewQueryRefDto> queryRefs)
        {
            var resolved = new List<(string QueryId, Query? NewQuery)>();
            var existingQueryCount = await db.Queries.CountAsync(q => q.TrackerId == trackerId);
            var newQueryCount = 0;

            foreach (var queryRef in queryRefs)
            {
                if (queryRef.QueryId != null)
                {
                    var exists = await db.Queries.AnyAsync(q => q.Id == queryRef.QueryId && q.TrackerId == trackerId);
                    if (!exists)
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("query"));

                    resolved.Add((queryRef.QueryId, null));
                }
                else if (queryRef.NewQuery != null)
                {
                    if (existingQueryCount + newQueryCount >= DataLimits.MaxQueryCount)
                        return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("queries", DataLimits.MaxQueryCount));

                    var validation = await QueryBuilder.ValidateSortsAndFilters(db, trackerId, queryRef.NewQuery.Sorts, queryRef.NewQuery.Filters);
                    if (validation.IsFailure)
                        return Result.Failure(validation.StatusCode, validation.Messages);

                    var newQuery = QueryBuilder.BuildQueryEntity(trackerId, queryRef.NewQuery);
                    newQueryCount++;
                    resolved.Add((newQuery.Id, newQuery));
                }
            }

            return Result.Success(resolved);
        }

        private async Task AttachViewQueries(string viewId, List<(string QueryId, Query? NewQuery)> resolvedQueries)
        {
            for (int i = 0; i < resolvedQueries.Count; i++)
            {
                var (queryId, newQuery) = resolvedQueries[i];
                if (newQuery != null)
                    await db.Queries.AddAsync(newQuery);

                await db.ViewQueries.AddAsync(new ViewQuery
                {
                    ViewId = viewId,
                    QueryId = queryId,
                    Order = i,
                });
            }
        }
    }
}

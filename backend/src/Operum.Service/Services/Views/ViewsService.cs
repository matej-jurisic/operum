using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Views;
using Operum.Model.DTOs.Views.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Domain.Views;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Views
{
    public class ViewsService(ICurrentUserService currentUserService, OperumContext db, IMapper mapper, ILogger<ViewsService> logger) : IViewsService
    {
        public async Task<Result<ViewDto>> CreateView(string trackerId, CreateViewDto view)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            foreach (var sort in view.Sorts)
            {
                var field = await db.Fields.FindAsync(sort.FieldId);
                if (field == null || field.TrackerId != trackerId)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("sort field"));
            }

            foreach (var filter in view.Filters)
            {
                var field = await db.Fields.FindAsync(filter.FieldId);
                if (field == null || field.TrackerId != trackerId)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("filter field"));

                // Validate operator is valid for the field type
                if (!ViewFilterValidator.IsValidOperatorForFieldType(filter.Operator, field.Type))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"operator '{filter.Operator}' for field type '{field.Type}'"));

                // Validate field value
                if (filter.Value != null && !ViewFilterValidator.IsValidFieldValue(filter.Value, field.Type))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"value '{filter.Value}' for field type '{field.Type}'"));
            }

            var userView = mapper.Map<CreateViewDto, View>(view);
            userView.TrackerId = trackerId;

            var maxOrder = await db.Views
                .Where(x => x.TrackerId == trackerId)
                .MaxAsync(x => (int?)x.Order) ?? 0;
            userView.Order = maxOrder + 1;

            await db.Views.AddAsync(userView);
            await db.SaveChangesAsync();
            var created = await GetView(trackerId, userView.Id);
            return Result.Success(created.Data);
        }

        public async Task<Result<ViewDto>> UpdateView(string trackerId, string viewId, UpdateViewDto view)
        {
            var user = currentUserService.GetCurrentUser();
            var userView = await db.Views
                .Include(x => x.Tracker)
                .FirstOrDefaultAsync(x => x.Id == viewId && x.TrackerId == trackerId);

            if (userView == null || userView.Tracker.OwnerId != user.Id)
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            foreach (var sort in view.Sorts)
            {
                var field = await db.Fields.FindAsync(sort.FieldId);
                if (field == null || field.TrackerId != trackerId)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("sort field"));
            }

            foreach (var filter in view.Filters)
            {
                var field = await db.Fields.FindAsync(filter.FieldId);
                if (field == null || field.TrackerId != trackerId)
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.ItemNotFound("filter field"));

                if (!ViewFilterValidator.IsValidOperatorForFieldType(filter.Operator, field.Type))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"operator '{filter.Operator}' for field type '{field.Type}'"));

                if (filter.Value != null && !ViewFilterValidator.IsValidFieldValue(filter.Value, field.Type))
                    return Result.Failure(ResultStatusCodes.BadRequest, Messages.Invalid($"value '{filter.Value}' for field type '{field.Type}'"));
            }

            userView.Name = view.Name;
            userView.Description = view.Description;

            await db.ViewSorts.Where(x => x.ViewId == viewId).ExecuteDeleteAsync();
            await db.ViewFilters.Where(x => x.ViewId == viewId).ExecuteDeleteAsync();

            var newSorts = view.Sorts.Select((s, i) =>
            {
                var sort = mapper.Map<CreateViewSortDto, ViewSort>(s);
                sort.ViewId = viewId;
                sort.Order = i;
                return sort;
            }).ToList();

            var newFilters = view.Filters.Select(f =>
            {
                var filter = mapper.Map<CreateViewFilterDto, ViewFilter>(f);
                filter.ViewId = viewId;
                return filter;
            }).ToList();

            await db.ViewSorts.AddRangeAsync(newSorts);
            await db.ViewFilters.AddRangeAsync(newFilters);
            db.Views.Update(userView);
            await db.SaveChangesAsync();

            var updated = await GetView(trackerId, viewId);
            return Result.Success(updated.Data);
        }

        public async Task<Result> DeleteView(string trackerId, string viewId)
        {
            var user = currentUserService.GetCurrentUser();

            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return Result.Failure(ResultStatusCodes.NotFound);
            }

            await db.Views.Where(x => x.Id == viewId && x.TrackerId == trackerId).ExecuteDeleteAsync();

            return Result.Success();
        }

        public async Task<Result<ViewDto>> GetView(string trackerId, string viewId)
        {
            var user = currentUserService.GetCurrentUser();

            var userView = await db.Views
                .Include(x => x.Tracker)
                    .ThenInclude(x => x.ApplicationUserTrackers)
                .Include(x => x.Sorts.OrderBy(s => s.Order))
                    .ThenInclude(x => x.Field)
                .Include(x => x.Filters)
                    .ThenInclude(x => x.Field)
                .FirstOrDefaultAsync(x => x.Id == viewId && x.TrackerId == trackerId);

            var hasAccess = userView != null && (userView.Tracker.OwnerId == user.Id || userView.Tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (userView == null || !hasAccess)
            {
                return Result.Failure(ResultStatusCodes.Forbidden);
            }

            return Result.Success(mapper.Map<View, ViewDto>(userView));
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
                .Include(x => x.Tracker)
                .Include(x => x.Filters)
                    .ThenInclude(x => x.Field)
                .Include(x => x.Sorts.OrderBy(s => s.Order))
                    .ThenInclude(x => x.Field)
                .Where(x => x.TrackerId == trackerId)
                .OrderBy(x => x.Order)
                .ToListAsync();

            return Result.Success(mapper.Map<List<View>, List<ViewDto>>(userViews));
        }

        public async Task<Result> ReorderViews(string trackerId, ReorderViewsDto reorderViews)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || user.Id != tracker.OwnerId)
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
    }
}

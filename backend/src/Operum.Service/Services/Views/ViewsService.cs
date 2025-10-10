using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs.Views;
using Operum.Model.DTOs.Views.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Domain.Views;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Views
{
    public class ViewsService(ICurrentUserService currentUserService, OperumContext db, IMapper mapper) : IViewsService
    {
        public async Task<Result<ViewDto>> CreateView(string trackerId, CreateViewDto view)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return Result.Failure(ResultStatus.NotFound);
            }

            foreach (var sort in view.Sorts)
            {
                var field = await db.Fields.FindAsync(sort.FieldId);
                if (field == null || field.TrackerId != trackerId)
                    return Result.Failure(ResultStatus.BadRequest, "Sort field not found or doesn't belong to tracker");
            }

            foreach (var filter in view.Filters)
            {
                var field = await db.Fields.FindAsync(filter.FieldId);
                if (field == null || field.TrackerId != trackerId)
                    return Result.Failure(ResultStatus.BadRequest, "Filter field not found or doesn't belong to tracker");

                // Validate operator is valid for the field type
                if (!ViewFilterValidator.IsValidOperatorForFieldType(filter.Operator, field.Type))
                    return Result.Failure(ResultStatus.BadRequest, $"Operator '{filter.Operator}' is not valid for field type '{field.Type}'");

                // Validate field value
                if (filter.Value != null && !ViewFilterValidator.IsValidFieldValue(filter.Value, field.Type))
                    return Result.Failure(ResultStatus.BadRequest, $"Value '{filter.Value}' is invalid for field type '{field.Type}'");
            }

            var userView = mapper.Map<CreateViewDto, View>(view);
            userView.TrackerId = trackerId;
            await db.Views.AddAsync(userView);
            await db.SaveChangesAsync();
            var created = await GetView(trackerId, userView.Id);
            return Result.Success(created.Data);
        }

        public async Task<Result> DeleteView(string trackerId, string viewId)
        {
            var user = currentUserService.GetCurrentUser();

            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return Result.Failure(ResultStatus.NotFound);
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
                return Result.Failure(ResultStatus.Forbidden);
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
                return Result.Failure(ResultStatus.Forbidden);
            }

            var userViews = await db.Views
                .Include(x => x.Tracker)
                .Include(x => x.Filters)
                    .ThenInclude(x => x.Field)
                .Include(x => x.Sorts.OrderBy(s => s.Order))
                    .ThenInclude(x => x.Field)
                .Where(x => x.TrackerId == trackerId)
                .ToListAsync();

            return Result.Success(mapper.Map<List<View>, List<ViewDto>>(userViews));
        }
    }
}

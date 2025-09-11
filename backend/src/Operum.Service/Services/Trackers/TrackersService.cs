using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Helpers;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Trackers
{
    public class TrackersService(IAuthorizationService authorizationService, OperumContext db, IMapper mapper) : ITrackersService
    {
        public async Task<ServiceResponse<TrackerDto>> CreateTracker(CreateTrackerDto tracker)
        {
            var user = authorizationService.GetCurrentUserDto();

            var trackerCount = await db.Trackers.Where(x => x.OwnerId == user.Id).CountAsync();
            if (trackerCount >= DataLimits.MaxTrackerCount)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, $"Maximum number of trackers {DataLimits.MaxTrackerCount} reached.");
            }

            var trackerModel = mapper.Map<CreateTrackerDto, Tracker>(tracker);

            trackerModel.OwnerId = user.Id;
            trackerModel.Color = trackerModel.Color?.ToLower();

            await db.Trackers.AddAsync(trackerModel);
            await db.SaveChangesAsync();

            var created = await GetTracker(trackerModel.Id);
            return ServiceResponse.Success(created.Data);
        }

        public async Task<ServiceResponse> DeleteTracker(string id)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(id);

            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            db.Trackers.Remove(tracker);
            await db.SaveChangesAsync();
            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse<TrackerDto>> GetTracker(string id)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers
                .Include(x => x.Fields)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            return ServiceResponse.Success(mapper.Map<Tracker, TrackerDto>(tracker));
        }

        public async Task<ServiceResponse<List<TrackerDto>>> GetTrackerList()
        {
            var user = authorizationService.GetCurrentUserDto();
            var trackers = await db.Trackers
                .Include(x => x.Fields)
                .Where(x => x.OwnerId == user.Id)
                .ToListAsync();
            return ServiceResponse.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(trackers));
        }

        public async Task<ServiceResponse<TrackerDto>> UpdateTracker(string id, UpdateTrackerDto tracker)
        {
            var user = authorizationService.GetCurrentUserDto();
            var originalTracker = await db.Trackers.FindAsync(id);

            if (originalTracker?.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            mapper.Map(tracker, originalTracker);
            db.Trackers.Update(originalTracker);
            await db.SaveChangesAsync();

            var updatedTracker = await GetTracker(originalTracker.Id);
            return ServiceResponse.Success(updatedTracker.Data);
        }

        public async Task<ServiceResponse<List<FieldAnalyticsDto>>> GetTrackerAnalytics(string trackerId, string? viewId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            View? view = null;
            if (!string.IsNullOrEmpty(viewId))
            {
                view = await db.Views
                    .Include(v => v.Filters)
                    .ThenInclude(s => s.Field)
                    .FirstOrDefaultAsync(v => v.Id == viewId && v.TrackerId == trackerId);

                if (view == null)
                {
                    return ServiceResponse.Failure(StatusCodeEnum.NotFound, "View not found or doesn't belong to this tracker");
                }
            }

            var entriesQuery = db.Entries
                .Include(x => x.FieldValues)
                .ThenInclude(x => x.Field)
                .Where(x => x.TrackerId == trackerId);

            if (view != null && view.Filters.Count != 0)
            {
                entriesQuery = ViewHelpers.ApplyViewFilters(entriesQuery, view.Filters);
            }

            var entries = await entriesQuery.ToListAsync();

            // Process analytics in parallel with proper null handling
            var analyticsResult = entries
                .SelectMany(e => e.FieldValues)
                .GroupBy(fv => fv.Field.Id)
                .AsParallel()
                .Select(group =>
                {
                    var field = group.First().Field;
                    var fieldValues = group.ToList();

                    var analytics = field.Type switch
                    {
                        DataTypes.Number => TrackerAnalyticsHelpers.GetNumericAnalytics(fieldValues),
                        DataTypes.DateTime => TrackerAnalyticsHelpers.GetDateTimeAnalytics(fieldValues),
                        DataTypes.Date => TrackerAnalyticsHelpers.GetDateAnalytics(fieldValues),
                        DataTypes.TimeSpan => TrackerAnalyticsHelpers.GetTimeSpanAnalytics(fieldValues),
                        DataTypes.Bool => TrackerAnalyticsHelpers.GetBooleanAnalytics(fieldValues),
                        _ => null
                    };

                    if (analytics != null)
                    {
                        analytics.FieldName = field.Name;
                        analytics.FieldType = field.Type;
                    }

                    return analytics;
                })
                .Where(analytics => analytics != null)
                .Cast<FieldAnalyticsDto>()
                .OrderBy(a => a.FieldName)
                .ToList();

            return ServiceResponse.Success(analyticsResult);
        }
    }
}

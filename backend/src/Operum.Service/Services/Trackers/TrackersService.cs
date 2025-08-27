using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Helpers;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;
using Operum.Service.Services.Trackers.Helpers;

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

        public async Task<ServiceResponse<List<FieldAnalyticsDto>>> GetTrackerAnalytics(string trackerId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var fieldsWithData = await db.Fields
                .Include(x => x.FieldValues)
                .Where(x => x.TrackerId == trackerId)
                .Select(f => new
                {
                    f.Id,
                    f.Type,
                    f.Name,
                    Values = f.FieldValues
                })
                .ToListAsync();

            var analyticsResult = new List<FieldAnalyticsDto>();

            foreach (var field in fieldsWithData)
            {
                var analytics = field.Type switch
                {
                    DataTypes.Number => TrackerAnalyticsHelpers.GetNumericAnalytics(field.Values, field.Name),
                    DataTypes.DateTime => TrackerAnalyticsHelpers.GetDateTimeAnalytics(field.Values, field.Name),
                    DataTypes.Date => TrackerAnalyticsHelpers.GetDateAnalytics(field.Values, field.Name),
                    DataTypes.TimeSpan => TrackerAnalyticsHelpers.GetTimeSpanAnalytics(field.Values, field.Name),
                    DataTypes.Bool => TrackerAnalyticsHelpers.GetBooleanAnalytics(field.Values, field.Name),
                    _ => null
                };

                if (analytics != null)
                {
                    analyticsResult.Add(analytics);
                }
            }

            return ServiceResponse.Success(analyticsResult);
        }
    }
}

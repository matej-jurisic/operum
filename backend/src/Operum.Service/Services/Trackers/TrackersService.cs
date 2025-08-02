using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.DTOs.Trackers;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Trackers
{
    public class TrackersService(IAuthorizationService authorizationService, OperumContext db, IMapper mapper) : ITrackersService
    {
        public async Task<ServiceResponse<TrackerDto>> CreateTracker(CreateTrackerDto tracker)
        {
            var user = authorizationService.GetCurrentUserDto();
            var trackerModel = mapper.Map<CreateTrackerDto, Tracker>(tracker);
            trackerModel.OwnerId = user.Id;

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
    }
}

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
            var user = authorizationService.GetCurrentApplicationUserDto();
            var createTracker = mapper.Map<CreateTrackerDto, Tracker>(tracker);
            createTracker.OwnerId = user.Id;
            var created = await db.Trackers.AddAsync(createTracker);
            await db.SaveChangesAsync();
            return ServiceResponse.Success(mapper.Map<Tracker, TrackerDto>(created.Entity));
        }

        public async Task<ServiceResponse> DeleteTracker(string id)
        {
            var user = authorizationService.GetCurrentApplicationUserDto();
            var tracker = await db.Trackers.FindAsync(id);
            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }
            db.Remove(tracker);
            await db.SaveChangesAsync();
            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse<TrackerDto>> GetTracker(string id)
        {
            var user = authorizationService.GetCurrentApplicationUserDto();
            var tracker = await db.Trackers.FindAsync(id);
            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }
            return ServiceResponse.Success(mapper.Map<Tracker, TrackerDto>(tracker));
        }

        public async Task<ServiceResponse<List<TrackerDto>>> GetTrackerList()
        {
            var user = authorizationService.GetCurrentApplicationUserDto();
            var trackers = await db.Trackers.Where(x => x.OwnerId == user.Id).ToListAsync();
            return ServiceResponse.Success(mapper.Map<List<Tracker>, List<TrackerDto>>(trackers));
        }

        public async Task<ServiceResponse<TrackerDto>> UpdateTracker(UpdateTrackerDto tracker)
        {
            var user = authorizationService.GetCurrentApplicationUserDto();
            var originalTracker = await db.Trackers
                .Include(x => x.Owner)
                .FirstOrDefaultAsync(x => x.Id == tracker.Id);
            if (originalTracker == null || originalTracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }
            mapper.Map(tracker, originalTracker);
            var updated = db.Update(originalTracker);
            await db.SaveChangesAsync();
            return ServiceResponse.Success(mapper.Map<Tracker, TrackerDto>(updated.Entity));
        }
    }
}

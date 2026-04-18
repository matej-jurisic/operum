using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.TrackerConstants;
using Operum.Model.DTOs.TrackerConstants.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Fields
{
    public class TrackerConstantsService(ICurrentUserService currentUserService, IMapper mapper, OperumContext db) : ITrackerConstantsService
    {
        public async Task<Result<TrackerConstantDto>> CreateConstant(string trackerId, CreateTrackerConstantDto dto)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);
            var isOwner = tracker?.OwnerId == user.Id;
            var userTracker = tracker?.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);

            if (tracker == null || (!isOwner && userTracker?.CanEditSchema != true))
                return Result.Failure(ResultStatusCodes.NotFound);

            var constantCount = await db.TrackerConstants.Where(c => c.TrackerId == trackerId).CountAsync();
            if (constantCount >= DataLimits.MaxConstantCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("constants", DataLimits.MaxConstantCount));

            var nameExists = await db.TrackerConstants
                .AnyAsync(c => c.TrackerId == trackerId && c.Name.ToLower() == dto.Name.ToLower());
            if (nameExists)
                return Result.Failure(ResultStatusCodes.BadRequest, "A constant with this name already exists.");

            // Prevent collision with existing field names
            var fieldNameExists = await db.Fields
                .AnyAsync(f => f.TrackerId == trackerId && f.Name.ToLower() == dto.Name.ToLower());
            if (fieldNameExists)
                return Result.Failure(ResultStatusCodes.BadRequest, "A field with this name already exists. Choose a different name for the constant.");

            var constant = mapper.Map<CreateTrackerConstantDto, TrackerConstant>(dto);
            constant.TrackerId = trackerId;

            await db.TrackerConstants.AddAsync(constant);
            await db.SaveChangesAsync();

            return Result.Success(mapper.Map<TrackerConstant, TrackerConstantDto>(constant));
        }

        public async Task<Result<TrackerConstantDto>> GetConstant(string trackerId, string constantId)
        {
            var user = currentUserService.GetCurrentUser();
            var constant = await db.TrackerConstants
                .Include(c => c.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(c => c.Id == constantId && c.TrackerId == trackerId);

            if (constant == null)
                return Result.Failure(ResultStatusCodes.NotFound);

            var hasAccess = constant.Tracker.OwnerId == user.Id ||
                            constant.Tracker.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id);
            if (!hasAccess)
                return Result.Failure(ResultStatusCodes.Forbidden);

            return Result.Success(mapper.Map<TrackerConstant, TrackerConstantDto>(constant));
        }

        public async Task<Result<List<TrackerConstantDto>>> GetConstantList(string trackerId)
        {
            var user = currentUserService.GetCurrentUser();
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId);

            if (tracker == null)
                return Result.Failure(ResultStatusCodes.NotFound);

            var hasAccess = tracker.OwnerId == user.Id ||
                            tracker.ApplicationUserTrackers.Any(ut => ut.ApplicationUserId == user.Id);
            if (!hasAccess)
                return Result.Failure(ResultStatusCodes.Forbidden);

            var constants = await db.TrackerConstants
                .Where(c => c.TrackerId == trackerId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Result.Success(mapper.Map<List<TrackerConstant>, List<TrackerConstantDto>>(constants));
        }

        public async Task<Result<TrackerConstantDto>> UpdateConstant(string trackerId, string constantId, UpdateTrackerConstantDto dto)
        {
            var user = currentUserService.GetCurrentUser();
            var constant = await db.TrackerConstants
                .Include(c => c.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(c => c.Id == constantId && c.TrackerId == trackerId);

            var isOwnerUpdate = constant?.Tracker.OwnerId == user.Id;
            var utUpdate = constant?.Tracker.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (constant == null || (!isOwnerUpdate && utUpdate?.CanEditSchema != true))
                return Result.Failure(ResultStatusCodes.NotFound);

            var nameExists = await db.TrackerConstants
                .AnyAsync(c => c.TrackerId == trackerId && c.Name.ToLower() == dto.Name.ToLower() && c.Id != constantId);
            if (nameExists)
                return Result.Failure(ResultStatusCodes.BadRequest, "A constant with this name already exists.");

            // Prevent collision with existing field names
            var fieldNameExists = await db.Fields
                .AnyAsync(f => f.TrackerId == trackerId && f.Name.ToLower() == dto.Name.ToLower());
            if (fieldNameExists)
                return Result.Failure(ResultStatusCodes.BadRequest, "A field with this name already exists. Choose a different name for the constant.");

            mapper.Map(dto, constant);
            db.TrackerConstants.Update(constant);
            await db.SaveChangesAsync();

            return Result.Success(mapper.Map<TrackerConstant, TrackerConstantDto>(constant));
        }

        public async Task<Result> DeleteConstant(string trackerId, string constantId)
        {
            var user = currentUserService.GetCurrentUser();
            var constant = await db.TrackerConstants
                .Include(c => c.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(c => c.Id == constantId && c.TrackerId == trackerId);

            var isOwnerDel = constant?.Tracker.OwnerId == user.Id;
            var utDel = constant?.Tracker.ApplicationUserTrackers.FirstOrDefault(ut => ut.ApplicationUserId == user.Id);
            if (constant == null || (!isOwnerDel && utDel?.CanEditSchema != true))
                return Result.Failure(ResultStatusCodes.NotFound);

            db.TrackerConstants.Remove(constant);
            await db.SaveChangesAsync();

            return Result.Success();
        }
    }
}

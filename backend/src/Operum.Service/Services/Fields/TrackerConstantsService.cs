using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
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

            if (dto.Values.Count > DataLimits.MaxConstantValueCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("conditional values", DataLimits.MaxConstantValueCount));

            var validationError = ValidateConstantValues(dto.Values, dto.Type, trackerId);
            if (validationError != null)
                return Result.Failure(ResultStatusCodes.BadRequest, validationError);

            var constant = mapper.Map<CreateTrackerConstantDto, TrackerConstant>(dto);
            constant.TrackerId = trackerId;
            constant.Values = BuildConstantValues(dto.Values);

            await db.TrackerConstants.AddAsync(constant);
            await db.SaveChangesAsync();

            await db.Entry(constant).Collection(c => c.Values).Query()
                .Include(v => v.Filters)
                .LoadAsync();

            return Result.Success(mapper.Map<TrackerConstant, TrackerConstantDto>(constant));
        }

        public async Task<Result<TrackerConstantDto>> GetConstant(string trackerId, string constantId)
        {
            var user = currentUserService.GetCurrentUser();
            var constant = await db.TrackerConstants
                .Include(c => c.Tracker)
                    .ThenInclude(t => t.ApplicationUserTrackers)
                .Include(c => c.Values)
                    .ThenInclude(v => v.Filters)
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
                .Include(c => c.Values)
                    .ThenInclude(v => v.Filters)
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
                .Include(c => c.Values)
                    .ThenInclude(v => v.Filters)
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

            if (dto.Values.Count > DataLimits.MaxConstantValueCount)
                return Result.Failure(ResultStatusCodes.BadRequest, Messages.MaxNumberReached("conditional values", DataLimits.MaxConstantValueCount));

            var validationError = ValidateConstantValues(dto.Values, dto.Type, trackerId);
            if (validationError != null)
                return Result.Failure(ResultStatusCodes.BadRequest, validationError);

            mapper.Map(dto, constant);

            // Replace all conditional values: remove old, insert new separately so EF
            // tracks new rows as Added (not Modified), avoiding a concurrency exception.
            var oldValues = constant.Values.ToList();
            db.TrackerConstantValues.RemoveRange(oldValues);

            var newValues = BuildConstantValues(dto.Values);
            foreach (var v in newValues) v.TrackerConstantId = constant.Id;
            await db.TrackerConstantValues.AddRangeAsync(newValues);

            // Mark only the constant's scalar properties as modified, not the whole graph.
            db.Entry(constant).State = EntityState.Modified;

            await db.SaveChangesAsync();

            // Reload new values with filters for mapping
            constant.Values = newValues;
            foreach (var v in newValues)
                await db.Entry(v).Collection(x => x.Filters).LoadAsync();

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

        private static List<TrackerConstantValue> BuildConstantValues(List<CreateTrackerConstantValueDto> valueDtos)
        {
            return valueDtos.Select(v => new TrackerConstantValue
            {
                Priority = v.Priority,
                Value = v.Value,
                Filters = v.Filters.Select(f => new TrackerConstantValueFilter
                {
                    FieldId = f.FieldId,
                    Operator = f.Operator,
                    Value = f.Value
                }).ToList()
            }).ToList();
        }

        private string? ValidateConstantValues(List<CreateTrackerConstantValueDto> values, string constantType, string trackerId)
        {
            foreach (var v in values)
            {
                if (string.IsNullOrWhiteSpace(v.Value))
                    return "Each conditional value must have a value.";

                var valueIsValid = constantType switch
                {
                    DataTypes.Number => double.TryParse(v.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _),
                    DataTypes.Bool => bool.TryParse(v.Value, out _),
                    DataTypes.TimeSpan => TimeSpan.TryParse(v.Value, out _),
                    _ => false
                };
                if (!valueIsValid)
                    return $"Conditional value '{v.Value}' is not valid for type '{constantType}'.";

                if (v.Filters.Count == 0)
                    return "Each conditional value must have at least one filter.";

                if (v.Filters.Count > DataLimits.MaxFilters)
                    return Messages.MaxNumberReached("filters per conditional value", DataLimits.MaxFilters);

                foreach (var f in v.Filters)
                {
                    if (string.IsNullOrWhiteSpace(f.FieldId))
                        return "Each filter must have a field selected.";
                    if (!IsKnownOperator(f.Operator))
                        return $"Unknown operator '{f.Operator}'.";
                }
            }
            return null;
        }

        private static bool IsKnownOperator(string op) =>
            op is OperatorTypes.EqualsOperator or OperatorTypes.NotEquals
                or OperatorTypes.GreaterThan or OperatorTypes.GreaterThanOrEqual
                or OperatorTypes.LessThan or OperatorTypes.LessThanOrEqual
                or OperatorTypes.Contains or OperatorTypes.StartsWith or OperatorTypes.EndsWith;
    }
}

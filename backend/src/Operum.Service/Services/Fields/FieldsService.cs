using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.Enums;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Fields
{
    public class FieldsService(IAuthorizationService authorizationService, IMapper mapper, OperumContext db, ILogger<FieldsService> logger) : IFieldsService
    {
        public async Task<Result<FieldDto>> CreateField(string trackerId, CreateFieldDto field)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || !user.Owns(tracker))
            {
                return Result.Failure(StatusCodeEnum.NotFound);
            }

            var fieldCount = await db.Fields.Where(x => x.TrackerId == trackerId).CountAsync();
            if (fieldCount >= DataLimits.MaxFieldCount)
            {
                return Result.Failure(StatusCodeEnum.BadRequest, $"Maximum number of {DataLimits.MaxFieldCount} fields reached.");
            }

            if (!DataTypes.IsValid(field.Type)) return Result.Failure(StatusCodeEnum.BadRequest, $"Field type {field.Type} is not allowed.");

            var newField = mapper.Map<CreateFieldDto, Field>(field);

            newField.TrackerId = trackerId;

            // Set the order to be last
            var maxOrder = await db.Fields
                .Where(x => x.TrackerId == trackerId)
                .MaxAsync(x => (int?)x.Order) ?? 0;
            newField.Order = maxOrder + 1;

            await db.Fields.AddAsync(newField);
            await db.SaveChangesAsync();

            var created = await GetField(trackerId, newField.Id);
            return Result.Success(created.Data);
        }

        public async Task<Result> DeleteField(string trackerId, string fieldId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var field = await db.Fields
                .Include(x => x.Tracker)
                .FirstOrDefaultAsync(x => x.Id == fieldId && x.TrackerId == trackerId);

            if (field == null || !user.Owns(field))
            {
                return Result.Failure(StatusCodeEnum.NotFound);
            }

            db.Fields.Remove(field);
            await db.SaveChangesAsync();

            // Reorder remaining fields to fill the gap
            await ReorderFieldsAfterDeletion(trackerId, field.Order);

            return Result.Success();
        }

        public async Task<Result<FieldDto>> GetField(string trackerId, string fieldId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var field = await db.Fields
                 .Include(x => x.Tracker)
                    .ThenInclude(x => x.ApplicationUserTrackers)
                 .FirstOrDefaultAsync(x => x.Id == fieldId && x.TrackerId == trackerId);

            var hasAccess = field != null && (field.Tracker.OwnerId == user.Id || field.Tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (field == null || !hasAccess)
            {
                return Result.Failure(StatusCodeEnum.Forbidden);
            }

            return Result.Success(mapper.Map<Field, FieldDto>(field));
        }

        public async Task<Result<List<FieldDto>>> GetFieldList(string trackerId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers
                 .Include(x => x.ApplicationUserTrackers)
                 .FirstOrDefaultAsync(x => x.Id == trackerId);

            var hasAccess = tracker != null && (tracker.OwnerId == user.Id || tracker.ApplicationUserTrackers.Any(x => x.ApplicationUserId == user.Id));

            if (tracker == null || !hasAccess)
            {
                return Result.Failure(StatusCodeEnum.Forbidden);
            }

            var fields = await db.Fields
                .Where(x => x.TrackerId == trackerId)
                .OrderBy(x => x.Order)
                .ToListAsync();

            return Result.Success(mapper.Map<List<Field>, List<FieldDto>>(fields));
        }

        public async Task<Result> ReorderFields(string trackerId, ReorderFieldsDto reorderFields)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return Result.Failure(StatusCodeEnum.NotFound);
            }

            // Validate that all field IDs belong to this tracker
            var existingFields = await db.Fields
                .Where(x => x.TrackerId == trackerId)
                .Select(x => x.Id)
                .ToListAsync();

            var requestedFieldIds = reorderFields.FieldIds.ToHashSet();
            var existingFieldIds = existingFields.ToHashSet();

            // Check if all requested field IDs exist and belong to this tracker
            if (!requestedFieldIds.SetEquals(existingFieldIds))
            {
                return Result.Failure(StatusCodeEnum.BadRequest,
                    "Invalid field IDs provided or missing fields in reorder request.");
            }

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < reorderFields.FieldIds.Count; i++)
                {
                    var fieldId = reorderFields.FieldIds[i];
                    var field = await db.Fields.FindAsync(fieldId);

                    if (field != null && field.TrackerId == trackerId)
                    {
                        field.Order = i + 1;
                        db.Fields.Update(field);
                    }
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Exception occurred while reordering fields.");
                return Result.Failure(StatusCodeEnum.InternalServerError,
                    "Failed to reorder fields. Please try again.");
            }
        }

        public async Task<Result<FieldDto>> UpdateField(string trackerId, string fieldId, UpdateFieldDto field)
        {
            var user = authorizationService.GetCurrentUserDto();
            var originalField = await db.Fields
                .Include(x => x.Tracker)
                .FirstOrDefaultAsync(x => x.Id == fieldId && x.TrackerId == trackerId);

            if (originalField == null || !user.Owns(originalField))
            {
                return Result.Failure(StatusCodeEnum.NotFound);
            }

            if (!DataTypes.IsValid(field.Type)) return Result.Failure(StatusCodeEnum.BadRequest, $"Field type {field.Type} is not allowed.");

            mapper.Map(field, originalField);
            db.Fields.Update(originalField);
            await db.SaveChangesAsync();

            var updatedField = await GetField(trackerId, fieldId);
            return Result.Success(updatedField.Data);
        }

        private async Task ReorderFieldsAfterDeletion(string trackerId, int deletedOrder)
        {
            var fieldsToUpdate = await db.Fields
                .Where(x => x.TrackerId == trackerId && x.Order > deletedOrder)
                .ToListAsync();

            foreach (var field in fieldsToUpdate)
            {
                field.Order -= 1;
                db.Fields.Update(field);
            }

            await db.SaveChangesAsync();
        }
    }
}
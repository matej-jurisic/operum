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
        public async Task<ServiceResponse<FieldDto>> CreateField(string trackerId, CreateFieldDto field)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);
            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var fieldCount = await db.Fields.Where(x => x.TrackerId == trackerId).CountAsync();
            if (fieldCount >= DataLimits.MaxFieldCount)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, $"Maximum number of fields {DataLimits.MaxFieldCount} reached.");
            }

            if (!DataTypes.IsValid(field.Type)) return ServiceResponse.Failure(StatusCodeEnum.BadRequest, $"Field type {field.Type} is not allowed.");

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
            return ServiceResponse.Success(created.Data);
        }

        public async Task<ServiceResponse> DeleteField(string trackerId, string fieldId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var field = await db.Fields
                .Include(x => x.Tracker)
                .FirstOrDefaultAsync(x => x.Id == fieldId && x.TrackerId == trackerId);

            if (field == null || !user.Owns(field))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            db.Fields.Remove(field);
            await db.SaveChangesAsync();

            // Reorder remaining fields to fill the gap
            await ReorderFieldsAfterDeletion(trackerId, field.Order);

            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse<FieldDto>> GetField(string trackerId, string fieldId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var field = await db.Fields
                 .Include(x => x.Tracker)
                 .FirstOrDefaultAsync(x => x.Id == fieldId && x.TrackerId == trackerId);

            if (field == null || !user.Owns(field))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            return ServiceResponse.Success(mapper.Map<Field, FieldDto>(field));
        }

        public async Task<ServiceResponse<List<FieldDto>>> GetFieldList(string trackerId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var fields = await db.Fields
                .Where(x => x.TrackerId == trackerId)
                .OrderBy(x => x.Order)
                .ToListAsync();

            return ServiceResponse.Success(mapper.Map<List<Field>, List<FieldDto>>(fields));
        }

        public async Task<ServiceResponse> ReorderFields(string trackerId, ReorderFieldsDto reorderFields)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || !user.Owns(tracker))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
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
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest,
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

                return ServiceResponse.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Exception occurred while reordering fields.");
                return ServiceResponse.Failure(StatusCodeEnum.InternalServerError,
                    "Failed to reorder fields. Please try again.");
            }
        }

        public async Task<ServiceResponse<FieldDto>> UpdateField(string trackerId, string fieldId, UpdateFieldDto field)
        {
            var user = authorizationService.GetCurrentUserDto();
            var originalField = await db.Fields
                .Include(x => x.Tracker)
                .FirstOrDefaultAsync(x => x.Id == fieldId && x.TrackerId == trackerId);

            if (originalField == null || !user.Owns(originalField))
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            if (!DataTypes.IsValid(field.Type)) return ServiceResponse.Failure(StatusCodeEnum.BadRequest, $"Field type {field.Type} is not allowed.");

            mapper.Map(field, originalField);
            db.Fields.Update(originalField);
            await db.SaveChangesAsync();

            var updatedField = await GetField(trackerId, fieldId);
            return ServiceResponse.Success(updatedField.Data);
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
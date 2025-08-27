using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Helpers;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Fields
{
    public class FieldService(IAuthorizationService authorizationService, IMapper mapper, OperumContext db) : IFieldsService
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

            var fields = await db.Fields.Where(x => x.TrackerId == trackerId).ToListAsync();
            return ServiceResponse.Success(mapper.Map<List<Field>, List<FieldDto>>(fields));
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
    }
}

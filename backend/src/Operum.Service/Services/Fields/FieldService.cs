using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.DTOs.Fields;
using Operum.Model.Enums;
using Operum.Model.Models;
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
            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            if (!DataTypes.IsValid(field.Type)) return ServiceResponse.Failure(StatusCodeEnum.BadRequest, ["Field type is not allowed."]);

            var newField = mapper.Map<CreateFieldDto, Field>(field);

            newField.TrackerId = trackerId;

            await db.Fields.AddAsync(newField);
            await db.SaveChangesAsync();

            var created = await GetField(newField.Id);
            return ServiceResponse.Success(created.Data);
        }

        public async Task<ServiceResponse> DeleteField(string id)
        {
            var user = authorizationService.GetCurrentUserDto();
            var field = await db.Fields
                .Include(x => x.Tracker)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (field == null || field.Tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            db.Fields.Remove(field);
            await db.SaveChangesAsync();
            return ServiceResponse.Success();
        }

        public async Task<ServiceResponse<FieldDto>> GetField(string id)
        {
            var user = authorizationService.GetCurrentUserDto();
            var field = await db.Fields
                 .Include(x => x.Tracker)
                 .FirstOrDefaultAsync(x => x.Id == id);

            if (field == null || field.Tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            return ServiceResponse.Success(mapper.Map<Field, FieldDto>(field));
        }

        public async Task<ServiceResponse<List<FieldDto>>> GetFieldList(string trackerId)
        {
            var user = authorizationService.GetCurrentUserDto();
            var tracker = await db.Trackers.FindAsync(trackerId);

            if (tracker == null || tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            var fields = await db.Fields.Where(x => x.TrackerId == trackerId).ToListAsync();
            return ServiceResponse.Success(mapper.Map<List<Field>, List<FieldDto>>(fields));
        }

        public async Task<ServiceResponse<FieldDto>> UpdateField(string id, UpdateFieldDto field)
        {
            var user = authorizationService.GetCurrentUserDto();
            var originalField = await db.Fields
                .Include(x => x.Tracker)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (originalField == null || originalField.Tracker.OwnerId != user.Id)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound);
            }

            if (!DataTypes.IsValid(field.Type)) return ServiceResponse.Failure(StatusCodeEnum.BadRequest, ["Field type is not allowed."]);

            mapper.Map(field, originalField);
            db.Fields.Update(originalField);
            await db.SaveChangesAsync();

            var updatedField = await GetField(originalField.Id);
            return ServiceResponse.Success(updatedField.Data);
        }
    }
}

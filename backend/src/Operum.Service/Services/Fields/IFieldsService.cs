using Operum.Model.Common;
using Operum.Model.DTOs.Fields;

namespace Operum.Service.Services.Fields
{
    public interface IFieldsService
    {
        public Task<ServiceResponse<FieldDto>> CreateField(string trackerId, CreateFieldDto field);
        public Task<ServiceResponse<FieldDto>> GetField(string id);
        public Task<ServiceResponse<List<FieldDto>>> GetFieldList(string trackerId);
        public Task<ServiceResponse<FieldDto>> UpdateField(string id, UpdateFieldDto field);
        public Task<ServiceResponse> DeleteField(string id);
    }
}

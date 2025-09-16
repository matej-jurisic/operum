using Operum.Model.Common;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers.Requests;

namespace Operum.Service.Services.Fields
{
    public interface IFieldsService
    {
        public Task<ServiceResponse<FieldDto>> CreateField(string trackerId, CreateFieldDto field);
        public Task<ServiceResponse<FieldDto>> GetField(string trackerId, string fieldId);
        public Task<ServiceResponse<List<FieldDto>>> GetFieldList(string trackerId);
        public Task<ServiceResponse<FieldDto>> UpdateField(string trackerId, string fieldId, UpdateFieldDto field);
        public Task<ServiceResponse> ReorderFields(string trackerId, ReorderFieldsDto reorderFields);
        public Task<ServiceResponse> DeleteField(string trackerId, string fieldId);
    }
}

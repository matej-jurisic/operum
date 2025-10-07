using Operum.Model.Common;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Fields.Requests;

namespace Operum.Service.Services.Fields
{
    public interface IFieldsService
    {
        public Task<Result<FieldDto>> CreateField(string trackerId, CreateFieldDto field);
        public Task<Result<FieldDto>> GetField(string trackerId, string fieldId);
        public Task<Result<List<FieldDto>>> GetFieldList(string trackerId);
        public Task<Result<FieldDto>> UpdateField(string trackerId, string fieldId, UpdateFieldDto field);
        public Task<Result> ReorderFields(string trackerId, ReorderFieldsDto reorderFields);
        public Task<Result> DeleteField(string trackerId, string fieldId);
    }
}

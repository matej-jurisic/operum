using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Service.Services.Fields;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/trackers/{trackerId}/[controller]")]
    public class FieldsController(IFieldsService fieldsService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetFieldList([FromRoute] string trackerId)
        {
            return GetApiResponse(await fieldsService.GetFieldList(trackerId));
        }

        [HttpGet("{fieldId}")]
        public async Task<IActionResult> GetField([FromRoute] string trackerId, [FromRoute] string fieldId)
        {
            return GetApiResponse(await fieldsService.GetField(trackerId, fieldId));
        }

        [HttpPost]
        public async Task<IActionResult> CreateField([FromRoute] string trackerId, [FromBody] CreateFieldDto field)
        {
            return GetApiResponse(await fieldsService.CreateField(trackerId, field));
        }

        [HttpDelete("{fieldId}")]
        public async Task<IActionResult> DeleteField([FromRoute] string trackerId, [FromRoute] string fieldId)
        {
            return GetApiResponse(await fieldsService.DeleteField(trackerId, fieldId));
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderFields([FromRoute] string trackerId, [FromBody] ReorderFieldsDto reorderFields)
        {
            return GetApiResponse(await fieldsService.ReorderFields(trackerId, reorderFields));
        }

        [HttpPut("{fieldId}")]
        public async Task<IActionResult> UpdateField([FromRoute] string trackerId, [FromRoute] string fieldId, [FromBody] UpdateFieldDto field)
        {
            return GetApiResponse(await fieldsService.UpdateField(trackerId, fieldId, field));
        }
    }
}
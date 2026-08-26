using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Widgets.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WidgetsController(IWidgetsService widgetsService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetWidgets([FromQuery] string? trackerId)
        {
            return GetApiResponse(await widgetsService.GetWidgets(trackerId));
        }

        [HttpGet("{widgetId}")]
        public async Task<IActionResult> GetWidget([FromRoute] string widgetId)
        {
            return GetApiResponse(await widgetsService.GetWidget(widgetId));
        }

        [HttpPost]
        public async Task<IActionResult> CreateWidget([FromBody] CreateWidgetDto dto)
        {
            return GetApiResponse(await widgetsService.CreateWidget(dto));
        }

        // Only the widget's name and description -- the definition (result type, code,
        // sources, field mapping) is fixed at creation. See UpdateWidgetDto.
        [HttpPut("{widgetId}")]
        public async Task<IActionResult> UpdateWidget([FromRoute] string widgetId, [FromBody] UpdateWidgetDto dto)
        {
            return GetApiResponse(await widgetsService.UpdateWidget(widgetId, dto));
        }

        // Removes the widget everywhere -- every dashboard placing it loses the placement
        // too (see WidgetsService.DeleteWidget). The client is expected to confirm this
        // with the user before calling it.
        [HttpDelete("{widgetId}")]
        public async Task<IActionResult> DeleteWidget([FromRoute] string widgetId)
        {
            return GetApiResponse(await widgetsService.DeleteWidget(widgetId));
        }

        [HttpGet("entries")]
        public async Task<IActionResult> GetEntriesWidgets([FromQuery] string? trackerId)
        {
            return GetApiResponse(await widgetsService.GetEntriesWidgets(trackerId));
        }

        [HttpGet("entries/{entriesWidgetId}")]
        public async Task<IActionResult> GetEntriesWidget([FromRoute] string entriesWidgetId)
        {
            return GetApiResponse(await widgetsService.GetEntriesWidget(entriesWidgetId));
        }

        [HttpPost("entries")]
        public async Task<IActionResult> CreateEntriesWidget([FromBody] CreateEntriesWidgetDto dto)
        {
            return GetApiResponse(await widgetsService.CreateEntriesWidget(dto));
        }

        [HttpPut("entries/{entriesWidgetId}")]
        public async Task<IActionResult> UpdateEntriesWidget([FromRoute] string entriesWidgetId, [FromBody] UpdateEntriesWidgetDto dto)
        {
            return GetApiResponse(await widgetsService.UpdateEntriesWidget(entriesWidgetId, dto));
        }

        [HttpDelete("entries/{entriesWidgetId}")]
        public async Task<IActionResult> DeleteEntriesWidget([FromRoute] string entriesWidgetId)
        {
            return GetApiResponse(await widgetsService.DeleteEntriesWidget(entriesWidgetId));
        }
    }
}

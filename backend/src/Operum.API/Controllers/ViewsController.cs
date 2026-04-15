using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Views.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/trackers/{trackerId}/[controller]")]
    public class ViewsController(IViewsService viewsService) : BaseController
    {
        [HttpPost]
        public async Task<IActionResult> CreateView([FromBody] CreateViewDto view, [FromRoute] string trackerId)
        {
            return GetApiResponse(await viewsService.CreateView(trackerId, view));
        }

        [HttpGet("{viewId}")]
        public async Task<IActionResult> GetView([FromRoute] string trackerId, [FromRoute] string viewId)
        {
            return GetApiResponse(await viewsService.GetView(trackerId, viewId));
        }

        [HttpGet]
        public async Task<IActionResult> GetViewList([FromRoute] string trackerId)
        {
            return GetApiResponse(await viewsService.GetViewList(trackerId));
        }

        [HttpPut("{viewId}")]
        public async Task<IActionResult> UpdateView([FromRoute] string trackerId, [FromRoute] string viewId, [FromBody] UpdateViewDto view)
        {
            return GetApiResponse(await viewsService.UpdateView(trackerId, viewId, view));
        }

        [HttpDelete("{viewId}")]
        public async Task<IActionResult> DeleteView([FromRoute] string trackerId, [FromRoute] string viewId)
        {
            return GetApiResponse(await viewsService.DeleteView(trackerId, viewId));
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderViews([FromRoute] string trackerId, [FromBody] ReorderViewsDto reorderViews)
        {
            return GetApiResponse(await viewsService.ReorderViews(trackerId, reorderViews));
        }
    }
}

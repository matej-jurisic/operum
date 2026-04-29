using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Dashboard.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController(IDashboardService dashboardService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetDashboards()
        {
            return GetApiResponse(await dashboardService.GetDashboards());
        }

        [HttpPost]
        public async Task<IActionResult> CreateDashboard([FromBody] CreateDashboardDto dto)
        {
            return GetApiResponse(await dashboardService.CreateDashboard(dto));
        }

        [HttpGet("{dashboardId}")]
        public async Task<IActionResult> GetDashboard([FromRoute] string dashboardId)
        {
            return GetApiResponse(await dashboardService.GetDashboard(dashboardId));
        }

        [HttpDelete("{dashboardId}")]
        public async Task<IActionResult> DeleteDashboard([FromRoute] string dashboardId)
        {
            return GetApiResponse(await dashboardService.DeleteDashboard(dashboardId));
        }

        [HttpGet("{dashboardId}/analytics")]
        public async Task<IActionResult> GetDashboardAnalytics([FromRoute] string dashboardId)
        {
            return GetApiResponse(await dashboardService.GetDashboardAnalytics(dashboardId));
        }

        [HttpPost("{dashboardId}/items")]
        public async Task<IActionResult> AddDashboardItem([FromRoute] string dashboardId, [FromBody] AddDashboardItemDto dto)
        {
            return GetApiResponse(await dashboardService.AddDashboardItem(dashboardId, dto));
        }

        [HttpDelete("{dashboardId}/items/{itemId}")]
        public async Task<IActionResult> RemoveDashboardItem([FromRoute] string dashboardId, [FromRoute] string itemId)
        {
            return GetApiResponse(await dashboardService.RemoveDashboardItem(dashboardId, itemId));
        }

        [HttpPut("{dashboardId}/items/reorder")]
        public async Task<IActionResult> ReorderDashboardItems([FromRoute] string dashboardId, [FromBody] List<string> orderedItemIds)
        {
            return GetApiResponse(await dashboardService.ReorderDashboardItems(dashboardId, orderedItemIds));
        }
    }
}

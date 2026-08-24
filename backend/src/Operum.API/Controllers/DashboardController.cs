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

        [HttpPut("{dashboardId}")]
        public async Task<IActionResult> UpdateDashboard([FromRoute] string dashboardId, [FromBody] UpdateDashboardDto dto)
        {
            return GetApiResponse(await dashboardService.UpdateDashboard(dashboardId, dto));
        }

        [HttpDelete("{dashboardId}")]
        public async Task<IActionResult> DeleteDashboard([FromRoute] string dashboardId)
        {
            return GetApiResponse(await dashboardService.DeleteDashboard(dashboardId));
        }

        [HttpGet("{dashboardId}/widgets")]
        public async Task<IActionResult> GetDashboardWidgets([FromRoute] string dashboardId)
        {
            return GetApiResponse(await dashboardService.GetDashboardWidgets(dashboardId));
        }

        [HttpPost("{dashboardId}/items")]
        public async Task<IActionResult> AddDashboardItem([FromRoute] string dashboardId, [FromBody] AddDashboardItemDto dto)
        {
            return GetApiResponse(await dashboardService.AddDashboardItem(dashboardId, dto));
        }

        [HttpPost("{dashboardId}/items/from-analytic")]
        public async Task<IActionResult> AddDashboardItemFromAnalytic([FromRoute] string dashboardId, [FromBody] AddDashboardItemFromAnalyticDto dto)
        {
            return GetApiResponse(await dashboardService.AddDashboardItemFromAnalytic(dashboardId, dto));
        }

        [HttpDelete("{dashboardId}/items/{itemId}")]
        public async Task<IActionResult> RemoveDashboardItem([FromRoute] string dashboardId, [FromRoute] string itemId)
        {
            return GetApiResponse(await dashboardService.RemoveDashboardItem(dashboardId, itemId));
        }

        [HttpPut("{dashboardId}/layout")]
        public async Task<IActionResult> UpdateDashboardLayout([FromRoute] string dashboardId, [FromBody] UpdateDashboardLayoutDto dto)
        {
            return GetApiResponse(await dashboardService.UpdateDashboardLayout(dashboardId, dto));
        }
    }
}

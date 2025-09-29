using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Service.Services.Analytics;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController(IAnalyticsService analyticsService) : BaseController
    {
        [HttpGet("admin-analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAnalyticList()
        {
            return GetApiResponse(await analyticsService.GetAnalyticList());
        }

        [HttpGet]
        public async Task<IActionResult> GetPublicAnalyticList()
        {
            return GetApiResponse(await analyticsService.GetPublicAnalyticList());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAnalytic([FromBody] CreateAnalyticRequestDto createAnalytic)
        {
            return GetApiResponse(await analyticsService.CreateAnalytic(createAnalytic));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAnalytic([FromRoute] string id)
        {
            return GetApiResponse(await analyticsService.DeleteAnalytic(id));
        }
    }
}

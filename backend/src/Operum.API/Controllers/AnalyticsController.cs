using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController(IAnalyticsService analyticsService) : BaseController
    {
        [HttpGet]
        public IActionResult GetAnanlyticConfig()
        {
            return GetApiResponse(analyticsService.GetAnalyticConfig());
        }

        // Calculates a chart definition once against live data without saving it -- the
        // Explore page. Authorized: unlike the catalog lookup above, this reads the user's
        // trackers and entries.
        [Authorize]
        [HttpPost("evaluate")]
        public async Task<IActionResult> Evaluate([FromBody] EvaluateWidgetDto dto)
        {
            return GetApiResponse(await analyticsService.Evaluate(dto));
        }
    }
}

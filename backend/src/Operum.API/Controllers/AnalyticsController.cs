using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Service.Services.Analytics;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/trackers/{trackerId}/[controller]")]
    public class AnalyticsController(IAnalyticsService analyticsService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetTrackerAnalytics([FromRoute] string trackerId)
        {
            return GetApiResponse(await analyticsService.GetTrackerAnalytics(trackerId));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CorrelationInsightsController(ICorrelationInsightService correlationInsightService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetInsights()
        {
            return GetApiResponse(await correlationInsightService.GetInsights());
        }

        // Recomputes synchronously rather than queuing a job: a user's own tracker data is
        // small enough that this finishes well within a normal request.
        [HttpPost("run")]
        public async Task<IActionResult> RunNow(CancellationToken ct)
        {
            return GetApiResponse(await correlationInsightService.RunNow(ct));
        }

        [HttpPost("{insightId}/dismiss")]
        public async Task<IActionResult> Dismiss([FromRoute] string insightId)
        {
            return GetApiResponse(await correlationInsightService.Dismiss(insightId));
        }
    }
}

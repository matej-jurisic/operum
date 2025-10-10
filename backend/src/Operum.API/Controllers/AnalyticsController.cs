using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
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
    }
}

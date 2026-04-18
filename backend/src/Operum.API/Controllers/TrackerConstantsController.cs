using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.TrackerConstants.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/trackers/{trackerId}/constants")]
    public class TrackerConstantsController(ITrackerConstantsService trackerConstantsService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetConstantList([FromRoute] string trackerId)
        {
            return GetApiResponse(await trackerConstantsService.GetConstantList(trackerId));
        }

        [HttpGet("{constantId}")]
        public async Task<IActionResult> GetConstant([FromRoute] string trackerId, [FromRoute] string constantId)
        {
            return GetApiResponse(await trackerConstantsService.GetConstant(trackerId, constantId));
        }

        [HttpPost]
        public async Task<IActionResult> CreateConstant([FromRoute] string trackerId, [FromBody] CreateTrackerConstantDto dto)
        {
            return GetApiResponse(await trackerConstantsService.CreateConstant(trackerId, dto));
        }

        [HttpPut("{constantId}")]
        public async Task<IActionResult> UpdateConstant([FromRoute] string trackerId, [FromRoute] string constantId, [FromBody] UpdateTrackerConstantDto dto)
        {
            return GetApiResponse(await trackerConstantsService.UpdateConstant(trackerId, constantId, dto));
        }

        [HttpDelete("{constantId}")]
        public async Task<IActionResult> DeleteConstant([FromRoute] string trackerId, [FromRoute] string constantId)
        {
            return GetApiResponse(await trackerConstantsService.DeleteConstant(trackerId, constantId));
        }
    }
}

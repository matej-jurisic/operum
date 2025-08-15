using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Service.Services.Trackers;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrackersController(ITrackersService trackerService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetTrackerList()
        {
            return GetApiResponse(await trackerService.GetTrackerList());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTracker([FromRoute] string id)
        {
            return GetApiResponse(await trackerService.GetTracker(id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTracker(CreateTrackerDto tracker)
        {
            return GetApiResponse(await trackerService.CreateTracker(tracker));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTracker([FromRoute] string id, UpdateTrackerDto tracker)
        {
            return GetApiResponse(await trackerService.UpdateTracker(id, tracker));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTracker(string id)
        {
            return GetApiResponse(await trackerService.DeleteTracker(id));
        }

        [HttpGet("{id}/analytics")]
        public async Task<IActionResult> GetTrackerAnalytics([FromRoute] string id)
        {
            return GetApiResponse(await trackerService.GetTrackerAnalytics(id));
        }
    }
}

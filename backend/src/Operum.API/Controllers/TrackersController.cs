using Microsoft.AspNetCore.Mvc;
using Operum.Model.DTOs.Trackers;
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
        public async Task<IActionResult> GeTracker(string id)
        {
            return GetApiResponse(await trackerService.GetTracker(id));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTracker(CreateTrackerDto tracker)
        {
            return GetApiResponse(await trackerService.CreateTracker(tracker));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTracker(UpdateTrackerDto tracker)
        {
            return GetApiResponse(await trackerService.UpdateTracker(tracker));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTracker(string id)
        {
            return GetApiResponse(await trackerService.DeleteTracker(id));
        }
    }
}

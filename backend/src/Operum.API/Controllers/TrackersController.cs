using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.Constants;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Service.Services.Trackers;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrackersController(ITrackersService trackerService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetTrackerList([FromQuery] string filter = TrackerFilters.Owned)
        {
            return GetApiResponse(await trackerService.GetTrackerList(filter));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-templates")]
        public async Task<IActionResult> GetAllTemplateTrackerList()
        {
            return GetApiResponse(await trackerService.GetAllTemplateTrackerList());
        }

        [HttpGet("templates")]
        public async Task<IActionResult> GetPublicTemplateTrackerList()
        {
            return GetApiResponse(await trackerService.GetPublicTemplateTrackerList());
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

        [HttpPut("{id}/default-view")]
        public async Task<IActionResult> UpdateDefaultView([FromRoute] string id, [FromQuery] string? defaultViewId)
        {
            return GetApiResponse(await trackerService.UpdateDefaultView(id, defaultViewId));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTracker(string id)
        {
            return GetApiResponse(await trackerService.DeleteTracker(id));
        }

        [HttpGet("{trackerId}/analytics")]
        public async Task<IActionResult> GetTrackerAnalytics([FromRoute] string trackerId, [FromQuery] string? viewId)
        {
            return GetApiResponse(await trackerService.GetTrackerAnalytics(trackerId, viewId));
        }

        [HttpPost("{trackerId}/users")]
        public async Task<IActionResult> AddUserToTracker([FromRoute] string trackerId, [FromBody] ModifyUserTrackerDto request)
        {
            return GetApiResponse(await trackerService.AddUserToTracker(trackerId, request));
        }

        [HttpGet("{trackerId}/users")]
        public async Task<IActionResult> GetApplicationUserTrackerList([FromRoute] string trackerId)
        {
            return GetApiResponse(await trackerService.GetApplicationUserTrackerList(trackerId));
        }

        [HttpDelete("{trackerId}/users")]
        public async Task<IActionResult> RemoveUserFromTracker([FromRoute] string trackerId, [FromBody] ModifyUserTrackerDto request)
        {
            return GetApiResponse(await trackerService.RemoveUserFromTracker(trackerId, request));
        }
    }
}

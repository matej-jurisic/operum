using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.Constants;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Service.Interfaces;

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

        [HttpGet("{trackerId}")]
        public async Task<IActionResult> GetTracker([FromRoute] string trackerId)
        {
            return GetApiResponse(await trackerService.GetTracker(trackerId));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTracker(CreateTrackerDto tracker)
        {
            return GetApiResponse(await trackerService.CreateTracker(tracker));
        }

        [HttpPut("{trackerId}")]
        public async Task<IActionResult> UpdateTracker([FromRoute] string trackerId, UpdateTrackerDto tracker)
        {
            return GetApiResponse(await trackerService.UpdateTracker(trackerId, tracker));
        }

        [HttpPut("{trackerId}/default-view")]
        public async Task<IActionResult> UpdateDefaultView([FromRoute] string trackerId, [FromBody] string? viewId)
        {
            return GetApiResponse(await trackerService.UpdateDefaultView(trackerId, viewId));
        }

        [HttpDelete("{trackerId}")]
        public async Task<IActionResult> DeleteTracker(string trackerId)
        {
            return GetApiResponse(await trackerService.DeleteTracker(trackerId));
        }

        [HttpPost("{trackerId}/users")]
        public async Task<IActionResult> AddUserToTracker([FromRoute] string trackerId, [FromBody] AddUserToTrackerDto request)
        {
            return GetApiResponse(await trackerService.AddUserToTracker(trackerId, request));
        }

        [HttpGet("{trackerId}/users")]
        public async Task<IActionResult> GetApplicationUserTrackerList([FromRoute] string trackerId)
        {
            return GetApiResponse(await trackerService.GetApplicationUserTrackerList(trackerId));
        }

        [HttpDelete("{trackerId}/users")]
        public async Task<IActionResult> RemoveUserFromTracker([FromRoute] string trackerId, [FromBody] RemoveUserFromTrackerDto request)
        {
            return GetApiResponse(await trackerService.RemoveUserFromTracker(trackerId, request));
        }

        [HttpPut("{trackerId}/users")]
        public async Task<IActionResult> UpdateCollaboratorPermissions([FromRoute] string trackerId, [FromBody] UpdateCollaboratorPermissionsDto request)
        {
            return GetApiResponse(await trackerService.UpdateCollaboratorPermissions(trackerId, request));
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderTrackers([FromBody] ReorderTrackersDto dto)
        {
            return GetApiResponse(await trackerService.ReorderTrackers(dto));
        }
    }
}

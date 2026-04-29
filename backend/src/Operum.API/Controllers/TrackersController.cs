using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.Constants;
using Operum.Model.DTOs.Analytics.Requests;
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
        public async Task<IActionResult> UpdateDefaultView([FromRoute] string trackerId, [FromBody] List<string>? viewIds)
        {
            return GetApiResponse(await trackerService.UpdateDefaultView(trackerId, viewIds));
        }

        [HttpDelete("{trackerId}")]
        public async Task<IActionResult> DeleteTracker(string trackerId)
        {
            return GetApiResponse(await trackerService.DeleteTracker(trackerId));
        }

        [HttpGet("{trackerId}/analytics")]
        public async Task<IActionResult> GetTrackerAnalytics([FromRoute] string trackerId, [FromQuery] List<string> viewId)
        {
            return GetApiResponse(await trackerService.GetTrackerAnalytics(trackerId, viewId));
        }

        [HttpGet("{trackerId}/analytics/summary")]
        public async Task<IActionResult> GetTrackerAnalyticsSummary([FromRoute] string trackerId)
        {
            return GetApiResponse(await trackerService.GetTrackerAnalyticsSummary(trackerId));
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

        [HttpPost("{trackerId}/analytics")]
        public async Task<IActionResult> AddTrackerAnalytic([FromRoute] string trackerId, [FromBody] CreateAnalyticDto request)
        {
            return GetApiResponse(await trackerService.AddAnalytic(trackerId, request));
        }

        [HttpDelete("{trackerId}/analytics/{trackerAnalyticId}")]
        public async Task<IActionResult> RemoveTrackerAnalytic([FromRoute] string trackerId, [FromRoute] string trackerAnalyticId)
        {
            return GetApiResponse(await trackerService.RemoveAnalytic(trackerId, trackerAnalyticId));
        }

        [HttpPut("{trackerId}/analytics/reorder")]
        public async Task<IActionResult> ReorderTrackerAnalytics([FromRoute] string trackerId, [FromBody] ReorderAnalyticsDto reorderAnalyticsDto)
        {
            return GetApiResponse(await trackerService.ReorderAnalytics(trackerId, reorderAnalyticsDto));
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderTrackers([FromBody] ReorderTrackersDto dto)
        {
            return GetApiResponse(await trackerService.ReorderTrackers(dto));
        }
    }
}

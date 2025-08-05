using Microsoft.AspNetCore.Mvc;
using Operum.Model.DTOs.Entry;
using Operum.Model.DTOs.Fields;
using Operum.Model.DTOs.Trackers;
using Operum.Service.Services.Analytics;
using Operum.Service.Services.Entries;
using Operum.Service.Services.Fields;
using Operum.Service.Services.Trackers;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrackersController(ITrackersService trackerService, IFieldsService fieldsService, IEntriesService entriesService, IAnalyticsService analyticsService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetTrackerList()
        {
            return GetApiResponse(await trackerService.GetTrackerList());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTracker(string id)
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

        [HttpGet("{trackerId}/fields")]
        public async Task<IActionResult> GetFieldList(string trackerId)
        {
            return GetApiResponse(await fieldsService.GetFieldList(trackerId));
        }

        [HttpGet("fields/{fieldId}")]
        public async Task<IActionResult> GetField(string fieldId)
        {
            return GetApiResponse(await fieldsService.GetField(fieldId));
        }

        [HttpPost("{trackerId}/fields")]
        public async Task<IActionResult> CreateField([FromRoute] string trackerId, CreateFieldDto field)
        {
            return GetApiResponse(await fieldsService.CreateField(trackerId, field));
        }

        [HttpDelete("fields/{fieldId}")]
        public async Task<IActionResult> DeleteField([FromRoute] string fieldId)
        {
            return GetApiResponse(await fieldsService.DeleteField(fieldId));
        }

        [HttpPut("fields/{fieldId}")]
        public async Task<IActionResult> UpdateField([FromRoute] string fieldId, UpdateFieldDto field)
        {
            return GetApiResponse(await fieldsService.UpdateField(fieldId, field));
        }

        [HttpPost("{trackerId}/entries")]
        public async Task<IActionResult> CreateEntry([FromRoute] string trackerId, CreateEntryDto entry)
        {
            return GetApiResponse(await entriesService.CreateEntry(trackerId, entry));
        }

        [HttpGet("{trackerId}/entries")]
        public async Task<IActionResult> GetEntries([FromRoute] string trackerId)
        {
            return GetApiResponse(await entriesService.GetEntries(trackerId));
        }

        [HttpGet("{trackerId}/entries/{entryId}")]
        public async Task<IActionResult> GetEntry([FromRoute] string trackerId, [FromRoute] string entryId)
        {
            return GetApiResponse(await entriesService.GetEntry(trackerId, entryId));
        }

        [HttpPut("{trackerId}/entries/{entryId}")]
        public async Task<IActionResult> UpdateEntry([FromRoute] string trackerId, [FromRoute] string entryId, UpdateEntryDto entry)
        {
            return GetApiResponse(await entriesService.UpdateEntry(trackerId, entryId, entry));
        }

        [HttpDelete("{trackerId}/entries/{entryId}")]
        public async Task<IActionResult> DeleteEntry([FromRoute] string trackerId, [FromRoute] string entryId)
        {
            return GetApiResponse(await entriesService.DeleteEntry(trackerId, entryId));
        }

        [HttpGet("{trackerId}/fields/{fieldId}/analytics/numeric")]
        public async Task<IActionResult> GetSingleFieldNumericAnalytics([FromRoute] string trackerId, [FromRoute] string fieldId)
        {
            return GetApiResponse(await analyticsService.GetSingleFieldNumericAnalytics(trackerId, fieldId));
        }
    }
}

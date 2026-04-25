using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/trackers/{trackerId}/[controller]")]
    public class EntriesController(IEntriesService entriesService) : BaseController
    {
        [HttpPost]
        public async Task<IActionResult> CreateEntry([FromRoute] string trackerId, [FromBody] CreateEntryDto entry)
        {
            return GetApiResponse(await entriesService.CreateEntry(trackerId, entry));
        }

        [HttpGet]
        public async Task<IActionResult> GetEntries(
            [FromRoute] string trackerId,
            [FromQuery] List<string> viewId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            return GetApiResponse(await entriesService.GetEntries(trackerId, viewId, page, pageSize));
        }

        [HttpGet("{entryId}")]
        public async Task<IActionResult> GetEntry([FromRoute] string trackerId, [FromRoute] string entryId)
        {
            return GetApiResponse(await entriesService.GetEntry(trackerId, entryId));
        }

        [HttpPut("{entryId}")]
        public async Task<IActionResult> UpdateEntry([FromRoute] string trackerId, [FromRoute] string entryId, [FromBody] UpdateEntryDto entry)
        {
            return GetApiResponse(await entriesService.UpdateEntry(trackerId, entryId, entry));
        }

        [HttpDelete("{entryId}")]
        public async Task<IActionResult> DeleteEntry([FromRoute] string trackerId, [FromRoute] string entryId)
        {
            return GetApiResponse(await entriesService.DeleteEntry(trackerId, entryId));
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteEntries([FromRoute] string trackerId, [FromBody] DeleteEntriesDto deleteRequest)
        {
            return GetApiResponse(await entriesService.DeleteEntries(trackerId, deleteRequest.EntryIds));
        }

        [HttpPost("batch")]
        public async Task<IActionResult> BatchEntries([FromRoute] string trackerId, [FromBody] BatchEntriesDto batch)
        {
            return GetApiResponse(await entriesService.BatchEntries(trackerId, batch));
        }

        [HttpPost("recalculate")]
        public async Task<IActionResult> RecalculateEntries([FromRoute] string trackerId, [FromBody] RecalculateEntriesDto request)
        {
            return GetApiResponse(await entriesService.RecalculateEntries(trackerId, request.EntryIds));
        }

        [HttpPost("import-csv")]
        public async Task<IActionResult> ImportCsv([FromRoute] string trackerId, [FromForm] ImportEntriesDto file)
        {
            return GetApiResponse(await entriesService.ImportEntriesFromCsv(trackerId, file.File));
        }

        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportCsv([FromRoute] string trackerId, [FromQuery] List<string> viewId)
        {
            return GetApiFileResponse(await entriesService.ExportEntriesToCsv(trackerId, viewId));
        }
    }
}

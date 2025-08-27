using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Entry.Requests;
using Operum.Service.Services.Entries;

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
        public async Task<IActionResult> GetEntries([FromRoute] string trackerId)
        {
            return GetApiResponse(await entriesService.GetEntries(trackerId));
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

        [HttpPost("import-csv")]
        public async Task<IActionResult> ImportCsv([FromRoute] string trackerId, [FromForm] ImportEntriesDto file)
        {
            return GetApiResponse(await entriesService.ImportEntriesFromCsv(trackerId, file.File));
        }
    }
}

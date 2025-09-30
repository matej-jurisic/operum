using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Operum.Model.Common;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Entry;
using Operum.Model.DTOs.Entry.Requests;

namespace Operum.Service.Services.Entries
{
    public interface IEntriesService
    {
        public Task<Result<EntryDto>> CreateEntry(string trackerId, CreateEntryDto entry);
        public Task<Result<List<EntryDto>>> GetEntries(string trackerId, string? viewId);
        public Task<Result<EntryDto>> GetEntry(string trackerId, string entryId);
        public Task<Result<EntryDto>> UpdateEntry(string trackerId, string entryId, UpdateEntryDto updateEntry);
        public Task<Result> DeleteEntry(string trackerId, string entryId);
        public Task<Result> DeleteEntries(string trackerId, List<string> entryIdList);
        public Task<Result<List<EntryDto>>> ImportEntriesFromCsv(string trackerId, IFormFile file);
        public Task<Result<FileContentResult>> ExportEntriesToCsv(string trackerId, string? viewId = null);
    }
}

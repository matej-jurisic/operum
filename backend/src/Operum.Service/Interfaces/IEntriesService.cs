using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Operum.Model.Common;
using Operum.Model.DTOs.Entries;
using Operum.Model.DTOs.Entries.Requests;

namespace Operum.Service.Interfaces
{
    public interface IEntriesService
    {
        public Task<Result<EntryDto>> CreateEntry(string trackerId, CreateEntryDto entry);
        public Task<Result<PagedResult<EntryDto>>> GetEntries(string trackerId, string? viewId, int page, int pageSize);
        public Task<Result<EntryDto>> GetEntry(string trackerId, string entryId);
        public Task<Result<EntryDto>> UpdateEntry(string trackerId, string entryId, UpdateEntryDto updateEntry);
        public Task<Result> DeleteEntry(string trackerId, string entryId);
        public Task<Result> DeleteEntries(string trackerId, EntrySelectionDto selection);
        public Task<Result<List<EntryDto>>> ImportEntriesFromCsv(string trackerId, IFormFile file);
        public Task<Result<FileContentResult>> ExportEntriesToCsv(string trackerId, string? viewId);
        public Task<Result> RecalculateEntries(string trackerId, EntrySelectionDto selection);
        public Task<Result> BatchEntries(string trackerId, BatchEntriesDto batch);
    }
}

using Microsoft.AspNetCore.Http;
using Operum.Model.Common;
using Operum.Model.DTOs.Entry;
using Operum.Model.DTOs.Entry.Requests;

namespace Operum.Service.Services.Entries
{
    public interface IEntriesService
    {
        public Task<ServiceResponse<EntryDto>> CreateEntry(string trackerId, CreateEntryDto entry);
        public Task<ServiceResponse<List<EntryDto>>> GetEntries(string trackerId);
        public Task<ServiceResponse<EntryDto>> GetEntry(string trackerId, string entryId);
        public Task<ServiceResponse<EntryDto>> UpdateEntry(string trackerId, string entryId, UpdateEntryDto updateEntry);
        public Task<ServiceResponse> DeleteEntry(string trackerId, string entryId);
        public Task<ServiceResponse> DeleteEntries(string trackerId, List<string> entryIdList);
        public Task<ServiceResponse<List<EntryDto>>> ImportEntriesFromCsv(string trackerId, IFormFile file);
    }
}

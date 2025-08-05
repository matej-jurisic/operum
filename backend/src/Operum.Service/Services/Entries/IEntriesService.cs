using Operum.Model.Common;
using Operum.Model.DTOs.Entry;

namespace Operum.Service.Services.Entries
{
    public interface IEntriesService
    {
        public Task<ServiceResponse<EntryDto>> CreateEntry(string trackerId, CreateEntryDto entry);
        public Task<ServiceResponse<List<EntryDto>>> GetEntries(string trackerId);
        public Task<ServiceResponse<EntryDto>> GetEntry(string trackerId, string entryId);
        public Task<ServiceResponse<EntryDto>> UpdateEntry(string trackerId, string entryId, UpdateEntryDto updateEntry);
        public Task<ServiceResponse<EntryDto>> DeleteEntry(string trackerId, string entryId);
    }
}

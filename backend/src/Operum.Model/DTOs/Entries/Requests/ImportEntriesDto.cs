using Microsoft.AspNetCore.Http;

namespace Operum.Model.DTOs.Entries.Requests
{
    public class ImportEntriesDto
    {
        public IFormFile File { get; set; } = null!;
    }
}

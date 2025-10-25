using Microsoft.AspNetCore.Http;

namespace Operum.Model.DTOs.Entries.Requests
{
    public class ImportEntriesDto
    {
        public required IFormFile File { get; set; } = null!;
    }
}

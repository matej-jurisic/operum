namespace Operum.Model.DTOs.Entries.Requests
{
    public class DeleteEntriesDto
    {
        public required List<string> EntryIds { get; set; } = [];
    }
}

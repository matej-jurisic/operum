namespace Operum.Model.DTOs.Entries.Requests
{
    public class RecalculateEntriesDto
    {
        public required List<string> EntryIds { get; set; } = [];
    }
}

namespace Operum.Model.DTOs.Entries.Requests
{
    public class BatchEntriesDto
    {
        public List<Dictionary<string, string?>> Creates { get; set; } = [];
        public List<BatchUpdateEntryDto> Updates { get; set; } = [];
        public List<string> Deletes { get; set; } = [];
    }

    public class BatchUpdateEntryDto
    {
        public required string EntryId { get; set; }
        public required Dictionary<string, string?> FieldValues { get; set; } = [];
    }
}

namespace Operum.Model.DTOs.Entries
{
    public class EntryRevisionDto
    {
        public string Id { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string ChangedByUserName { get; set; } = string.Empty;
        public List<EntryRevisionFieldChangeDto> Changes { get; set; } = [];
    }

    public class EntryRevisionFieldChangeDto
    {
        public string FieldId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}

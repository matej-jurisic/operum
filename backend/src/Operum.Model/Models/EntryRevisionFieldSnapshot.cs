namespace Operum.Model.Models
{
    // One field's display value at the time an EntryRevision was written. Plain POCO, JSON-
    // serialized into EntryRevision.Snapshot; not an EF entity.
    public class EntryRevisionFieldSnapshot
    {
        public string FieldId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}

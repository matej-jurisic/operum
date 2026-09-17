namespace Operum.Model.DTOs.Dashboard
{
    // Serialized camelCase; written by hand rather than through the controller's JSON formatting.
    public class EntriesWidgetConfigDto
    {
        // Deduped, in display order; empty means every field.
        public List<string> ColumnFieldIds { get; set; } = [];
    }
}

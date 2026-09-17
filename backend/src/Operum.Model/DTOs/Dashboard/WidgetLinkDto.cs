namespace Operum.Model.DTOs.Dashboard
{
    // FieldByQuery is keyed by clause index on the wire, then rewritten to SlotId on save (see
    // SaveFilterItemDto).
    public class WidgetLinkDto
    {
        public string ItemId { get; set; } = string.Empty;
        public string TrackerId { get; set; } = string.Empty;
        public Dictionary<string, string> FieldByQuery { get; set; } = [];
    }
}

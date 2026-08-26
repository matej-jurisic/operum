namespace Operum.Model.DTOs.Widgets
{
    // The Widget Library's view of an Entries widget's definition -- just which tracker it
    // reads from. Distinct from Operum.Model.DTOs.Dashboard.EntriesWidgetDto, which is the
    // resolved-for-rendering shape a placement returns (tracker name/color/icon/columns).
    public class EntriesWidgetDefinitionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
    }
}

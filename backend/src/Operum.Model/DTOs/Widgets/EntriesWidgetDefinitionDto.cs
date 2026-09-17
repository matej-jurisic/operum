namespace Operum.Model.DTOs.Widgets
{
    // Distinct from Operum.Model.DTOs.Dashboard.EntriesWidgetDto, the resolved-for-rendering
    // shape a placement returns.
    public class EntriesWidgetDefinitionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
    }
}

namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.QuickAdd widget: which tracker its
    // button opens the quick-add entry dialog for. Serialized with camelCase to match
    // every other DTO the client reads, since this one is written by hand rather than
    // through the controller's own JSON formatting.
    public class QuickAddWidgetConfigDto
    {
        public string TrackerId { get; set; } = string.Empty;
    }
}

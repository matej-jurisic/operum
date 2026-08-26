namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.Entries widget: which tracker its table
    // reads entries from, and how it's filtered. Serialized with camelCase to match every
    // other DTO the client reads, since this one is written by hand rather than through the
    // controller's own JSON formatting.
    //
    // At most one of ViewId/LinkedViewWidgetId is ever set — ViewId fixes the filter,
    // LinkedViewWidgetId instead follows a DashboardWidgetTypes.View item's own selection,
    // the same duality a DashboardItemSource has.
    public class EntriesWidgetConfigDto
    {
        public string TrackerId { get; set; } = string.Empty;
        public string? ViewId { get; set; }
        public string? LinkedViewWidgetId { get; set; }
    }
}

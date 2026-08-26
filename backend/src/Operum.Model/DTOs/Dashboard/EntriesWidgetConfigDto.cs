namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.Entries item: only how this placement
    // is filtered. Serialized with camelCase to match every other DTO the client reads,
    // since this one is written by hand rather than through the controller's own JSON
    // formatting. Which tracker the table reads from lives on the item's EntriesWidget
    // instead -- a placement can't change that without becoming a different widget.
    //
    // At most one of ViewId/LinkedViewWidgetId is ever set — ViewId fixes the filter,
    // LinkedViewWidgetId instead follows a DashboardWidgetTypes.View item's own selection,
    // the same duality a DashboardItemSource has.
    public class EntriesWidgetConfigDto
    {
        public string? ViewId { get; set; }
        public string? LinkedViewWidgetId { get; set; }
    }
}

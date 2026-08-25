namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.View widget: which tracker its dropdown
    // lists views for, and which of them is currently selected. Serialized with camelCase to
    // match every other DTO the client reads, since this one is written by hand rather than
    // through the controller's own JSON formatting.
    //
    // ViewId is the persisted selection, not just an initial default — changing the dropdown
    // rewrites it (see DashboardService.SetViewWidgetSelection), so every source linked to
    // this widget re-filters the same way for every future load, not just this browser
    // session.
    public class ViewWidgetConfigDto
    {
        public string TrackerId { get; set; } = string.Empty;
        public string? ViewId { get; set; }
    }
}

namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Changes what a DashboardWidgetTypes.View item's dropdown is currently set to. Persisted
    // on the item itself (see ViewWidgetConfigDto), so every source linked to it re-filters
    // for every viewer from here on, not just this browser session.
    public class SetViewWidgetSelectionDto
    {
        // Null clears the filter back to "All entries".
        public string? ViewId { get; set; }
    }
}

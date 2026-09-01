namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.Entries item: only how this placement
    // is filtered. Serialized with camelCase to match every other DTO the client reads,
    // since this one is written by hand rather than through the controller's own JSON
    // formatting. Which tracker the table reads from lives on the item's EntriesWidget
    // instead -- a placement can't change that without becoming a different widget.
    //
    // ViewId is the fixed tracker view the table reads through -- its base filter and the
    // columns it shows.
    public class EntriesWidgetConfigDto
    {
        public string? ViewId { get; set; }
    }
}

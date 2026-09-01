namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.Entries item: only which columns this
    // placement shows. Serialized with camelCase to match every other DTO the client reads,
    // since this one is written by hand rather than through the controller's own JSON
    // formatting. Which tracker the table reads from lives on the item's EntriesWidget
    // instead -- a placement can't change that without becoming a different widget.
    //
    // ColumnFieldIds are the tracker's fields to show, already deduped and in display order.
    // Empty means every field, the same fallback the tracker page uses. How the table is
    // filtered is not stored here at all -- that comes only from the view selector widgets
    // this placement is linked to (see ViewSelectorWidgetConfigDto).
    public class EntriesWidgetConfigDto
    {
        public List<string> ColumnFieldIds { get; set; } = [];
    }
}

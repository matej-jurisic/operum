namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Changes what a DashboardWidgetTypes.ViewSelector item's dropdown is currently set to.
    // Persisted on the item's Config, so it's what every future load starts from -- not just
    // this session. Null clears the selection (every following widget falls back to its
    // fixed view alone).
    public class SetViewSelectorSelectionDto
    {
        public string? SelectedId { get; set; }
    }
}

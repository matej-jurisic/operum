namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Changes what a DashboardWidgetTypes.Filter item's preset dropdown is currently set
    // to. Persisted on the item's Config, so it's what every future load starts from -- not
    // just this session. Null clears the selection (every follower falls back to whatever
    // its own typed clauses/fixed view apply alone).
    public class SetFilterPresetDto
    {
        public string? SelectedPresetId { get; set; }
    }
}

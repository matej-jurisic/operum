namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Changes what a DashboardWidgetTypes.Filter item's clauses are currently set to.
    // Keyed by the pooled query id; persisted on the item's Config so it's what every future
    // load starts from -- not just this session. A missing or empty value leaves that clause
    // unapplied.
    public class SetFilterValuesDto
    {
        public Dictionary<string, string?> Values { get; set; } = [];
    }
}

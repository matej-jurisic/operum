namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.ViewSelector item. Serialized with
    // camelCase to match every other DTO the client reads, since this one is written by hand
    // rather than through the controller's own JSON formatting.
    //
    // OptionIds name the board's DashboardViews the dropdown offers, in order. SelectedId is
    // the persisted current selection -- changing it (see SetViewSelectorSelection) re-filters
    // every widget in Links for every future load, not just this session. Each link carries,
    // per following Analytic widget and per tracker it reads from, which of that tracker's
    // fields each clause runs against, keyed by the pooled query id -- the union of query ids
    // across every option.
    public class ViewSelectorWidgetConfigDto
    {
        public List<string> OptionIds { get; set; } = [];
        public string? SelectedId { get; set; }
        public List<ViewSelectorLinkDto> Links { get; set; } = [];
    }

    public class ViewSelectorLinkDto
    {
        public string ItemId { get; set; } = string.Empty;
        public string TrackerId { get; set; } = string.Empty;
        public Dictionary<string, string> FieldByQuery { get; set; } = [];
    }
}

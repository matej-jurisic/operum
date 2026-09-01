namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.Filter item. Serialized camelCase
    // like every other hand-serialized dashboard Config, since this one is written by hand
    // rather than through the controller's JSON formatting.
    //
    // Two independent facets, both narrowing every followed widget in Links/PresetLinks:
    //
    // Own typed clauses -- QueryIds is the widget's own ordered clause set (pooled Query
    // ids, see QueryPool), resolved from the clauses the editor sends; it owns these
    // outright rather than pointing at a named DashboardView. ValueByQuery is the persisted
    // current value per clause, keyed by pooled query id -- changing it (see
    // SetFilterValues) re-filters every follower for every future load. A clause with no
    // entry here (or an empty one) is left unapplied. Links carries, per followed
    // Analytic/Entries widget and per tracker it reads from, which field each of these
    // clauses runs against.
    //
    // Presets -- PresetIds names the board's DashboardViews this widget offers as quick-apply
    // presets, in order. SelectedPresetId is the persisted current selection -- changing it
    // (see SetFilterPreset) applies that preset's whole clause set (filters AND sorts) to
    // every follower in PresetLinks for every future load, exactly the way the old
    // stand-alone "view selector" widget applied its selected option; this is functionally
    // that widget, folded in here as a second facet. PresetLinks carries the same per
    // follower/tracker field mapping as Links, but keyed by the union of query ids across
    // every preset (DashboardViewQuery ids, already pooled -- no index rewrite needed the
    // way Links' clause-index keys require).
    public class FilterWidgetConfigDto
    {
        public List<string> QueryIds { get; set; } = [];
        public Dictionary<string, string?> ValueByQuery { get; set; } = [];
        public List<WidgetLinkDto> Links { get; set; } = [];

        public List<string> PresetIds { get; set; } = [];
        public string? SelectedPresetId { get; set; }
        public List<WidgetLinkDto> PresetLinks { get; set; } = [];
    }
}

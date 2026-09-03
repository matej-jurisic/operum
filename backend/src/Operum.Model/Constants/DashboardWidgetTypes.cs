namespace Operum.Model.Constants
{
    // What a dashboard item renders. An analytic chart is the original kind; the
    // discriminator exists so a widget that isn't a chart can share the same grid, the
    // same placement columns and the same endpoints instead of needing a table of its own.
    public static class DashboardWidgetTypes
    {
        public const string Analytic = "analytic";

        // A button that opens a tracker's quick-add entry dialog from the board. Carries no
        // analytic definition — just Config, a QuickAddWidgetConfigDto naming the tracker.
        public const string QuickAdd = "quickAdd";

        // A read-only table of one tracker's entries. Carries no analytic definition either
        // — just Config, an EntriesWidgetConfigDto naming the fields it shows as columns. How
        // it's filtered comes only from the filter widgets it's linked to.
        public const string Entries = "entries";

        // A board filter widget with two independent facets, both narrowing whichever
        // Analytic/Entries widgets it's linked to. First, it owns a set of filter clauses
        // with a value typed directly on the board (its own QueryIds/ValueByQuery/Links).
        // Second, it can offer a dropdown of the board's DashboardViews as quick-apply
        // presets (PresetIds/SelectedPresetId/PresetLinks) — picking one applies that view's
        // whole clause set (filters AND sorts) to its followers, same as the old standalone
        // "view selector" widget did before being folded in here. Carries no analytic
        // definition — just Config, a FilterWidgetConfigDto. A typed clause left blank is
        // simply not applied; a preset left unselected contributes nothing.
        public const string Filter = "filter";

        // A short line of user-entered text that reads as a section title rather than a
        // chart. Carries no tracker or analytic — just Config, a TextWidgetConfigDto.
        public const string Header = "header";

        // A bare visual rule with no config at all. The grid already lets widgets leave
        // deliberate empty space; this is what turns a gap into something that reads as a
        // dividing line instead of unfinished layout.
        public const string Divider = "divider";

        // A free-form block of user-entered text, for context that isn't any tracker's
        // data. Shares its Config shape (TextWidgetConfigDto) with Header.
        public const string Note = "note";

        // A panel that holds a sub-grid of other widgets, so a group of them can be moved,
        // resized and titled as one. Carries no tracker or analytic and no Config: its only
        // state is which items name it as their parent (DashboardItem.ParentItemId) and
        // their placement within it. A Container can never sit inside another Container.
        public const string Container = "container";

        public static readonly HashSet<string> All =
            [Analytic, QuickAdd, Entries, Filter, Header, Divider, Note, Container];

        public static bool IsValid(string type) => All.Contains(type);
    }
}

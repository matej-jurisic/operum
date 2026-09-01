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

        // A dropdown over a set of the board's DashboardViews ("Current Month", "All Time").
        // Carries no analytic definition — just Config, a ViewSelectorWidgetConfigDto holding
        // the option ids, the current selection, and per followed widget which of its
        // tracker's fields each clause runs against. Picking an option re-filters every widget
        // wired to it, on top of whatever fixed view that widget already reads through.
        public const string ViewSelector = "viewSelector";

        // A read-only table of one tracker's entries. Carries no analytic definition either
        // — just Config, an EntriesWidgetConfigDto naming the fixed ViewId it reads through
        // (for its columns and base filter).
        public const string Entries = "entries";

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

        public static readonly HashSet<string> All =
            [Analytic, QuickAdd, ViewSelector, Entries, Header, Divider, Note];

        public static bool IsValid(string type) => All.Contains(type);
    }
}

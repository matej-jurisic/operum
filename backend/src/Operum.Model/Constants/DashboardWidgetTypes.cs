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

        // A dropdown over one tracker's views. Carries no analytic definition either — just
        // Config, a ViewWidgetConfigDto naming the tracker and the currently selected view.
        // Any DashboardItemSource can point its LinkedViewWidgetId at one of these instead of
        // a fixed ViewId, so changing the dropdown here re-filters every source linked to it.
        public const string View = "view";

        // A read-only table of one tracker's entries. Carries no analytic definition either
        // — just Config, an EntriesWidgetConfigDto naming the tracker and, at most one of, a
        // fixed ViewId or a LinkedViewWidgetId to follow instead, the same duality a
        // DashboardItemSource has.
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
            [Analytic, QuickAdd, View, Entries, Header, Divider, Note];

        public static bool IsValid(string type) => All.Contains(type);
    }
}

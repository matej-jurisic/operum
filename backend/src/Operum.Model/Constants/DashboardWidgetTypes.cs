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

        public static readonly HashSet<string> All = [Analytic, QuickAdd, View, Entries];

        public static bool IsValid(string type) => All.Contains(type);
    }
}

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

        public static readonly HashSet<string> All = [Analytic, QuickAdd];

        public static bool IsValid(string type) => All.Contains(type);
    }
}

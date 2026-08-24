namespace Operum.Model.Constants
{
    // What a dashboard item renders. An analytic chart is the only kind today; the
    // discriminator exists so a widget that isn't a chart can share the same grid, the
    // same placement columns and the same endpoints instead of needing a table of its own.
    public static class DashboardWidgetTypes
    {
        public const string Analytic = "analytic";

        public static readonly HashSet<string> All = [Analytic];

        public static bool IsValid(string type) => All.Contains(type);
    }
}

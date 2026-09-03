namespace Operum.Model.Constants.Analytics
{
    public static class AnalyticTypes
    {
        public const string SingleValue = "Single Value";
        public const string LineChart = "Line Chart";
        public const string ScatterChart = "Scatter Chart";
        public const string Calendar = "Calendar";
        public const string Donut = "Donut Chart";
        public const string BarChart = "Bar Chart";

        // Synthetic result type for dashboard widgets that combine multiple sources into
        // one chart (see DashboardService.GetDashboardAnalytics). Never a persisted
        // Analytic.ResultType, so it's intentionally excluded from `All`/`IsValid`, which
        // gate real per-tracker Analytic creation.
        public const string Composed = "Composed Chart";

        public static readonly HashSet<string> All =
        [
            SingleValue, LineChart, ScatterChart, Calendar, Donut, BarChart
        ];

        public static bool IsValid(string op) => All.Contains(op);

        // Result types that have a merge path for combining any number of tracker sources
        // into a single widget. Line and bar merge into a Composed chart; a calendar just
        // unions its dated events.
        private static readonly HashSet<string> MultiSourceTypes = [LineChart, BarChart, Calendar];

        public static bool SupportsMultipleSources(string resultType) => MultiSourceTypes.Contains(resultType);

        // A calculation whose two sources each contribute one axis of a scatter plot, joined
        // on a shared match field. Unlike the merge types above it needs exactly two sources,
        // not "one or more", so it's gated separately (see WidgetsService.CreateWidget).
        public static bool RequiresPairedSources(string resultType, string code) =>
            resultType == ScatterChart && code == AnalyticCodes.CorrelationScatter;
    }
}

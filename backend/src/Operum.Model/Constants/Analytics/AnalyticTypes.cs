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

        // Single Value shown as progress toward a target (stored on the Widget). Widget
        // Library only, hence AnalyticDefinition.WidgetOnly.
        public const string Goal = "Goal";

        // Synthetic result type for multi-source dashboard widgets (see
        // DashboardService.GetDashboardAnalytics). Never a persisted Analytic.ResultType,
        // so intentionally excluded from `All`/`IsValid`.
        public const string Composed = "Composed Chart";

        public static readonly HashSet<string> All =
        [
            SingleValue, LineChart, ScatterChart, Calendar, Donut, BarChart, Goal
        ];

        public static bool IsValid(string op) => All.Contains(op);

        private static readonly HashSet<string> MultiSourceTypes = [LineChart, BarChart, Calendar];

        public static bool SupportsMultipleSources(string resultType) => MultiSourceTypes.Contains(resultType);

        // Needs exactly two sources (not "one or more" like MultiSourceTypes); see
        // WidgetsService.CreateWidget.
        public static bool RequiresPairedSources(string resultType, string code) =>
            resultType == ScatterChart && code == AnalyticCodes.CorrelationScatter;
    }
}

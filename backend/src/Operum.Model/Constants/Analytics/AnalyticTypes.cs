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
    }
}

namespace Operum.Model.Constants.Analytics
{
    public static class AnalyticTypes
    {
        public const string SingleValue = "Single Value";
        public const string LineChart = "Line Chart";
        public const string ScatterChart = "Scatter Chart";
        public const string Calendar = "Calendar";
        public const string Donut = "Donut Chart";

        public static readonly HashSet<string> All =
        [
            SingleValue, LineChart, ScatterChart, Calendar, Donut
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}

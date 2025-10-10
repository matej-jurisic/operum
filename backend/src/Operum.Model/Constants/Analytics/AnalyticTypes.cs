namespace Operum.Model.Constants.Analytics
{
    public static class AnalyticTypes
    {
        public const string SingleValue = "SingleValue";
        public const string NumericChart = "NumericChart";
        public const string ScatterPlot = "ScatterPlot";
        public const string Calendar = "Calendar";

        public static readonly HashSet<string> All =
        [
            SingleValue, NumericChart, ScatterPlot, Calendar
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}

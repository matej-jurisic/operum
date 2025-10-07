namespace Operum.Model.Constants
{
    public static class AnalyticResultTypes
    {
        public const string SingleValue = "SingleValue";
        public const string NumericChart = "NumericChart";
        public const string ScatterChart = "ScatterChart";
        public const string CalendarEvents = "CalendarEvents";

        private static readonly HashSet<string> All =
        [
            SingleValue, NumericChart, ScatterChart, CalendarEvents
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }

}

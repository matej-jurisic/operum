namespace Operum.Model.Constants.Analytics
{
    public static class AnalyticCodes
    {
        public const string Count = "Count";
        public const string Min = "Min";
        public const string Max = "Max";
        public const string Average = "Average";
        public const string Sum = "Sum";
        public const string StdDev = "Standard Deviation";

        public const string TrueCount = "True Count";
        public const string FalseCount = "False Count";
        public const string TruePercentage = "True Percentage";

        public const string AggregatedLineChart = "Aggregated LineChart";
        public const string CumulativeLineChart = "Cumulative LineChart";
        public const string LineChart = "LineChart";

        public const string ScatterPlot = "ScatterPlot";
        public const string Calendar = "Calendar";

        public static readonly HashSet<string> All =
        [
            Count, Min, Max, Average, Sum, StdDev,
            TrueCount, FalseCount, TruePercentage,
            AggregatedLineChart, CumulativeLineChart, LineChart,
            Calendar, ScatterPlot
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}

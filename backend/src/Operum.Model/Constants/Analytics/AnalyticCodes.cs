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

        public const string AggregatedSumLineChart = "Aggregated Sum";
        public const string CumulativeLineChart = "Cumulative Sum";
        public const string LineChart = "Line Chart";

        public const string ScatterChart = "Scatter Chart";
        public const string Calendar = "Calendar";
        public const string DonutChart = "Donut Chart";

        public static readonly HashSet<string> All =
        [
            Count, Min, Max, Average, Sum, StdDev,
            TrueCount, FalseCount, TruePercentage,
            AggregatedSumLineChart, CumulativeLineChart, LineChart,
            Calendar, ScatterChart, DonutChart
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}

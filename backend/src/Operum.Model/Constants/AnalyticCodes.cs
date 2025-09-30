namespace Operum.Model.Constants
{
    public static class AnalyticCodes
    {
        public const string Count = "Count";
        public const string Min = "Min";
        public const string Max = "Max";
        public const string Average = "Average";
        public const string Sum = "Sum";
        public const string StdDev = "StdDev";

        public const string TrueCount = "TrueCount";
        public const string FalseCount = "FalseCount";
        public const string TruePercentage = "TruePercentage";

        public const string AggregatedLineChart = "AggregatedLineChart";
        public const string CumulativeLineChart = "CumulativeLineChart";
        public const string LineChart = "LineChart";


        public static readonly HashSet<string> All =
        [
            Count, Min, Max, Average, Sum, StdDev, TrueCount, FalseCount, TruePercentage, AggregatedLineChart, CumulativeLineChart, LineChart
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}

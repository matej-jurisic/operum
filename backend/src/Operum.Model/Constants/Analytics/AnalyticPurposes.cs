namespace Operum.Model.Constants.Analytics
{
    public static class AnalyticPurposes
    {
        public const string Xaxis = "X-axis";
        public const string Yaxis = "Y-axis";
        public const string Value = "Value";
        public const string What = "What";
        public const string When = "When";

        public static readonly HashSet<string> All =
        [
            Xaxis, Yaxis, Value, When, What
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}

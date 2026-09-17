namespace Operum.Model.Constants.Analytics
{
    public static class AnalyticPurposes
    {
        public const string Xaxis = "X-axis";
        public const string Yaxis = "Y-axis";
        public const string Value = "Value";
        public const string What = "What";
        public const string When = "When";
        public const string Name = "Name";

        // Min/Max only, optional: field shown for the entry picked by Value.
        public const string Display = "Display";

        // Join key for correlation-scatter's two sources.
        public const string Match = "Match";

        public static readonly HashSet<string> All =
        [
            Xaxis, Yaxis, Value, When, What, Name, Match, Display
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}

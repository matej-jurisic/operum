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

        // The field two correlation-scatter sources are joined on: a point pairs the two
        // trackers' values for each match key they share.
        public const string Match = "Match";

        public static readonly HashSet<string> All =
        [
            Xaxis, Yaxis, Value, When, What, Name, Match
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}

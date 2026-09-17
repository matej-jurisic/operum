namespace Operum.Model.Constants.Analytics
{
    // Maps retired fused Line/Bar codes (e.g. "Daily") to their (Grouping, Code) replacement,
    // for the AddWidgetGrouping migration and old bookmarked Explore URLs.
    public static class LegacyLineBarCodes
    {
        public static readonly Dictionary<string, (string Grouping, string Code)> Map = new()
        {
            // Line
            ["Line Chart"] = (AnalyticGroupings.None, AnalyticCodes.RawValues),
            ["Aggregated Sum"] = (AnalyticGroupings.Exact, AnalyticCodes.Sum),
            ["Cumulative Sum"] = (AnalyticGroupings.Exact, AnalyticCodes.CumulativeSum),
            ["Daily"] = (AnalyticGroupings.Daily, AnalyticCodes.Sum),
            ["Weekly"] = (AnalyticGroupings.Weekly, AnalyticCodes.Sum),
            ["Monthly"] = (AnalyticGroupings.Monthly, AnalyticCodes.Sum),
            ["Yearly"] = (AnalyticGroupings.Yearly, AnalyticCodes.Sum),

            // Bar
            ["Count Bar Chart"] = (AnalyticGroupings.Exact, AnalyticCodes.Count),
            ["Sum Bar Chart"] = (AnalyticGroupings.Exact, AnalyticCodes.Sum),
            ["Average Bar Chart"] = (AnalyticGroupings.Exact, AnalyticCodes.Average),
            ["Daily Bar Chart"] = (AnalyticGroupings.Daily, AnalyticCodes.Sum),
            ["Weekly Bar Chart"] = (AnalyticGroupings.Weekly, AnalyticCodes.Sum),
            ["Monthly Bar Chart"] = (AnalyticGroupings.Monthly, AnalyticCodes.Sum),
            ["Yearly Bar Chart"] = (AnalyticGroupings.Yearly, AnalyticCodes.Sum),
        };

        public static (string? Grouping, string Code) Resolve(string? grouping, string code)
        {
            if (!string.IsNullOrEmpty(grouping))
                return (grouping, code);

            return Map.TryGetValue(code, out var pair) ? (pair.Grouping, pair.Code) : (grouping, code);
        }
    }
}

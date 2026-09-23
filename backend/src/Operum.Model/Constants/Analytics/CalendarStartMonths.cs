namespace Operum.Model.Constants.Analytics
{
    // Which month a calendar widget opens on. Null on a placement means automatic: the current
    // month if it has entries, else the next upcoming one, else the most recent past one.
    public static class CalendarStartMonths
    {
        public const string Current = "Current";
        public const string LatestPast = "LatestPast";
        public const string EarliestPast = "EarliestPast";
        public const string NextUpcoming = "NextUpcoming";
        public const string LatestUpcoming = "LatestUpcoming";

        public static readonly HashSet<string> All = [Current, LatestPast, EarliestPast, NextUpcoming, LatestUpcoming];

        public static bool IsValid(string value) => All.Contains(value);
    }
}

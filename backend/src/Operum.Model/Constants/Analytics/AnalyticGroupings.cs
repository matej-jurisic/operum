namespace Operum.Model.Constants.Analytics
{
    // Only Line and Bar charts carry a grouping; every other result type's calculation is
    // the aggregation code alone.
    public static class AnalyticGroupings
    {
        // No bucketing/aggregation: one mark per entry.
        public const string None = "None";

        // One bucket per distinct field value.
        public const string Exact = "Exact";

        // Date/datetime fields only; week bucket starts Monday.
        public const string Daily = "Daily";
        public const string Weekly = "Weekly";
        public const string Monthly = "Monthly";
        public const string Yearly = "Yearly";

        public static readonly HashSet<string> All =
        [
            None, Exact, Daily, Weekly, Monthly, Yearly
        ];

        public static readonly HashSet<string> DateBuckets =
        [
            Daily, Weekly, Monthly, Yearly
        ];

        public static bool IsValid(string op) => All.Contains(op);
    }
}

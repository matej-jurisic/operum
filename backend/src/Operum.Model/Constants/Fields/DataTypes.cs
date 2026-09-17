namespace Operum.Model.Constants.Fields
{
    public static class DataTypes
    {
        public const string String = "string";
        public const string Number = "number";
        public const string Date = "date";
        public const string DateTime = "datetime";
        public const string TimeSpan = "timespan";
        public const string Bool = "bool";
        public const string Reference = "reference";

        public static readonly HashSet<string> All =
        [
            String, Number, Date, DateTime, TimeSpan, Bool, Reference
        ];

        public static readonly HashSet<string> CalculatedCompatible = [Number, Bool, TimeSpan];

        // Types that filter/sort identically, so a clause authored for one may run against the
        // other. Date/datetime are both stored and compared as a point in time.
        private static readonly string[][] InterchangeableGroups =
        [
            [Date, DateTime],
        ];

        public static bool IsValid(string value) => All.Contains(value);

        public static bool AreCompatible(string clauseType, string fieldType) =>
            string.Equals(clauseType, fieldType, StringComparison.OrdinalIgnoreCase) ||
            InterchangeableGroups.Any(group =>
                group.Contains(clauseType, StringComparer.OrdinalIgnoreCase) &&
                group.Contains(fieldType, StringComparer.OrdinalIgnoreCase));
    }
}

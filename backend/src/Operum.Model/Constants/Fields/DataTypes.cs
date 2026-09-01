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

        public static readonly HashSet<string> All =
        [
            String, Number, Date, DateTime, TimeSpan, Bool
        ];

        public static readonly HashSet<string> CalculatedCompatible = [Number, Bool, TimeSpan];

        // Data types that filter and sort identically, so a clause authored for one may run
        // against a field of the other. Today that's only date/datetime -- both are stored and
        // compared as a point in time; "date" just hides the clock.
        private static readonly string[][] InterchangeableGroups =
        [
            [Date, DateTime],
        ];

        public static bool IsValid(string value) => All.Contains(value);

        // Whether a clause of data type <paramref name="clauseType"/> may be bound to a field
        // of type <paramref name="fieldType"/> -- an exact match, or the two sharing an
        // interchangeable group.
        public static bool AreCompatible(string clauseType, string fieldType) =>
            string.Equals(clauseType, fieldType, StringComparison.OrdinalIgnoreCase) ||
            InterchangeableGroups.Any(group =>
                group.Contains(clauseType, StringComparer.OrdinalIgnoreCase) &&
                group.Contains(fieldType, StringComparer.OrdinalIgnoreCase));
    }
}

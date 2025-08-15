namespace Operum.Model.Constants
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
        public static bool IsValid(string value) => All.Contains(value);
    }
}

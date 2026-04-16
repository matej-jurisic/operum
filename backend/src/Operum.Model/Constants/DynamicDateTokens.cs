namespace Operum.Model.Constants
{
    public static class DynamicDateTokens
    {
        public const string Today = "today";
        public const string StartOfMonth = "start_of_month";
        public const string EndOfMonth = "end_of_month";

        public static readonly HashSet<string> All = [Today, StartOfMonth, EndOfMonth];

        public static DateTime? Resolve(string token) => token switch
        {
            Today => DateTime.UtcNow.Date,
            StartOfMonth => new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            EndOfMonth => new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month,
                DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month), 23, 59, 59, DateTimeKind.Utc),
            _ => null
        };
    }
}

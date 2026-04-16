namespace Operum.Model.Constants
{
    public static class DynamicDateTokens
    {
        public const string Today = "today";
        public const string StartOfWeek = "start_of_week";
        public const string EndOfWeek = "end_of_week";
        public const string StartOfMonth = "start_of_month";
        public const string EndOfMonth = "end_of_month";
        public const string StartOfYear = "start_of_year";
        public const string EndOfYear = "end_of_year";

        public static readonly HashSet<string> All = [Today, StartOfWeek, EndOfWeek, StartOfMonth, EndOfMonth, StartOfYear, EndOfYear];

        public static DateTime? Resolve(string token)
        {
            var now = DateTime.UtcNow;
            // Week starts on Monday
            var daysFromMonday = ((int)now.DayOfWeek + 6) % 7;
            return token switch
            {
                Today => now.Date,
                StartOfWeek => now.Date.AddDays(-daysFromMonday),
                EndOfWeek => now.Date.AddDays(6 - daysFromMonday).Add(new TimeSpan(23, 59, 59)),
                StartOfMonth => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOfMonth => new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, DateTimeKind.Utc),
                StartOfYear => new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndOfYear => new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                _ => null
            };
        }
    }
}

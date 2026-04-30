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

        public const string LastNHours = "last_n_hours";
        public const string LastNDays = "last_n_days";
        public const string LastNWeeks = "last_n_weeks";
        public const string LastNMonths = "last_n_months";

        public static readonly HashSet<string> Fixed = [Today, StartOfWeek, EndOfWeek, StartOfMonth, EndOfMonth, StartOfYear, EndOfYear];
        // Keep All as an alias for backwards compatibility
        public static readonly HashSet<string> All = Fixed;

        private static readonly HashSet<string> ParameterizedPrefixes = [LastNHours, LastNDays, LastNWeeks, LastNMonths];

        public static bool IsValid(string token) =>
            Fixed.Contains(token) || TryParseParameterized(token, out _, out _);

        public static DateTime? Resolve(string token)
        {
            var now = DateTime.UtcNow;
            var daysFromMonday = ((int)now.DayOfWeek + 6) % 7;

            if (Fixed.Contains(token))
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

            if (TryParseParameterized(token, out var prefix, out var n))
                return prefix switch
                {
                    LastNHours => now.AddHours(-n),
                    LastNDays => now.Date.AddDays(-n),
                    LastNWeeks => now.Date.AddDays(-n * 7),
                    LastNMonths => now.Date.AddMonths(-n),
                    _ => null
                };

            return null;
        }

        private static bool TryParseParameterized(string token, out string prefix, out int n)
        {
            prefix = string.Empty;
            n = 0;
            var colon = token.IndexOf(':');
            if (colon < 0) return false;
            prefix = token[..colon];
            return ParameterizedPrefixes.Contains(prefix)
                && int.TryParse(token[(colon + 1)..], out n)
                && n > 0;
        }
    }
}

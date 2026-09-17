using System.Globalization;
using Operum.Model.Extensions;

namespace Operum.Model.Constants
{
    /// <summary>
    /// Date filter tokens resolved at query time: <c>token</c> or <c>token:n</c>. Anchors snap to
    /// a period boundary with a signed offset in that period (bare anchor = offset 0, e.g.
    /// <c>start_of_month:-1</c> = last month); lookbacks (<c>last_n_*</c>) measure backwards from
    /// now with no snapping. All boundaries are computed in the user's time zone and returned as UTC.
    /// </summary>
    public static class DynamicDateTokens
    {
        public const string Now = "now";

        public const string Today = "today";
        public const string EndOfDay = "end_of_day";
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

        public static readonly HashSet<string> Anchors =
            [Today, EndOfDay, StartOfWeek, EndOfWeek, StartOfMonth, EndOfMonth, StartOfYear, EndOfYear];

        private static readonly HashSet<string> LookbackPrefixes = [LastNHours, LastNDays, LastNWeeks, LastNMonths];

        public static bool IsValid(string token) =>
            token == Now || TryParseAnchor(token, out _, out _) || TryParseLookback(token, out _, out _);

        /// <summary>Resolves a token or a literal ISO date/datetime to a UTC instant; null if neither.</summary>
        public static DateTime? ResolveValue(string? value, TimeZoneInfo tz)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            var resolved = Resolve(value, tz);
            if (resolved.HasValue)
                return resolved;

            if (!DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var parsed))
                return null;

            return parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
        }

        public static DateTime? Resolve(string token, TimeZoneInfo tz)
        {
            if (token == Now)
                return DateTime.UtcNow;

            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            if (TryParseAnchor(token, out var anchor, out var offset))
            {
                var local = ResolveAnchorLocal(anchor, offset, nowLocal);
                return local.HasValue ? TimeZoneResolver.ToUtc(local.Value, tz) : null;
            }

            if (TryParseLookback(token, out var prefix, out var n))
            {
                // Hours are a pure instant offset, so they need no calendar or zone reasoning.
                if (prefix == LastNHours)
                    return DateTime.UtcNow.AddHours(-n);

                var local = prefix switch
                {
                    LastNDays => nowLocal.Date.AddDays(-n),
                    LastNWeeks => nowLocal.Date.AddDays(-n * 7),
                    LastNMonths => nowLocal.Date.AddMonths(-n),
                    _ => (DateTime?)null
                };
                return local.HasValue ? TimeZoneResolver.ToUtc(local.Value, tz) : null;
            }

            return null;
        }

        // End-of-period = start of next period minus one tick, to include the whole final second.
        private static DateTime? ResolveAnchorLocal(string anchor, int offset, DateTime nowLocal)
        {
            var startOfDay = nowLocal.Date;
            var startOfWeek = startOfDay.AddDays(-(((int)nowLocal.DayOfWeek + 6) % 7));
            var startOfMonth = new DateTime(nowLocal.Year, nowLocal.Month, 1, 0, 0, 0);
            var startOfYear = new DateTime(nowLocal.Year, 1, 1, 0, 0, 0);

            return anchor switch
            {
                Today => startOfDay.AddDays(offset),
                EndOfDay => startOfDay.AddDays(offset + 1).AddTicks(-1),
                StartOfWeek => startOfWeek.AddDays(offset * 7),
                EndOfWeek => startOfWeek.AddDays((offset + 1) * 7).AddTicks(-1),
                StartOfMonth => startOfMonth.AddMonths(offset),
                EndOfMonth => startOfMonth.AddMonths(offset + 1).AddTicks(-1),
                StartOfYear => startOfYear.AddYears(offset),
                EndOfYear => startOfYear.AddYears(offset + 1).AddTicks(-1),
                _ => null
            };
        }

        private static bool TryParseAnchor(string token, out string anchor, out int offset)
        {
            anchor = string.Empty;
            offset = 0;

            var colon = token.IndexOf(':');
            if (colon < 0)
            {
                anchor = token;
                return Anchors.Contains(token);
            }

            anchor = token[..colon];
            return Anchors.Contains(anchor) && int.TryParse(token[(colon + 1)..], out offset);
        }

        private static bool TryParseLookback(string token, out string prefix, out int n)
        {
            prefix = string.Empty;
            n = 0;

            var colon = token.IndexOf(':');
            if (colon < 0) return false;

            prefix = token[..colon];
            return LookbackPrefixes.Contains(prefix)
                && int.TryParse(token[(colon + 1)..], out n)
                && n != 0;
        }
    }
}

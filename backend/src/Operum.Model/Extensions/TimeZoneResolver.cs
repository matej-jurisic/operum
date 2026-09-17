namespace Operum.Model.Extensions
{
    public static class TimeZoneResolver
    {
        // Falls back to UTC when missing or unknown; filtering must never fail on a stale zone id.
        public static TimeZoneInfo FromId(string? timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                return TimeZoneInfo.Utc;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }

        // Must agree with FromId on what is storable, or an unsupported id silently degrades to UTC.
        public static bool IsValid(string? timeZoneId) =>
            !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);

        // Tolerates both DST edge cases: a time that doesn't exist (spring forward) and one
        // that happens twice (fall back).
        public static DateTime ToUtc(DateTime local, TimeZoneInfo tz)
        {
            var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

            if (tz.IsInvalidTime(unspecified))
            {
                // The wall clock skipped this instant; walk forward until it exists again.
                for (var i = 0; i < 24 && tz.IsInvalidTime(unspecified); i++)
                    unspecified = unspecified.AddMinutes(30);
            }

            if (tz.IsAmbiguousTime(unspecified))
            {
                // Two instants match this wall time; the larger offset is the earlier of the two,
                // so a period start covers the repeated hour instead of skipping it.
                var earliestOffset = tz.GetAmbiguousTimeOffsets(unspecified).Max();
                return DateTime.SpecifyKind(unspecified - earliestOffset, DateTimeKind.Utc);
            }

            return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
        }

        public static (DateTime Start, DateTime EndExclusive) LocalDayWindow(DateTime utcInstant, TimeZoneInfo tz)
        {
            var localDay = TimeZoneInfo.ConvertTimeFromUtc(utcInstant, tz).Date;
            return (ToUtc(localDay, tz), ToUtc(localDay.AddDays(1), tz));
        }
    }
}

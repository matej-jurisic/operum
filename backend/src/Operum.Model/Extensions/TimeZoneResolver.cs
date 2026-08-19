namespace Operum.Model.Extensions
{
    public static class TimeZoneResolver
    {
        /// <summary>
        /// Resolves a stored IANA/Windows time zone id, falling back to UTC when it is missing or
        /// unknown to the host. Filtering must never fail because a user has a stale zone id.
        /// </summary>
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

        /// <summary>
        /// Whether the host can resolve this id. Validation and <see cref="FromId"/> must agree on
        /// what is storable, or an unsupported id gets saved and silently degrades to UTC.
        /// </summary>
        public static bool IsValid(string? timeZoneId) =>
            !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);

        /// <summary>
        /// Converts a local wall-clock boundary to a UTC instant, tolerating the two ways DST breaks
        /// one: a time that does not exist (spring forward) and a time that happens twice (fall back).
        /// </summary>
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

        /// <summary>
        /// The UTC half-open range covering the local calendar day that <paramref name="utcInstant"/>
        /// falls on. Equality on a date field means "same day the user would see on a calendar",
        /// which is a window rather than a single instant once time zones are involved.
        /// </summary>
        public static (DateTime Start, DateTime EndExclusive) LocalDayWindow(DateTime utcInstant, TimeZoneInfo tz)
        {
            var localDay = TimeZoneInfo.ConvertTimeFromUtc(utcInstant, tz).Date;
            return (ToUtc(localDay, tz), ToUtc(localDay.AddDays(1), tz));
        }
    }
}

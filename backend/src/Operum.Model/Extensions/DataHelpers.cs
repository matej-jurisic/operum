using System.Globalization;

namespace Operum.Model.Extensions
{
    public static class DataHelpers
    {
        public static DateTime GetDateTimeFromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            // 1. Try European CSV formats FIRST (dd/MM/yyyy)
            if (TryParseEuropeanDate(value, out DateTime parsed))
            {
                return parsed;
            }

            // 2. Try ISO 8601 (React frontend) with InvariantCulture
            if (DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out parsed))
            {
                return parsed.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                    : parsed.ToUniversalTime();
            }

            throw new FormatException($"Unable to parse '{value}' as a valid DateTime.");
        }

        private static bool TryParseEuropeanDate(string value, out DateTime result)
        {
            // date only: dd/MM/yyyy - assume UTC midnight
            if (DateTime.TryParseExact(
                    value,
                    "dd/MM/yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out result))
            {
                return true;
            }

            // date + time with timezone: dd/MM/yyyy HH:mm:ss zzz (e.g., 23/10/2025 21:21:22 +02:00)
            if (DateTime.TryParseExact(
                    value,
                    "dd/MM/yyyy HH:mm:ss zzz",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dateTimeWithOffset))
            {
                result = dateTimeWithOffset.ToUniversalTime();
                return true;
            }

            // date + time with timezone (no colon): dd/MM/yyyy HH:mm:ss zz (e.g., 23/10/2025 21:21:22 +02)
            if (DateTime.TryParseExact(
                    value,
                    "dd/MM/yyyy HH:mm:ss zz",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out dateTimeWithOffset))
            {
                result = dateTimeWithOffset.ToUniversalTime();
                return true;
            }

            return false;
        }
    }
}
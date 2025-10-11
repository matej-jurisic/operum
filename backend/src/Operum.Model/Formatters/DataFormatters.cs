using Operum.Model.Extensions;
using Operum.Model.Models;
using System.Globalization;

namespace Operum.Model.Converters
{
    public static class DataFormatters
    {
        public static DateTime? StringToDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            DateTime parsed;

            // 1. Try European formats (dd/MM/yyyy and time variants)
            string[] europeanFormats =
            [
                "dd/MM/yyyy",
                "dd/MM/yyyy HH:mm:ss zzz", // e.g. 23/10/2025 21:21:22 +02:00
                "dd/MM/yyyy HH:mm:ss zz"   // e.g. 23/10/2025 21:21:22 +02
            ];

            foreach (var format in europeanFormats)
            {
                var style = format == "dd/MM/yyyy"
                    ? DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                    : DateTimeStyles.None;

                if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, style, out parsed))
                {
                    if (format != "dd/MM/yyyy")
                        parsed = parsed.ToUniversalTime();

                    return parsed;
                }
            }

            // 2. Try ISO 8601 (React frontend)
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

            return null;
        }

        public static string TimeSpanToString(TimeSpan ts)
        {
            double totalSeconds = Math.Round(ts.TotalSeconds, 2);
            int hours = (int)(totalSeconds / 3600);
            int minutes = (int)(totalSeconds % 3600 / 60);
            double seconds = totalSeconds % 60;
            return $"{hours:D2}:{minutes:D2}:{seconds:00.##}";
        }

        public static string NumberToString(double value) => value.ToString("F2");

        public static double FieldValueToDouble(FieldValue? fv)
        {
            if (fv == null) return 0;
            return fv.GetFieldValue() switch
            {
                double d => d,
                TimeSpan ts => ts.TotalMinutes,
                bool b => b ? 1.0 : 0.0,
                _ => 0
            };
        }

        public static string DateTimeToDateString(DateTime dt)
        {
            return dt.ToString("o");
        }

        public static string DateTimeToDateTimeString(DateTime dt)
        {
            return dt.ToString("o");
        }
    }
}
namespace Operum.Model.Extensions
{
    public static class DataHelpers
    {
        public static DateTime GetDateTimeFromString(string value)
        {
            var parsed = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
            if (parsed.Kind == DateTimeKind.Unspecified)
                parsed = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return parsed.ToUniversalTime();
        }
    }
}

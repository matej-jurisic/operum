using Operum.Model.Extensions;
using Operum.Model.Models;

namespace Operum.Service.Helpers.Analytics
{
    public static class AnalyticFormatters
    {
        public static double ConvertToNumeric(FieldValue? fv)
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

        public static string FormatValue(FieldValue fv) =>
            fv.GetValueAsString() ?? string.Empty;

        public static string FormatNumber(double value) => value.ToString("F2");

        public static string FormatTimeSpan(TimeSpan ts)
        {
            double totalSeconds = Math.Round(ts.TotalSeconds, 2);
            int hours = (int)(totalSeconds / 3600);
            int minutes = (int)(totalSeconds % 3600 / 60);
            double seconds = totalSeconds % 60;
            return $"{hours:D2}:{minutes:D2}:{seconds:00.##}";
        }
    }
}

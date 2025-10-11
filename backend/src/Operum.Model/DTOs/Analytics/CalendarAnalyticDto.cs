using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics
{
    public class CalendarAnalyticDto : AnalyticDto
    {
        public string DateFieldName { get; set; } = string.Empty;
        public string? DateFieldType { get; set; }
        public string EventFieldName { get; set; } = string.Empty;
        public List<CalendarPointDto> Points { get; set; } = [];

        public CalendarAnalyticDto()
        {
            ResultType = AnalyticTypes.Calendar;
        }
    }
}

using Operum.Model.Constants.Analytics;

namespace Operum.Model.DTOs.Analytics
{
    public class CalendarAnalyticDto : AnalyticDto
    {
        public string DateFieldName { get; set; } = string.Empty;
        public string EventFieldName { get; set; } = string.Empty;
        public List<CalendarPointDto> Points { get; set; } = [];

        public CalendarAnalyticDto()
        {
            ResultType = AnalyticTypes.Calendar;
        }
    }
}

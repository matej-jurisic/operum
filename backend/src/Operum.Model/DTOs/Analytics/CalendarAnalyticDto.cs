using Operum.Model.Constants.Analytics;
using Operum.Model.DTOs.Fields;

namespace Operum.Model.DTOs.Analytics
{
    public class CalendarAnalyticDto : AnalyticDto
    {
        public FieldDto WhenField { get; set; } = null!;
        public FieldDto WhatField { get; set; } = null!;
        public List<CalendarPointDto> Points { get; set; } = [];

        public CalendarAnalyticDto()
        {
            ResultType = AnalyticTypes.Calendar;
        }
    }
}

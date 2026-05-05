namespace Operum.Model.DTOs.Notifications
{
    public class NotificationEventDto
    {
        public string EventType { get; set; } = string.Empty;
        public string? TimeOfDay { get; set; }

        // Day
        public int? IntervalDays { get; set; }
        public bool? SkipWeekendsDay { get; set; }

        // Week
        public int? IntervalWeeks { get; set; }
        public List<string>? DaysOfWeek { get; set; }

        // Month
        public int? DayOfMonth { get; set; }
        public bool? LastDayOfMonth { get; set; }
        public bool? SkipWeekendsMonth { get; set; }
    }
}

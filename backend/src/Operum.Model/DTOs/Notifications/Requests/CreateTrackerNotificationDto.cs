namespace Operum.Model.DTOs.Notifications.Requests
{
    public class CreateTrackerNotificationDto
    {
        public required string Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<string> ViewIds { get; set; } = [];
        public required CreateNotificationEventDto Event { get; set; }
        public required CreateNotificationConditionDto Condition { get; set; }
    }

    public class CreateNotificationEventDto
    {
        public required string EventType { get; set; }
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

    public class CreateNotificationConditionDto
    {
        public required string ValueMode { get; set; }
        public string? AnalyticCode { get; set; }
        public List<CreateNotificationConditionPurposeFieldDto> PurposeFields { get; set; } = [];
        public List<CreateNotificationConditionFilterDto> Filters { get; set; } = [];
    }

    public class CreateNotificationConditionFilterDto
    {
        public string? FieldId { get; set; }
        public required string Operator { get; set; }
        public string? Value { get; set; }
    }

    public class CreateNotificationConditionPurposeFieldDto
    {
        public required string FieldId { get; set; }
        public required string Purpose { get; set; }
    }
}

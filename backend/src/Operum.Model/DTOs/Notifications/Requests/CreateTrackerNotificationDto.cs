namespace Operum.Model.DTOs.Notifications.Requests
{
    public class CreateTrackerNotificationDto
    {
        public required string Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<string> ViewIds { get; set; } = [];
        public required CreateNotificationConditionDto Condition { get; set; }
    }

    public class CreateNotificationConditionDto
    {
        public required string Code { get; set; }
        public required string Operator { get; set; }
        public required string Value { get; set; }
        public List<CreateNotificationConditionFieldDto> ConditionFields { get; set; } = [];
    }

    public class CreateNotificationConditionFieldDto
    {
        public required string FieldId { get; set; }
        public required string Purpose { get; set; }
    }
}

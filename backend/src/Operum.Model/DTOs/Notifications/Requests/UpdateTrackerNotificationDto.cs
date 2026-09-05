namespace Operum.Model.DTOs.Notifications.Requests
{
    public class UpdateTrackerNotificationDto
    {
        public required string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string? ViewId { get; set; }
        public string? MessageTemplate { get; set; }
        public required CreateNotificationEventDto Event { get; set; }
        public required CreateNotificationConditionDto Condition { get; set; }
    }
}

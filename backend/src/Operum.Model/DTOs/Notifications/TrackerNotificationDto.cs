namespace Operum.Model.DTOs.Notifications
{
    public class TrackerNotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
        public bool IsTriggered { get; set; }
        public List<string> ViewIds { get; set; } = [];
        public NotificationConditionDto Condition { get; set; } = null!;
    }
}

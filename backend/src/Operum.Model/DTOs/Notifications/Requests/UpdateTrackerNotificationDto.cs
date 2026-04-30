namespace Operum.Model.DTOs.Notifications.Requests
{
    public class UpdateTrackerNotificationDto
    {
        public required string Name { get; set; }
        public bool IsEnabled { get; set; }
        public int CooldownMinutes { get; set; }
        public List<string> ViewIds { get; set; } = [];
        public required CreateNotificationConditionDto Condition { get; set; }
    }
}

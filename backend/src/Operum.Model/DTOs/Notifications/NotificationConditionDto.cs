namespace Operum.Model.DTOs.Notifications
{
    public class NotificationConditionDto
    {
        public string Code { get; set; } = string.Empty;
        public string ResultType { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public List<NotificationConditionFieldDto> ConditionFields { get; set; } = [];
    }
}

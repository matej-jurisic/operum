namespace Operum.Model.DTOs.Notifications
{
    public class NotificationConditionDto
    {
        public string ValueMode { get; set; } = string.Empty;
        public string? AnalyticCode { get; set; }
        public string? AnalyticResultType { get; set; }
        public List<NotificationConditionPurposeFieldDto> PurposeFields { get; set; } = [];
        public List<NotificationConditionFilterDto> Filters { get; set; } = [];
    }

    public class NotificationConditionFilterDto
    {
        public string? FieldId { get; set; }
        public string Operator { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class NotificationConditionPurposeFieldDto
    {
        public string Purpose { get; set; } = string.Empty;
        public string FieldId { get; set; } = string.Empty;
    }
}

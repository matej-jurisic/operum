using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class NotificationCondition
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Code { get; set; } = string.Empty;
        public string ResultType { get; set; } = "Single Value";
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public string NotificationId { get; set; } = string.Empty;
        [ForeignKey(nameof(NotificationId))]
        public virtual TrackerNotification Notification { get; set; } = null!;

        public virtual List<NotificationConditionField> ConditionFields { get; set; } = [];
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public enum NotificationValueMode { Entry, Analytic }

    public class NotificationCondition
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public NotificationValueMode ValueMode { get; set; }

        // Analytic mode only
        public string? AnalyticCode { get; set; }
        public string? AnalyticResultType { get; set; }

        public string NotificationId { get; set; } = string.Empty;
        [ForeignKey(nameof(NotificationId))]
        public virtual TrackerNotification Notification { get; set; } = null!;

        public virtual List<NotificationConditionFilter> Filters { get; set; } = [];
        public virtual List<NotificationConditionPurposeField> PurposeFields { get; set; } = [];
    }
}

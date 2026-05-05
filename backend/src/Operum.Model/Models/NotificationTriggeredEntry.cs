using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class NotificationTriggeredEntry
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

        public string NotificationId { get; set; } = string.Empty;
        [ForeignKey(nameof(NotificationId))]
        public virtual TrackerNotification Notification { get; set; } = null!;

        public string EntryId { get; set; } = string.Empty;
        [ForeignKey(nameof(EntryId))]
        public virtual Entry Entry { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class TrackerNotification
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public bool IsTriggered { get; set; } = false;
        public DateTime? LastEvaluatedAt { get; set; }
        public DateTime? LastFiredAt { get; set; }
        public string? ViewId { get; set; }

        /// <summary>
        /// Optional custom push body, supporting <c>{count}</c>/<c>{value}</c>/<c>{tracker}</c>/<c>{notification}</c>
        /// tokens (see <see cref="Operum.Service.Domain.Notifications.NotificationMessageBuilder"/>). Falls back to a
        /// generic default when null or blank.
        /// </summary>
        public string? MessageTemplate { get; set; }

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public virtual NotificationEvent Event { get; set; } = null!;
        public virtual NotificationCondition Condition { get; set; } = null!;
        public virtual List<NotificationTriggeredEntry> TriggeredEntries { get; set; } = [];
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    /// <summary>
    /// One delivered notification in a user's in-app inbox. Written per tracker member every
    /// time a <see cref="TrackerNotification"/> fires, independently of web push. Survives the
    /// notification being deleted (<see cref="NotificationId"/> is nulled, not cascaded).
    /// </summary>
    public class InboxNotification
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public string? NotificationId { get; set; }
        [ForeignKey(nameof(NotificationId))]
        public virtual TrackerNotification? Notification { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}

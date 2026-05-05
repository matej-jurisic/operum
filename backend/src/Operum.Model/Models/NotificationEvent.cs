using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public enum NotificationEventType { Day, Week, Month, Triggered }

    public class NotificationEvent
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public NotificationEventType EventType { get; set; }
        public TimeOnly? TimeOfDay { get; set; }

        // Day
        public int? IntervalDays { get; set; }
        public bool? SkipWeekendsDay { get; set; }

        // Week
        public int? IntervalWeeks { get; set; }
        public int? DaysOfWeekMask { get; set; }

        // Month
        public int? DayOfMonth { get; set; }
        public bool? LastDayOfMonth { get; set; }
        public bool? SkipWeekendsMonth { get; set; }

        public string NotificationId { get; set; } = string.Empty;
        [ForeignKey(nameof(NotificationId))]
        public virtual TrackerNotification Notification { get; set; } = null!;
    }
}

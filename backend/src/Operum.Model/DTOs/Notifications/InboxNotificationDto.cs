namespace Operum.Model.DTOs.Notifications
{
    public class InboxNotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        public string? NotificationName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class InboxPageDto
    {
        public List<InboxNotificationDto> Items { get; set; } = [];
        public int UnreadCount { get; set; }
        public bool HasMore { get; set; }
    }
}

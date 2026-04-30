using Operum.Model.Common;
using Operum.Model.DTOs.Notifications;
using Operum.Model.DTOs.Notifications.Requests;

namespace Operum.Service.Interfaces
{
    public interface INotificationsService
    {
        Task<Result<List<TrackerNotificationDto>>> GetNotifications(string trackerId);
        Task<Result<TrackerNotificationDto>> CreateNotification(string trackerId, CreateTrackerNotificationDto dto);
        Task<Result<TrackerNotificationDto>> UpdateNotification(string trackerId, string notificationId, UpdateTrackerNotificationDto dto);
        Task<Result> DeleteNotification(string trackerId, string notificationId);
        Task<Result<TrackerNotificationDto>> ToggleEnabled(string trackerId, string notificationId);
    }
}

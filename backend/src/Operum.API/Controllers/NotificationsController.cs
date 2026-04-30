using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Notifications.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/trackers/{trackerId}/[controller]")]
    public class NotificationsController(INotificationsService notificationsService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromRoute] string trackerId)
        {
            return GetApiResponse(await notificationsService.GetNotifications(trackerId));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromRoute] string trackerId, [FromBody] CreateTrackerNotificationDto dto)
        {
            return GetApiResponse(await notificationsService.CreateNotification(trackerId, dto));
        }

        [HttpPut("{notificationId}")]
        public async Task<IActionResult> UpdateNotification([FromRoute] string trackerId, [FromRoute] string notificationId, [FromBody] UpdateTrackerNotificationDto dto)
        {
            return GetApiResponse(await notificationsService.UpdateNotification(trackerId, notificationId, dto));
        }

        [HttpDelete("{notificationId}")]
        public async Task<IActionResult> DeleteNotification([FromRoute] string trackerId, [FromRoute] string notificationId)
        {
            return GetApiResponse(await notificationsService.DeleteNotification(trackerId, notificationId));
        }

        [HttpPatch("{notificationId}/toggle")]
        public async Task<IActionResult> ToggleEnabled([FromRoute] string trackerId, [FromRoute] string notificationId)
        {
            return GetApiResponse(await notificationsService.ToggleEnabled(trackerId, notificationId));
        }
    }
}

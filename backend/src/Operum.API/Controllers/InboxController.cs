using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.API.Filters;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [RequiresNotifications]
    [Route("api/[controller]")]
    public class InboxController(IInboxService inboxService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetInbox([FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            return GetApiResponse(await inboxService.GetInbox(skip, take));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            return GetApiResponse(await inboxService.GetUnreadCount());
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead([FromRoute] string id)
        {
            return GetApiResponse(await inboxService.MarkRead(id));
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            return GetApiResponse(await inboxService.MarkAllRead());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            return GetApiResponse(await inboxService.Delete(id));
        }
    }
}

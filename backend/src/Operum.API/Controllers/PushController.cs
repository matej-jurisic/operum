using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.Common;
using Operum.Model.DTOs.Push;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PushController(IWebPushService pushService, ICurrentUserService currentUserService) : BaseController
    {
        [HttpGet("public-key")]
        [AllowAnonymous]
        public IActionResult GetPublicKey()
        {
            return GetApiResponse(Result.Success<string>(pushService.GetVapidPublicKey()));
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] RegisterPushSubscriptionDto dto)
        {
            var user = currentUserService.GetCurrentUser();
            await pushService.RegisterSubscriptionAsync(user.Id, dto);
            return GetApiResponse(Result.Success());
        }

        [HttpDelete("subscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnregisterPushSubscriptionDto dto)
        {
            var user = currentUserService.GetCurrentUser();
            await pushService.UnregisterSubscriptionAsync(user.Id, dto.Endpoint);
            return GetApiResponse(Result.Success());
        }
    }
}

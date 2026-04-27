using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController(IAdminService adminService) : BaseController
    {
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            return GetApiResponse(await adminService.GetStats());
        }

        [HttpGet("trackers")]
        public async Task<IActionResult> GetAllTrackers()
        {
            return GetApiResponse(await adminService.GetAllTrackers());
        }
    }
}

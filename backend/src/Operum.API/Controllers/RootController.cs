using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class RootController : BaseController
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            return Ok("Welcome to the API! This is the root endpoint.");
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace Operum.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : BaseController
    {
        [HttpGet]
        public IActionResult PlaceholderGetProjects()
        {
            return Ok("Project response");
        }
    }
}

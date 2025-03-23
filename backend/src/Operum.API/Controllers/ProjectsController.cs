using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Operum.Model.Common;

namespace Operum.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : BaseController
    {
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult PlaceholderGetProjects()
        {
            return GetApiResponse(ServiceResponse.Success());
        }
    }
}

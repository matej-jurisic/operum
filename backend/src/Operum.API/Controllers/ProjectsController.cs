using Microsoft.AspNetCore.Mvc;
using Operum.Model.Common;

namespace Operum.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : BaseController
    {
        [HttpGet]
        public IActionResult PlaceholderGetProjects()
        {
            return GetApiResponse(ServiceResponse.Success());
        }
    }
}

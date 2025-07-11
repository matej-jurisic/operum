using Microsoft.AspNetCore.Mvc;
using Operum.Model.Common;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : BaseController
    {
        [HttpGet]
        public IActionResult PlaceholderGetProjects()
        {
            return GetApiResponse(ServiceResponse.Success());
        }
    }
}

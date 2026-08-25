using Microsoft.AspNetCore.Mvc;
using Operum.API.Controllers.Base;
using Operum.Model.DTOs.Queries.Requests;
using Operum.Service.Interfaces;

namespace Operum.API.Controllers
{
    [ApiController]
    [Route("api/trackers/{trackerId}/[controller]")]
    public class QueriesController(IQueriesService queriesService) : BaseController
    {
        [HttpPost]
        public async Task<IActionResult> CreateQuery([FromBody] CreateQueryDto query, [FromRoute] string trackerId)
        {
            return GetApiResponse(await queriesService.CreateQuery(trackerId, query));
        }

        [HttpGet("{queryId}")]
        public async Task<IActionResult> GetQuery([FromRoute] string trackerId, [FromRoute] string queryId)
        {
            return GetApiResponse(await queriesService.GetQuery(trackerId, queryId));
        }

        [HttpGet]
        public async Task<IActionResult> GetQueryList([FromRoute] string trackerId)
        {
            return GetApiResponse(await queriesService.GetQueryList(trackerId));
        }

        [HttpPut("{queryId}")]
        public async Task<IActionResult> UpdateQuery([FromRoute] string trackerId, [FromRoute] string queryId, [FromBody] UpdateQueryDto query)
        {
            return GetApiResponse(await queriesService.UpdateQuery(trackerId, queryId, query));
        }

        [HttpDelete("{queryId}")]
        public async Task<IActionResult> DeleteQuery([FromRoute] string trackerId, [FromRoute] string queryId)
        {
            return GetApiResponse(await queriesService.DeleteQuery(trackerId, queryId));
        }
    }
}

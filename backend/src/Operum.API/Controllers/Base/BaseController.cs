using Microsoft.AspNetCore.Mvc;
using Operum.Model.Common;
using Operum.Model.Enums;

namespace Operum.API.Controllers.Base
{
    public class BaseController : ControllerBase
    {
        [NonAction]
        protected IActionResult GetApiResponse<T>(Result<T> serviceResponse)
        {
            var apiResponse = new ApiResponse
            {
                Messages = serviceResponse.Messages,
                Data = serviceResponse.Data,
                StatusCode = serviceResponse.StatusCode,
            };
            return SetStatusCode(apiResponse);
        }

        [NonAction]
        protected IActionResult GetApiResponse(Result serviceResponse)
        {
            var apiResponse = new ApiResponse
            {
                Messages = serviceResponse.Messages,
                StatusCode = serviceResponse.StatusCode,
            };
            return SetStatusCode(apiResponse);
        }

        [NonAction]
        protected IActionResult GetApiResponse(IEnumerable<string> messages, ResultStatus statusCode)
        {
            var apiResponse = new ApiResponse
            {
                Messages = messages,
                StatusCode = statusCode,
            };
            return SetStatusCode(apiResponse);
        }

        [NonAction]
        private ObjectResult SetStatusCode(ApiResponse apiResponse) => apiResponse.StatusCode switch
        {
            ResultStatus.Ok => Ok(apiResponse),
            ResultStatus.BadRequest => BadRequest(apiResponse),
            ResultStatus.Unauthorized => Unauthorized(apiResponse),
            ResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, apiResponse),
            ResultStatus.NotFound => NotFound(apiResponse),
            ResultStatus.Conflict => Conflict(apiResponse),
            ResultStatus.Error => StatusCode(500, apiResponse),
            _ => StatusCode(500, new ApiResponse
            {
                Messages = ["An unexpected error occurred"],
                StatusCode = ResultStatus.Error
            })
        };
    }
}
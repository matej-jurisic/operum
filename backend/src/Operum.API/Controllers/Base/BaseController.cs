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
        protected IActionResult GetApiResponse(IEnumerable<string> messages, StatusCodeEnum statusCode)
        {
            var apiResponse = new ApiResponse
            {
                Messages = messages,
                StatusCode = statusCode,
            };
            return SetStatusCode(apiResponse);
        }

        [NonAction]
        private IActionResult SetStatusCode(ApiResponse apiResponse)
        {
            return apiResponse.StatusCode switch
            {
                StatusCodeEnum.Ok => Ok(apiResponse),
                StatusCodeEnum.BadRequest => BadRequest(apiResponse),
                StatusCodeEnum.Unauthorized => Unauthorized(apiResponse),
                StatusCodeEnum.Forbidden => StatusCode(StatusCodes.Status403Forbidden, apiResponse),
                StatusCodeEnum.NotFound => NotFound(apiResponse),
                StatusCodeEnum.Conflict => Conflict(apiResponse),
                StatusCodeEnum.InternalServerError => StatusCode(500, apiResponse),
                _ => StatusCode(500, new ApiResponse
                {
                    Messages = ["An unexpected error occurred"],
                    StatusCode = StatusCodeEnum.InternalServerError
                })
            };
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Operum.Model.Common;
using Operum.Model.Enums;

namespace Operum.API.Controllers
{
    public class BaseController : ControllerBase
    {
        [NonAction]
        public IActionResult SetStatusCode(ApiResponse apiResponse)
        {
            if (apiResponse.StatusCode == StatusCodeEnum.Ok)
                return Ok(apiResponse);

            if (apiResponse.StatusCode == 0)
            {
                apiResponse.StatusCode = StatusCodeEnum.InternalServerError;
                return StatusCode((int)StatusCodeEnum.InternalServerError, apiResponse);
            }

            return StatusCode((int)apiResponse.StatusCode, apiResponse);
        }

        [NonAction]
        public IActionResult GetApiResponse<T>(ServiceResponse<T> serviceResponse)
        {
            ApiResponse apiResponse = new()
            {
                Messages = serviceResponse.Messages,
                Data = serviceResponse.Data,
                StatusCode = serviceResponse.StatusCode,
            };

            return SetStatusCode(apiResponse);
        }

        [NonAction]
        public IActionResult GetApiResponse(ServiceResponse serviceResponse)
        {
            ApiResponse apiResponse = new()
            {
                Messages = serviceResponse.Messages,
                StatusCode = serviceResponse.StatusCode,
            };
            return SetStatusCode(apiResponse);
        }

        [NonAction]
        public IActionResult GetApiResponse(List<string> messages, StatusCodeEnum statusCode)
        {
            ApiResponse apiResponse = new()
            {
                Messages = messages,
                StatusCode = statusCode,
            };
            return SetStatusCode(apiResponse);
        }
    }
}

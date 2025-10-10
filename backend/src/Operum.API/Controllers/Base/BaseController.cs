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
        protected IActionResult GetApiResponse(IEnumerable<string> messages, ResultStatusCodes statusCode)
        {
            var apiResponse = new ApiResponse
            {
                Messages = messages,
                StatusCode = statusCode,
            };
            return SetStatusCode(apiResponse);
        }

        [NonAction]
        protected IActionResult GetApiFileResponse(Result<FileContentResult> result)
        {
            if (!result.IsSuccess)
            {
                return GetApiResponse(result);
            }
            var fileResult = result.Data!;
            return File(fileResult.FileContents, fileResult.ContentType, fileResult.FileDownloadName);
        }

        [NonAction]
        private ObjectResult SetStatusCode(ApiResponse apiResponse) => apiResponse.StatusCode switch
        {
            ResultStatusCodes.Ok => Ok(apiResponse),
            ResultStatusCodes.BadRequest => BadRequest(apiResponse),
            ResultStatusCodes.Unauthorized => Unauthorized(apiResponse),
            ResultStatusCodes.Forbidden => StatusCode(StatusCodes.Status403Forbidden, apiResponse),
            ResultStatusCodes.NotFound => NotFound(apiResponse),
            ResultStatusCodes.Conflict => Conflict(apiResponse),
            ResultStatusCodes.Error => StatusCode(500, apiResponse),
            _ => StatusCode(500, new ApiResponse
            {
                Messages = ["An unexpected error occurred"],
                StatusCode = ResultStatusCodes.Error
            })
        };
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Operum.Model.Common;
using Operum.Model.Enums;

namespace Operum.API.Middleware
{
    public class AuthorizationResultHandlerMiddleware : IAuthorizationMiddlewareResultHandler
    {
        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Forbidden)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var apiResponse = new ApiResponse
                {
                    Messages = new[] { "Unauthorized." },
                    StatusCode = StatusCodeEnum.Forbidden
                };

                await context.Response.WriteAsJsonAsync(apiResponse);
                return;
            }
            await next(context);
        }
    }
}

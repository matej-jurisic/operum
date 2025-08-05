using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Operum.Model.Common;
using Operum.Model.Enums;
using System.Net;

namespace Operum.API.Middleware
{
    public class AuthorizationResultHandlerMiddleware : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is not null)
            {
                await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
                return;
            }

            if (authorizeResult.Forbidden)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "application/json";

                var apiResponse = new ApiResponse
                {
                    Messages = [StatusCodeEnum.Forbidden.ToString()],
                    StatusCode = StatusCodeEnum.Forbidden
                };

                await context.Response.WriteAsJsonAsync(apiResponse);
                return;
            }

            if (!authorizeResult.Succeeded)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";

                var apiResponse = new ApiResponse
                {
                    Messages = [StatusCodeEnum.Unauthorized.ToString()],
                    StatusCode = StatusCodeEnum.Unauthorized
                };

                await context.Response.WriteAsJsonAsync(apiResponse);
                return;
            }

            await next(context);
        }
    }
}

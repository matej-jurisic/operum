using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Operum.Model.Common;
using Operum.Model.Configuration;
using Operum.Model.Enums;

namespace Operum.API.Filters
{
    /// <summary>
    /// Hides an endpoint behind <see cref="FeatureSettings.Notifications"/>: with the
    /// feature off it answers 404, as if the route never existed. The frontend is gated
    /// by its own build-time flag, so it never has to ask the backend about this.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequiresNotificationsAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var features = context.HttpContext.RequestServices
                .GetRequiredService<IOptions<FeatureSettings>>().Value;

            if (features.Notifications)
                return;

            context.Result = new NotFoundObjectResult(new ApiResponse
            {
                IsSuccess = false,
                StatusCode = ResultStatusCodes.NotFound,
                Messages = ["Notifications are disabled"]
            });
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}

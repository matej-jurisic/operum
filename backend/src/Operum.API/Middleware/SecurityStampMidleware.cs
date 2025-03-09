using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Operum.Model.Models;
using Operum.Service.Services.Auth;
using Operum.Service.Services.Token;
using System.IdentityModel.Tokens.Jwt;

namespace Operum.API.Middleware
{
    public class SecurityStampMidleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task Invoke(HttpContext context, UserManager<ApplicationUser> userManager, IAuthenticationService authenticationService)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                await _next(context);
                return;
            }

            var token = context.Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                await _next(context);
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
                var securityStamp = jwtToken.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.SecurityStamp)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                var user = await userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                var dbSecurityStamp = await userManager.GetSecurityStampAsync(user);

                if (dbSecurityStamp != securityStamp)
                {
                    var expires = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;

                    DateTime? expirationTime = null;

                    if (long.TryParse(expires, out long expUnixTimestamp))
                    {
                        expirationTime = DateTimeOffset.FromUnixTimeSeconds(expUnixTimestamp).UtcDateTime;
                    }

                    authenticationService.SetAuthCookie(user, expires: expirationTime);
                }
            }
            catch (Exception)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await _next(context);
        }
    }
}

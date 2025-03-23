using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Operum.Model.Constants;
using Operum.Model.Models;
using Operum.Service.Services.Auth;
using Operum.Service.Services.Token;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Operum.API.Middleware
{
    public class SecurityStampMidleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task Invoke(HttpContext context, UserManager<ApplicationUser> userManager, IAuthenticationService authenticationService, ITokenService tokenService)
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

            if (context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                await _next(context);
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId)?.Value;
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

                    string newToken = await tokenService.CreateToken(user);
                    authenticationService.SetAuthCookie(newToken, user, expires: expirationTime);

                    if (!string.IsNullOrEmpty(newToken) && context.User.Identities.FirstOrDefault() is { } identity)
                    {
                        var newClaims = handler.ReadJwtToken(newToken).Claims;

                        var newUserName = newClaims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.UniqueName)?.Value;
                        if (!string.IsNullOrEmpty(newUserName))
                        {
                            identity.RemoveClaim(identity.FindFirst(ClaimTypes.Name));
                            identity.AddClaim(new Claim(ClaimTypes.Name, newUserName));
                        }

                        var newRoles = newClaims.Where(c => c.Type == "role").ToList();
                        var existingRoleClaims = identity.FindAll(ClaimTypes.Role).ToList();
                        foreach (var existingRole in existingRoleClaims)
                        {
                            identity.RemoveClaim(existingRole);
                        }
                        foreach (var c in newRoles)
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, c.Value));
                        }
                    }
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

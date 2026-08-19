using Microsoft.AspNetCore.Http;
using Operum.Model;
using Operum.Model.Extensions;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using System.Security.Claims;

namespace Operum.Service.Services.Authorization
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor, OperumContext db) : ICurrentUserService
    {
        private TimeZoneInfo? cachedTimeZone;

        public User GetCurrentUser()
        {
            var applicationUser = GetCurrentUserOptional();
            return applicationUser ?? throw new UnauthorizedAccessException("User not found or token is invalid.");
        }

        public User? GetCurrentUserOptional()
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null || httpContext.User == null)
            {
                return null;
            }

            var userNameClaim = httpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Name);
            var idClaim = httpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);
            var emailClaim = httpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email);

            if (userNameClaim == null || idClaim == null || emailClaim == null) return null;

            return new User()
            {
                Email = emailClaim.Value,
                UserName = userNameClaim.Value,
                Id = idClaim.Value
            };
        }

        public TimeZoneInfo GetCurrentUserTimeZone()
        {
            if (cachedTimeZone != null) return cachedTimeZone;

            var user = GetCurrentUserOptional();
            if (user == null) return cachedTimeZone = TimeZoneInfo.Utc;

            // Not carried on the token: the zone can be changed at any time and a stale claim
            // would silently shift every dynamic date filter until the user signed in again.
            var timeZoneId = db.Users
                .Where(x => x.Id == user.Id)
                .Select(x => x.TimeZone)
                .FirstOrDefault();

            return cachedTimeZone = TimeZoneResolver.FromId(timeZoneId);
        }

        public List<string> GetCurrentUserRoles()
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null || httpContext.User == null)
            {
                throw new UnauthorizedAccessException("User not found or token is invalid.");
            }

            var roleClaims = httpContext.User.Claims.Where(claim => claim.Type == ClaimTypes.Role).ToList();

            if (roleClaims == null || roleClaims.Count == 0)
                return [];

            return [.. roleClaims.Select(claim => claim.Value)];
        }
    }
}

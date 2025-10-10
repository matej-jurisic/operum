using Microsoft.AspNetCore.Http;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using System.Security.Claims;

namespace Operum.Service.Services.Authorization
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
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

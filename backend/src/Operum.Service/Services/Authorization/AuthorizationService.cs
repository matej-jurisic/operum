using Microsoft.AspNetCore.Http;
using Operum.Model.DTOs;
using Operum.Model.Models;
using System.Security.Claims;

namespace Operum.Service.Services.Authorization
{
    public class AuthorizationService(IHttpContextAccessor httpContextAccessor) : IAuthorizationService
    {
        public ApplicationUserDto GetCurrentApplicationUserDto()
        {
            var applicationUser = GetCurrentApplicationUser();
            return new()
            {
                Email = applicationUser.Email,
                Id = applicationUser.Id,
                UserName = applicationUser.UserName
            };
        }

        public ApplicationUser GetCurrentApplicationUser()
        {
            var applicationUser = GetCurrentApplicationUserOptional();
            return applicationUser ?? throw new UnauthorizedAccessException("User not found or token is invalid.");
        }

        public ApplicationUser? GetCurrentApplicationUserOptional()
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

            return new ApplicationUser()
            {
                Email = emailClaim.Value,
                UserName = userNameClaim.Value,
                Id = idClaim.Value
            };
        }

        public List<string> GetCurrentApplicationUserRoles()
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null || httpContext.User == null)
            {
                throw new UnauthorizedAccessException("User not found or token is invalid.");
            }

            var roleClaims = httpContext.User.Claims.Where(claim => claim.Type == ClaimTypes.Role).ToList();

            if (roleClaims == null || roleClaims.Count == 0)
                return [];

            return roleClaims.Select(claim => claim.Value).ToList();
        }

    }
}

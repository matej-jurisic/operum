using Microsoft.AspNetCore.Http;
using Operum.Model.DTOs;
using Operum.Model.Models;
using System.IdentityModel.Tokens.Jwt;

namespace Operum.Service.Services.Authorization
{
    public class AuthorizationService(IHttpContextAccessor httpContextAccessor) : IAuthorizationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

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
            return applicationUser ?? throw new UnauthorizedAccessException();
        }

        public ApplicationUser? GetCurrentApplicationUserOptional()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var token = httpContext.Request.Cookies["AuthToken"];
            if (token == null) return null;

            var jwtHandler = new JwtSecurityTokenHandler();
            var jwt = jwtHandler.ReadJwtToken(token);

            var usernameClaim = jwt.Claims.FirstOrDefault(claim => claim.Type == "unique_name");
            var idClaim = jwt.Claims.FirstOrDefault(claim => claim.Type == "nameid");
            var emailClaim = jwt.Claims.FirstOrDefault(claim => claim.Type == "email");

            return (usernameClaim == null || idClaim == null || emailClaim == null) ? null : new ApplicationUser()
            {
                Email = emailClaim.Value,
                UserName = usernameClaim.Value,
                Id = idClaim.Value,
            };
        }
    }
}

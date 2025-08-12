using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Constants;
using Operum.Model.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Operum.Service.Services.Token
{
    public class TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor, OperumContext dbContext) : ITokenService
    {
        private static readonly JwtSecurityTokenHandler tokenHandler = new();
        private const string AuthTokenKey = "AuthToken";
        private const string RefreshTokenKey = "RefreshToken";

        private async Task<string> CreateToken(ApplicationUser user, DateTime expires)
        {
            if (user.UserName == null || user.Email == null) throw new Exception("User info is missing!");
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
            };

            var roles = await userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new(ClaimTypes.Role, role));
            }

            if (user.SecurityStamp != null)
            {
                claims.Add(new(CustomClaimTypes.SecurityStamp, user.SecurityStamp));
            }

            var secretKey = configuration["JwtSettings:Key"] ?? throw new Exception("Missing jwt configuration!");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var issuer = configuration["JwtSettings:Issuer"] ?? throw new Exception("Missing JWT Issuer!");
            var audience = configuration["JwtSettings:Audience"] ?? throw new Exception("Missing JWT Audience!");
            var expiresAt = expires;

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = issuer,
                Audience = audience,
                Expires = expiresAt,
                SigningCredentials = creds,
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string? GetCookie(string key)
        {
            return httpContextAccessor.HttpContext?.Request.Cookies[key];
        }

        private void SetCookie(string key, string value, DateTime expiresAt)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            };

            var response = httpContextAccessor.HttpContext?.Response;
            response?.Cookies.Delete(key);
            response?.Cookies.Append(key, value, options);
        }

        private void ClearCookie(string key)
        {
            SetCookie(key, "", DateTime.UtcNow.AddDays(-1));
        }

        public async Task<ServiceResponse<DateTime>> SetAuthTokenCookie(ApplicationUser user)
        {
            var expires = GetAuthTokenExpiry();
            var createdToken = await CreateToken(user, expires);
            SetCookie(AuthTokenKey, createdToken, expires);
            return ServiceResponse.Success(expires);
        }
        public async Task<ServiceResponse> SetRefreshTokenCookie(ApplicationUser user)
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                UserId = user.Id,
                ExpiresAt = GetRefreshTokenExpiry()
            };

            await dbContext.RefreshTokens.AddAsync(refreshToken);
            await dbContext.SaveChangesAsync();

            SetCookie(RefreshTokenKey, refreshToken.Token, refreshToken.ExpiresAt);
            return ServiceResponse.Success();
        }

        public string? GetRefreshToken()
        {
            return GetCookie(RefreshTokenKey);
        }

        public void ClearAuthCookies()
        {
            ClearCookie(RefreshTokenKey);
            ClearCookie(AuthTokenKey);
        }

        private DateTime GetAuthTokenExpiry()
        {
            var expiryMinutes = int.TryParse(configuration["JwtSettings:JwtExpiryMinutes"], out var minutes) ? minutes : 20;
            return DateTime.UtcNow.AddMinutes(expiryMinutes);
        }

        private DateTime GetRefreshTokenExpiry()
        {
            var expiryHours = int.TryParse(configuration["JwtSettings:RefreshExpiryHours"], out var hours) ? hours : 24;
            return DateTime.UtcNow.AddHours(expiryHours);
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Operum.Model.Constants;
using Operum.Model.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Operum.Service.Services.Token
{
    public class TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager) : ITokenService
    {
        private static readonly JwtSecurityTokenHandler tokenHandler = new();

        public async Task<string> CreateToken(ApplicationUser user, DateTime? expires = null)
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

            var expiryHours = int.TryParse(configuration["JwtSettings:ExpiryHours"], out var hours) ? hours : 6;
            var expiresAt = expires ?? DateTime.UtcNow.AddHours(expiryHours);

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
    }
}

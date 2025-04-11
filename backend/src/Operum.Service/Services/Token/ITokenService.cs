using Operum.Model.Common;
using Operum.Model.Models;

namespace Operum.Service.Services.Token
{
    public interface ITokenService
    {
        Task<string> CreateToken(ApplicationUser user, DateTime? expires = null);
        Task<ServiceResponse> SetAuthTokenCookie(ApplicationUser user, string? token = null, DateTime? expires = null);
        Task<ServiceResponse> SetRefreshTokenCookie(ApplicationUser user);
        string? GetRefreshToken();
        void ClearAuthCookies();
    }
}

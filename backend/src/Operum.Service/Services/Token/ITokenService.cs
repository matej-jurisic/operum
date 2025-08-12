using Operum.Model.Common;
using Operum.Model.Models;

namespace Operum.Service.Services.Token
{
    public interface ITokenService
    {
        Task<ServiceResponse<DateTime>> SetAuthTokenCookie(ApplicationUser user);
        Task<ServiceResponse> SetRefreshTokenCookie(ApplicationUser user);
        string? GetRefreshToken();
        void ClearAuthCookies();
    }
}

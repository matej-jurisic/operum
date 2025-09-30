using Operum.Model.Common;
using Operum.Model.Models;

namespace Operum.Service.Services.Token
{
    public interface ITokenService
    {
        Task<Result<DateTime>> SetAuthTokenCookie(ApplicationUser user);
        Task<Result> SetRefreshTokenCookie(ApplicationUser user);
        string? GetRefreshToken();
        void ClearAuthCookies();
    }
}

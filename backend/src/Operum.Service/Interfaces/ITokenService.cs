using Operum.Model.Common;
using Operum.Model.DTOs.Auth;
using Operum.Model.Models;

namespace Operum.Service.Interfaces
{
    public interface ITokenService
    {
        Task<Result<TokenResponseDto>> SetAuthTokenCookie(User user);
        Task<Result> SetRefreshTokenCookie(User user);
        string? GetRefreshToken();
        void ClearAuthCookies();
    }
}

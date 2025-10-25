using Operum.Model.DTOs.Auth;

namespace Operum.Service.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo?> GetUserInfoAsync(string idToken);
    }
}

using Operum.Model.DTOs.Auth;

namespace Operum.Service.Interfaces
{
    public interface IGoogleAuthService
    {
        /// <summary>Whether a Google OAuth client id is configured. When false, Google sign-in is unavailable.</summary>
        bool IsEnabled { get; }

        Task<GoogleUserInfo?> GetUserInfoAsync(string idToken);
    }
}

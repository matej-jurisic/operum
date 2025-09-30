using Operum.Model.Common;
using Operum.Model.DTOs.Auth;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.DTOs.Users;

namespace Operum.Service.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<Result<AuthResponseDto>> Login(LoginRequestDto loginRequest);
        Task<Result> Logout();
        Task<Result> Register(RegisterRequestDto registerRequest);
        Task<Result<ApplicationUserDto>> GetCurrentApplicationUser();
        Task<Result<AuthResponseDto>> RefreshToken();
        Task<Result> ConfirmEmail(string userId, string token);

        Task<Result<AuthResponseDto>> GoogleLogin(GoogleLoginRequestDto request);
    }
}

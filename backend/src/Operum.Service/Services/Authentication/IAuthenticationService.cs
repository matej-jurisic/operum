using Operum.Model.Common;
using Operum.Model.DTOs.Auth;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.DTOs.Users;

namespace Operum.Service.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<ServiceResponse<AuthResponseDto>> Login(LoginRequestDto loginRequest);
        Task<ServiceResponse> Logout();
        Task<ServiceResponse> Register(RegisterRequestDto registerRequest);
        Task<ServiceResponse<ApplicationUserDto>> GetCurrentApplicationUser();
        Task<ServiceResponse<AuthResponseDto>> RefreshToken();
        Task<ServiceResponse> ConfirmEmail(string userId, string token);

        Task<ServiceResponse<AuthResponseDto>> GoogleLogin(GoogleLoginRequestDto request);
    }
}

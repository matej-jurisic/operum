using Operum.Model.Common;
using Operum.Model.DTOs;
using Operum.Model.DTOs.Auth.Requests;

namespace Operum.Service.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<ServiceResponse<ApplicationUserDto>> Login(LoginRequestDto loginRequest);
        Task<ServiceResponse> Logout();
        Task<ServiceResponse> Register(RegisterRequestDto registerRequest);
        Task<ServiceResponse<ApplicationUserDto>> GetCurrentApplicationUser();
        Task<ServiceResponse<ApplicationUserDto>> RefreshToken();
        Task<ServiceResponse> ConfirmEmail(string userId, string token);
    }
}

using Operum.Model.Common;
using Operum.Model.DTOs;
using Operum.Model.DTOs.Requests;

namespace Operum.Service.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<ServiceResponse> Login(LoginRequestDto loginRequest);
        Task<ServiceResponse> Logout();
        Task<ServiceResponse> Register(RegisterRequestDto registerRequest);
        Task<ServiceResponse<ApplicationUserDto>> GetCurrentApplicationUser();
        Task<ServiceResponse> RefreshToken();
    }
}

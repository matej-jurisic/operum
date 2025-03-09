using Operum.Model.Common;
using Operum.Model.DTOs;

namespace Operum.Service.Services.Auth
{
    public interface IAuthenticationService
    {
        Task<ServiceResponse> Login(LoginRequestDto loginRequest);
        Task<ServiceResponse> Register(RegisterRequestDto registerRequest);
        ServiceResponse Logout();
        ServiceResponse<ApplicationUserDto> GetCurrentApplicationUser();
        Task<ServiceResponse> UpdateUserName(string newUsername);
    }
}

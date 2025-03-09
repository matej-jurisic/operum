using Operum.Model.Common;
using Operum.Model.DTOs;
using Operum.Model.Models;

namespace Operum.Service.Services.Auth
{
    public interface IAuthenticationService
    {
        Task<ServiceResponse> Login(LoginRequestDto loginRequest);
        Task<ServiceResponse> Register(RegisterRequestDto registerRequest);
        ServiceResponse Logout();
        ServiceResponse<ApplicationUserDto> GetCurrentApplicationUser();
        Task<ServiceResponse> UpdateUserName(string newUsername);
        ServiceResponse SetAuthCookie(ApplicationUser? user = null, bool valid = true, DateTime? expires = null);
    }
}

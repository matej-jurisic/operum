using Operum.Model.Common;
using Operum.Model.DTOs;
using Operum.Model.DTOs.Requests;
using Operum.Model.Models;

namespace Operum.Service.Services.Auth
{
    public interface IAuthenticationService
    {
        Task<ServiceResponse> Login(LoginRequestDto loginRequest);
        ServiceResponse Logout();
        Task<ServiceResponse> Register(RegisterRequestDto registerRequest);
        ServiceResponse<ApplicationUserDto> GetCurrentApplicationUser();
        ServiceResponse SetAuthCookie(string token, ApplicationUser? user = null, bool valid = true, DateTime? expires = null);
        Task<ServiceResponse> GenerateAndSetAuthCookie(ApplicationUser? user = null, bool valid = true, DateTime? expires = null);
        ServiceResponse ClearAuthCookie();
    }
}

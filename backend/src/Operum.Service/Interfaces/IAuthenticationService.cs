using Operum.Model.Common;
using Operum.Model.DTOs.Auth;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.DTOs.Users;

namespace Operum.Service.Interfaces
{
    public interface IAuthenticationService
    {
        Task<Result<AuthResponseDto>> Login(LoginDto loginRequest);
        Task<Result> Logout();
        Task<Result> Register(RegisterDto registerRequest);
        Task<Result<UserDto>> GetCurrentApplicationUser();
        Task<Result<AuthResponseDto>> RefreshToken();
        Task<Result> ConfirmEmail(ConfirmEmailDto request);
        Task<Result<AuthResponseDto>> LoginWithGoogle(GoogleLoginDto request);
    }
}

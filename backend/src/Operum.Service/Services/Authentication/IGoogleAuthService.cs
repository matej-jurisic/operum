using Operum.Model.Common;
using Operum.Model.DTOs.Auth;
using Operum.Model.Models;

namespace Operum.Service.Services.Authentication
{
    public interface IGoogleAuthService
    {
        Task<Result<GoogleTokenPayloadDto>> ValidateTokenAsync(string idToken);
        Task<Result<ApplicationUser>> FindOrCreateUserAsync(GoogleTokenPayloadDto payload);
    }
}

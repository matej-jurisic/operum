using Operum.Model.Common;
using Operum.Model.DTOs.Auth;
using Operum.Model.Models;

namespace Operum.Service.Services.Authentication
{
    public interface IGoogleAuthService
    {
        Task<ServiceResponse<GoogleTokenPayloadDto>> ValidateTokenAsync(string idToken);
        Task<ServiceResponse<ApplicationUser>> FindOrCreateUserAsync(GoogleTokenPayloadDto payload);
    }
}

using Operum.Model.Common;
using Operum.Model.DTOs.Auth;
using Operum.Model.Models;

namespace Operum.Service.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<Result<GoogleTokenPayloadDto>> ValidateTokenAsync(string idToken);
        Task<Result<User>> FindOrCreateUserAsync(GoogleTokenPayloadDto payload);
    }
}

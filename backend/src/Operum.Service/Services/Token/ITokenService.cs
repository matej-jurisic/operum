using Operum.Model.Models;

namespace Operum.Service.Services.Token
{
    public interface ITokenService
    {
        Task<string> CreateToken(ApplicationUser user, DateTime? expires = null);
    }
}

using Operum.Model.Models;

namespace Operum.Service.Services.Token
{
    public interface ITokenService
    {
        string CreateToken(ApplicationUser user);
    }
}

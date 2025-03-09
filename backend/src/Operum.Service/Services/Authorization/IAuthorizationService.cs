using Operum.Model.DTOs;
using Operum.Model.Models;

namespace Operum.Service.Services.Authorization
{
    public interface IAuthorizationService
    {
        ApplicationUser GetCurrentApplicationUser();
        ApplicationUser? GetCurrentApplicationUserOptional();
        ApplicationUserDto GetCurrentApplicationUserDto();
    }
}

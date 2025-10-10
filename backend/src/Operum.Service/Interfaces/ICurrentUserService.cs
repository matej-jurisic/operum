using Operum.Model.Models;

namespace Operum.Service.Interfaces
{
    public interface ICurrentUserService
    {
        User GetCurrentUser();
        List<string> GetCurrentUserRoles();
    }
}

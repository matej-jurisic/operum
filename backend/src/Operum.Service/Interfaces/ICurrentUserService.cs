using Operum.Model.Models;

namespace Operum.Service.Interfaces
{
    public interface ICurrentUserService
    {
        User GetCurrentUser();
        List<string> GetCurrentUserRoles();

        // Falls back to UTC outside a request or when the user has not set one.
        TimeZoneInfo GetCurrentUserTimeZone();
    }
}

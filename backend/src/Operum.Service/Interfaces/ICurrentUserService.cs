using Operum.Model.Models;

namespace Operum.Service.Interfaces
{
    public interface ICurrentUserService
    {
        User GetCurrentUser();
        List<string> GetCurrentUserRoles();

        /// <summary>
        /// The signed-in user's time zone, used to resolve dynamic date tokens against local period
        /// boundaries. Falls back to UTC outside a request or when the user has not set one.
        /// </summary>
        TimeZoneInfo GetCurrentUserTimeZone();
    }
}

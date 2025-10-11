using Operum.Model.Common;
using Operum.Model.DTOs.Users.Requests;

namespace Operum.Service.Interfaces
{
    public interface IRolesService
    {
        Task<Result> ChangeUserRole(string userId, ChangeUserRoleDto request);
        Result<List<string>> GetCurrentUserRoles();
        Task<Result<List<string>>> GetAllRoles();
    }
}

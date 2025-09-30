using Operum.Model.Common;
using Operum.Model.DTOs.Auth.Requests;

namespace Operum.Service.Services.Roles
{
    public interface IRolesService
    {
        Task<Result> ChangeUserRole(string userId, ModifyUserRoleRequestDto request);
        Result<List<string>> GetCurrentUserRoles();
        Task<Result<List<string>>> GetAllRoles();
    }
}

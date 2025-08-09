using Operum.Model.Common;
using Operum.Model.DTOs.Auth.Requests;

namespace Operum.Service.Services.Roles
{
    public interface IRolesService
    {
        Task<ServiceResponse> AddUserToRole(string userId, ModifyUserRoleRequestDto request);
        Task<ServiceResponse> RemoveUserFromRole(string userId, ModifyUserRoleRequestDto request);
        ServiceResponse<List<string>> GetCurrentUserRoles();
        Task<ServiceResponse<List<string?>>> GetAllRoles();
    }
}

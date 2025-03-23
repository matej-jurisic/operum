using Operum.Model.Common;
using Operum.Model.DTOs;
using Operum.Model.DTOs.Requests;

namespace Operum.Service.Services.Users
{
    public interface IUsersService
    {
        Task<ServiceResponse<List<ApplicationUserDto>>> GetAllUsers();
        Task<ServiceResponse> UpdateApplicationUser(UpdateApplicationUserRequestDto request);
    }
}

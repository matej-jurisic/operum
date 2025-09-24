using Operum.Model.Common;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.DTOs.Users;

namespace Operum.Service.Services.Users
{
    public interface IUsersService
    {
        Task<ServiceResponse<List<ApplicationUserDto>>> GetAllUsers();
        Task<ServiceResponse<List<PublicApplicationUserDto>>> SearchUsers(string search);
        Task<ServiceResponse> UpdateApplicationUser(UpdateApplicationUserRequestDto request);
        Task<ServiceResponse> ConfirmUserEmail(string userId);
    }
}

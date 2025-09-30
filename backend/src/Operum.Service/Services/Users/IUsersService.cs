using Operum.Model.Common;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.DTOs.Users;

namespace Operum.Service.Services.Users
{
    public interface IUsersService
    {
        Task<Result<List<ApplicationUserDto>>> GetAllUsers();
        Task<Result<List<PublicApplicationUserDto>>> SearchUsers(string search);
        Task<Result> UpdateApplicationUser(UpdateApplicationUserRequestDto request);
        Task<Result> ConfirmUserEmail(string userId);
    }
}

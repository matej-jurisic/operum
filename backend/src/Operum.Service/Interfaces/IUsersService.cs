using Operum.Model.Common;
using Operum.Model.DTOs.Users;

namespace Operum.Service.Interfaces
{
    public interface IUsersService
    {
        Task<Result<List<UserDto>>> GetAllUsers();
        Task<Result<List<PublicUserDto>>> SearchUsers(string search);
        Task<Result> ConfirmUserEmail(string userId);
    }
}

using Operum.Model.Common;
using Operum.Model.DTOs.Users;
using Operum.Model.DTOs.Users.Requests;

namespace Operum.Service.Interfaces
{
    public interface IUsersService
    {
        Task<Result<List<UserDto>>> GetAllUsers();
        Task<Result<List<PublicUserDto>>> SearchUsers(string search);
        Task<Result> ConfirmUserEmail(string userId);
        Task<Result<UserProfileStatsDto>> GetProfileStats();
        Task<Result<UserDto>> UpdateUsername(UpdateUsernameDto request);
        Task<Result> ChangePassword(ChangePasswordDto request);
        Task<Result> DeleteAccount();
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.Model.Common;
using Operum.Model.DTOs.Users;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Users
{
    public class UsersService(UserManager<User> userManager, IMapper mapper, ICurrentUserService currentUserService) : IUsersService
    {
        public async Task<Result> ConfirmUserEmail(string userId)
        {
            var user = currentUserService.GetCurrentUser();
            if (user.Id == userId)
            {
                return Result.Failure(ResultStatus.BadRequest, "Can't confirm your own email.");
            }

            var appUser = await userManager.FindByIdAsync(userId);

            if (appUser == null)
            {
                return Result.Failure(ResultStatus.NotFound, "User not found.");
            }

            if (appUser.EmailConfirmed)
            {
                return Result.Failure(ResultStatus.BadRequest, "Email already confirmed.");
            }

            appUser.EmailConfirmed = true;
            var result = await userManager.UpdateAsync(appUser);

            if (!result.Succeeded)
            {
                return Result.Failure(ResultStatus.Error, "Failed to confirm email.");
            }

            return Result.Success("Email confirmed successfully.");
        }

        public async Task<Result<List<UserDto>>> GetAllUsers()
        {
            var users = await userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var userDto = mapper.Map<User, UserDto>(user);

                // Get roles for this user
                var roles = await userManager.GetRolesAsync(user);
                userDto.Roles = [.. roles];

                userDtos.Add(userDto);
            }

            return Result.Success(userDtos.OrderBy(x => x.UserName).ToList());
        }

        public async Task<Result<List<PublicUserDto>>> SearchUsers(string search)
        {
            var lowerSearch = search.ToLower();
            var users = await userManager.Users.Where(x => x.UserName != null && x.UserName.ToLower().Contains(lowerSearch)).ToListAsync();

            return Result.Success(mapper.Map<List<User>, List<PublicUserDto>>(users));
        }
    }
}

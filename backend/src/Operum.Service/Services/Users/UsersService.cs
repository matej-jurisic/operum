using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Enums;
using Operum.Model.Constants;
using Operum.Model.DTOs.Users;
using Operum.Model.DTOs.Users.Requests;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;

namespace Operum.Service.Services.Users
{
    public class UsersService(UserManager<User> userManager, IMapper mapper, ICurrentUserService currentUserService, OperumContext db) : IUsersService
    {
        public async Task<Result> ConfirmUserEmail(string userId)
        {
            var user = currentUserService.GetCurrentUser();
            if (user.Id == userId)
            {
                return Result.Failure(ResultStatusCodes.BadRequest);
            }

            var appUser = await userManager.FindByIdAsync(userId);

            if (appUser == null)
            {
                return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("user"));
            }

            if (appUser.EmailConfirmed)
            {
                return Result.Success(Messages.EmailAlreadyConfirmed);
            }

            appUser.EmailConfirmed = true;
            var result = await userManager.UpdateAsync(appUser);

            if (!result.Succeeded)
            {
                return Result.Failure(ResultStatusCodes.Error);
            }

            return Result.Success(Messages.Success);
        }

        public async Task<Result<List<UserDto>>> GetAllUsers()
        {
            var users = await userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var userDto = mapper.Map<User, UserDto>(user);

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

        public async Task<Result<UserProfileStatsDto>> GetProfileStats()
        {
            var userId = currentUserService.GetCurrentUser().Id;

            var trackersOwned = await db.Trackers.CountAsync(t => t.OwnerId == userId && t.TrackerTypeId == null);
            var sharedWithMe = await db.UserTrackers.CountAsync(ut => ut.ApplicationUserId == userId);
            var totalEntries = await db.Entries
                .CountAsync(e => db.Trackers.Any(t => t.Id == e.TrackerId && t.OwnerId == userId));

            return Result.Success(new UserProfileStatsDto
            {
                TrackersOwned = trackersOwned,
                SharedWithMe = sharedWithMe,
                TotalEntries = totalEntries,
            });
        }

        public async Task<Result<UserDto>> UpdateUsername(UpdateUsernameDto request)
        {
            var userId = currentUserService.GetCurrentUser().Id;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("user"));

            var normalizedNew = request.UserName.ToUpper();
            if (await userManager.Users.AnyAsync(x => x.NormalizedUserName == normalizedNew && x.Id != userId))
                return Result.Failure(ResultStatusCodes.Conflict, string.Format(Messages.UsernameTaken, request.UserName));

            user.UserName = request.UserName;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Result.Failure(ResultStatusCodes.Error, result.Errors.Select(e => e.Description));

            var userDto = mapper.Map<User, UserDto>(user);
            var roles = await userManager.GetRolesAsync(user);
            userDto.Roles = [.. roles];
            return Result.Success(userDto, Messages.Success);
        }

        public async Task<Result> ChangePassword(ChangePasswordDto request)
        {
            var userId = currentUserService.GetCurrentUser().Id;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("user"));

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                return Result.Failure(ResultStatusCodes.BadRequest, result.Errors.Select(e => e.Description));

            return Result.Success(Messages.Success);
        }

        public async Task<Result> DeleteAccount()
        {
            var userId = currentUserService.GetCurrentUser().Id;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Result.Failure(ResultStatusCodes.NotFound, Messages.ItemNotFound("user"));

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return Result.Failure(ResultStatusCodes.Error, result.Errors.Select(e => e.Description));

            return Result.Success(Messages.Success);
        }
    }
}

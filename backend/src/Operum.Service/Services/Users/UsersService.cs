using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.Model.Common;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.DTOs.Users;
using Operum.Model.Enums;
using Operum.Model.Models;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Services.Authorization;

namespace Operum.Service.Services.Users
{
    public class UsersService(UserManager<ApplicationUser> userManager, IMapper mapper, IAuthorizationService authorizationService) : IUsersService
    {
        public async Task<ServiceResponse> ConfirmUserEmail(string userId)
        {
            var user = authorizationService.GetCurrentUserDto();
            if (user.Id == userId)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "Can't confirm your own email.");
            }

            var appUser = await userManager.FindByIdAsync(userId);

            if (appUser == null)
            {
                return ServiceResponse.Failure(StatusCodeEnum.NotFound, "User not found.");
            }

            if (appUser.EmailConfirmed)
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, "Email already confirmed.");
            }

            appUser.EmailConfirmed = true;
            var result = await userManager.UpdateAsync(appUser);

            if (!result.Succeeded)
            {
                return ServiceResponse.Failure(StatusCodeEnum.InternalServerError, "Failed to confirm email.");
            }

            return ServiceResponse.Success("Email confirmed successfully.");
        }

        public async Task<ServiceResponse<List<ApplicationUserDto>>> GetAllUsers()
        {
            var users = await userManager.Users.ToListAsync();
            var userDtos = new List<ApplicationUserDto>();

            foreach (var user in users)
            {
                var userDto = mapper.Map<ApplicationUser, ApplicationUserDto>(user);

                // Get roles for this user
                var roles = await userManager.GetRolesAsync(user);
                userDto.Roles = [.. roles];

                userDtos.Add(userDto);
            }

            return ServiceResponse.Success(userDtos.OrderBy(x => x.UserName).ToList());
        }

        public async Task<ServiceResponse> UpdateApplicationUser(UpdateApplicationUserRequestDto request)
        {
            var currentApplicationUser = authorizationService.GetCurrentUser();
            var applicationUser = await userManager.FindByIdAsync(currentApplicationUser.Id) ?? throw new UnauthorizedAccessException();
            applicationUser.UserName = request.UserName;

            var updateResult = await userManager.UpdateAsync(applicationUser);
            if (updateResult.Errors.Any())
            {
                return ServiceResponse.Failure(StatusCodeEnum.BadRequest, updateResult.Errors.Select(x => x.Description));
            }

            await userManager.UpdateSecurityStampAsync(applicationUser);

            return ServiceResponse.Success();
        }
    }
}

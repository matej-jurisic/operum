using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Operum.API.Middleware;
using Operum.Model.Models;
using Operum.Service.Services.Auth;
using Operum.Service.Services.Authorization;
using Operum.Service.Services.Roles;
using Operum.Service.Services.Token;
using Operum.Service.Services.Users;
using IAuthorizationService = Operum.Service.Services.Authorization.IAuthorizationService;

namespace Operum.API.Configuration
{
    public static class DependencyInjectionRegistrations
    {
        public static IServiceCollection RegisterDependencyInjections(this IServiceCollection services)
        {
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUsersService, UsersService>();
            services.AddScoped<IRolesService, RolesService>();
            services.AddScoped<SignInManager<ApplicationUser>>();
            services.AddScoped<UserManager<ApplicationUser>>();

            services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationResultHandlerMiddleware>();

            return services;
        }
    }
}

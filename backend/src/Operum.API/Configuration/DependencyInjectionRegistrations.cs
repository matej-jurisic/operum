using Microsoft.AspNetCore.Identity;
using Operum.Model.Models;
using Operum.Service.Services.Auth;
using Operum.Service.Services.Authorization;
using Operum.Service.Services.Token;

namespace Operum.API.Configuration
{
    public static class DependencyInjectionRegistrations
    {
        public static IServiceCollection RegisterDependencyInjections(this IServiceCollection services)
        {
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<SignInManager<ApplicationUser>>();
            services.AddScoped<UserManager<ApplicationUser>>();
            return services;
        }
    }
}

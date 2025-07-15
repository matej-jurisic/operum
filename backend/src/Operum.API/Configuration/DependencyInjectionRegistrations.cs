using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Operum.API.Middleware;
using Operum.Model.Models;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Mappings.Profiles;
using Operum.Service.Services.Authentication;
using Operum.Service.Services.Authorization;
using Operum.Service.Services.Entries;
using Operum.Service.Services.Fields;
using Operum.Service.Services.Roles;
using Operum.Service.Services.Token;
using Operum.Service.Services.Trackers;
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
            services.AddScoped<ITrackersService, TrackersService>();
            services.AddScoped<IFieldsService, FieldService>();
            services.AddScoped<IEntriesService, EntriesService>();
            services.AddScoped<SignInManager<ApplicationUser>>();
            services.AddScoped<UserManager<ApplicationUser>>();

            services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationResultHandlerMiddleware>();
            services.AddSingleton<IMappingProfile, MappingProfile>();
            services.AddSingleton<IMapper>(provider =>
            {
                var profiles = provider.GetServices<IMappingProfile>();
                return new Mapper(profiles);
            });

            return services;
        }
    }
}

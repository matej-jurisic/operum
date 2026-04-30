using Microsoft.AspNetCore.Authorization;
using Operum.API.Middleware;
using Operum.Model.Configuration;
using Operum.Service.Integrations.MailSender;
using Operum.Service.Interfaces;
using Operum.Service.Mappings.Mapper;
using Operum.Service.Mappings.Profiles;
using Operum.Service.Services.Analytics;
using Operum.Service.Services.Authentication;
using Operum.Service.Services.Authorization;
using Operum.Service.Services.Entries;
using Operum.Service.Services.Fields;
using Operum.Service.Services.Roles;
using Operum.Service.Services.Token;
using Operum.Service.Services.Admin;
using Operum.Service.Services.Trackers;
using Operum.Service.Services.Users;
using Operum.Service.Services.Dashboards;
using Operum.Service.Services.Notifications;
using Operum.Service.Services.Views;

namespace Operum.API.Configuration
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection Configure(this IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterBusinessServices();
            services.RegisterInfrastructureServices(configuration);
            services.RegisterMappingServices();

            return services;
        }

        private static IServiceCollection RegisterBusinessServices(this IServiceCollection services)
        {
            // Authentication & Authorization Services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<Service.Interfaces.IAuthorizationService, AuthorizationService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ITokenService, TokenService>();

            // Core Business Services
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IUsersService, UsersService>();
            services.AddScoped<IRolesService, RolesService>();
            services.AddScoped<ITrackersService, TrackersService>();
            services.AddScoped<IFieldsService, FieldsService>();
            services.AddScoped<IEntriesService, EntriesService>();
            services.AddScoped<IViewsService, ViewsService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<ITrackerConstantsService, TrackerConstantsService>();
            services.AddScoped<IFormulaEvaluationService, FormulaEvaluationService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<INotificationsService, NotificationsService>();

            return services;
        }

        private static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Authorization result middleware
            services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationResultHandlerMiddleware>();

            // Mail Service with MailGun Configuration
            services.Configure<MailGunConfigurationModel>(configuration.GetSection("MailGun"));
            services.AddSingleton<IMailSender, MailSender>();

            return services;
        }

        private static IServiceCollection RegisterMappingServices(this IServiceCollection services)
        {
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
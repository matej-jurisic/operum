using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Operum.API.Middleware;
using Operum.Model.Configuration;
using Operum.Service.Integrations;
using Operum.Service.Integrations.Firefly;
using Operum.Service.Integrations.Intervals;
using Operum.Service.Integrations.MailSender;
using Operum.Service.Services.Integrations;
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
using Operum.Service.Services.Push;
using Operum.Service.Services.Views;
using Operum.Service.Services.Widgets;

namespace Operum.API.Configuration
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection Configure(this IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterBusinessServices(configuration);
            services.RegisterInfrastructureServices(configuration);
            services.RegisterMappingServices();

            return services;
        }

        private static IServiceCollection RegisterBusinessServices(this IServiceCollection services, IConfiguration configuration)
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
            // The context-free write path. Registered alongside EntriesService because the
            // integrations work will point that service at it too, once both agree on
            // coercion behaviour.
            services.AddScoped<IEntryWriter, EntryWriter>();
            services.AddScoped<IViewsService, ViewsService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<ITrackerConstantsService, TrackerConstantsService>();
            services.AddScoped<IFormulaEvaluationService, FormulaEvaluationService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IWidgetsService, WidgetsService>();
            services.AddScoped<INotificationsService, NotificationsService>();
            services.AddScoped<IInboxService, InboxService>();
            services.AddScoped<IWebPushService, WebPushService>();

            // Notifications are opt-in while the feature is unfinished: without the flag the
            // evaluator never runs and the endpoints answer 404 (see RequiresNotificationsAttribute).
            if (configuration.GetValue("Features:Notifications", false))
            {
                services.AddHostedService<NotificationEvaluatorService>();
            }

            services.RegisterIntegrationProviders(configuration);

            return services;
        }

        /// <summary>
        /// Every integration provider, plus the registry that resolves them by key. A new
        /// provider is one line here and nothing else: the sync loop, the webhook endpoint and
        /// the CRUD service all go through the registry.
        /// </summary>
        private static IServiceCollection RegisterIntegrationProviders(this IServiceCollection services, IConfiguration configuration)
        {
            // Providers hold no per-request state -- a catalog and the logic to read a payload
            // -- so they are singletons. Anything a provider needs per call arrives as a
            // ProviderConnection argument, credential included.
            //
            // A named client rather than a typed one: the provider is a singleton and takes
            // the factory, so handlers still rotate instead of one being pinned for the life
            // of the process.
            services.AddHttpClient(IntervalsProvider.ProviderKey, client =>
            {
                client.BaseAddress = new Uri("https://intervals.icu/");
                client.Timeout = TimeSpan.FromSeconds(
                    configuration.GetValue("Integrations:HttpTimeoutSeconds", 30));
            });
            services.AddSingleton<IIntegrationProvider, IntervalsProvider>();

            // Firefly III needs no HttpClient at all: it is push-only, so nothing here ever
            // calls the user's instance.
            services.AddSingleton<IIntegrationProvider, FireflyProvider>();

            services.AddSingleton<IIntegrationProviderRegistry, IntegrationProviderRegistry>();

            services.AddScoped<ICredentialProtector, CredentialProtector>();
            services.AddScoped<IIntegrationSyncExecutor, IntegrationSyncExecutor>();
            services.AddScoped<IIntegrationsService, IntegrationsService>();
            services.AddScoped<IIntegrationWebhookReceiver, IntegrationWebhookReceiver>();

            // Integrations are opt-in while the feature is unfinished: without the flag the
            // sync loop never runs and the endpoints answer 404 (see RequiresIntegrationsAttribute).
            if (configuration.GetValue("Features:Integrations", false))
            {
                services.AddHostedService<IntegrationSyncService>();
            }

            return services;
        }

        private static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Authorization result middleware
            services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationResultHandlerMiddleware>();

            // Feature switches
            services.Configure<FeatureSettings>(configuration.GetSection("Features"));

            // Data Protection encrypts integration credentials at rest. Its key ring must
            // outlive the container: without a persisted path the keys are regenerated on
            // every restart and every stored credential becomes undecryptable. Left at the
            // in-memory default only when no path is configured, which is right for tests and
            // wrong for any deployment that stores a credential -- hence the warning.
            var keyPath = configuration.GetValue<string>("DataProtection:KeyPath");
            if (!string.IsNullOrWhiteSpace(keyPath))
            {
                services.AddDataProtection()
                    .SetApplicationName("Operum")
                    .PersistKeysToFileSystem(new DirectoryInfo(keyPath));
            }
            else
            {
                services.AddDataProtection().SetApplicationName("Operum");
            }

            // Mail Service with MailGun Configuration
            services.Configure<VapidSettings>(configuration.GetSection("Vapid"));
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
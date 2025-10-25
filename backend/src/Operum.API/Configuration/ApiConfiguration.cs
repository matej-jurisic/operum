using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Operum.API.Configuration
{
    public static class ApiConfiguration
    {
        public static void Configure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddFluentValidationRulesToSwagger();
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddHttpContextAccessor();
            services.RegisterCors(configuration);
            services.RegisterRateLimiting(configuration);

            DatabaseConfiguration.Configure(services, configuration);
            AuthenticationConfiguration.Configure(services, configuration);
            ValidationConfiguration.Configure(services);
            ServiceConfiguration.Configure(services, configuration);
        }

        private static void RegisterCors(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(opt =>
            {
                var allowedHosts = configuration.GetValue<string?>("AllowedHosts");
                var origins = allowedHosts?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];

                opt.AddPolicy("CorsPolicy", policy =>
                {
                    policy
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials()
                       .WithOrigins(origins)
                       .WithExposedHeaders("Content-Disposition");
                });
            });
        }

        private static void RegisterRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var windowMinutes = configuration.GetValue<int?>("RateLimiting:WindowMinutes");
            var permitLimit = configuration.GetValue<int?>("RateLimiting:PermitLimit");
            var queueLimit = configuration.GetValue<int?>("RateLimiting:QueueLimit");

            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("fixed", config =>
                {
                    config.Window = TimeSpan.FromMinutes(windowMinutes ?? 1);
                    config.PermitLimit = permitLimit ?? 120;
                    config.QueueLimit = queueLimit ?? 10;
                    config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
            });
        }
    }
}
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
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddHttpContextAccessor();
            services.RegisterCors(configuration);
            services.RegisterRateLimiting();

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
                var origins = allowedHosts?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    ?? ["http://localhost:3000", "https://localhost:3000"];

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

        private static void RegisterRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("fixed", config =>
                {
                    config.Window = TimeSpan.FromMinutes(1);
                    config.PermitLimit = 120;
                    config.QueueLimit = 10;
                    config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
            });
        }
    }
}
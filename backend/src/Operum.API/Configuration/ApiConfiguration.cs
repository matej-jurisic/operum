using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
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
            services.RegisterForwardedHeaders();
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
                var clientUrl = configuration.GetValue<string?>("ClientUrl");
                var origins = clientUrl?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];

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
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // One window per signed-in user, and per client address for anonymous calls
                // (login, register, webhooks), so one busy client never throttles everyone else.
                options.AddPolicy("fixed", httpContext =>
                {
                    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var partitionKey = userId != null
                        ? $"user:{userId}"
                        : $"ip:{httpContext.Connection.RemoteIpAddress}";

                    return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(windowMinutes ?? 1),
                        PermitLimit = permitLimit ?? 120,
                        // Queued requests wait for the next window, which outlasts the client's
                        // timeout, so rejecting straight away gives a clearer error.
                        QueueLimit = queueLimit ?? 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    });
                });
            });
        }

        private static void RegisterForwardedHeaders(this IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
                // The API port is bound to loopback and only reached through the host's reverse
                // proxy, so its X-Forwarded-For is trusted. Without this every anonymous request
                // would carry the proxy's address and share one rate limit window.
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }
    }
}
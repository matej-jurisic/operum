using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Enums;
using Operum.Model.Models;
using System.Text;
using System.Threading.RateLimiting;

namespace Operum.API.Configuration
{
    public static class WebServices
    {
        public static void Configure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddHttpContextAccessor();

            services.RegisterAuthServices(configuration);
            services.RegisterDatabase(configuration);
            services.RegisterCors(configuration);
            services.RegisterRateLimiting();
        }

        public static void RegisterDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("Operum");
            ArgumentException.ThrowIfNullOrEmpty(connectionString);

            services.AddDbContextFactory<OperumContext>(opt =>
            {
                opt.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                opt.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "backend"));
            });
        }

        private static void RegisterAuthServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterJwtAuthentication(configuration);
            services.RegisterIdentity();
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
                       .WithOrigins(origins);
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

        private static void RegisterJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtKey = configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.TryGetValue("AuthToken", out var token) && !string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        var endpoint = context.HttpContext.GetEndpoint();
                        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
                            return;

                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        await context.Response.WriteAsJsonAsync(new ApiResponse
                        {
                            Messages = ["Authentication required"],
                            StatusCode = StatusCodeEnum.Unauthorized
                        });
                    }
                };
            });
        }


        private static void RegisterIdentity(this IServiceCollection services)
        {
            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build())
                .SetInvokeHandlersAfterFailure(false);

            services.AddAuthorization();

            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddSignInManager<SignInManager<ApplicationUser>>()
                .AddUserManager<UserManager<ApplicationUser>>()
                .AddEntityFrameworkStores<OperumContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Password requirements
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;

                // Account lockout
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            });
        }
    }
}
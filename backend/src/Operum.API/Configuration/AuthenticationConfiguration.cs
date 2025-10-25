using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Enums;
using Operum.Model.Models;
using System.Text;

namespace Operum.API.Configuration
{
    public static class AuthenticationConfiguration
    {
        public static void Configure(this IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterJwtAuthentication(configuration);
            services.RegisterIdentity(configuration);
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
                            StatusCode = ResultStatusCodes.Unauthorized
                        });
                    }
                };
            })
            .AddGoogle(options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
            })
            .AddGitHub(options =>
            {
                options.ClientId = configuration["Authentication:GitHub:ClientId"]!;
                options.ClientSecret = configuration["Authentication:GitHub:ClientSecret"]!;
                options.Scope.Add("user:email");
            });
        }


        private static void RegisterIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build())
                .SetInvokeHandlersAfterFailure(false);

            services.AddAuthorization();

            services.AddIdentityCore<User>()
                .AddRoles<IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddSignInManager<SignInManager<User>>()
                .AddUserManager<UserManager<User>>()
                .AddEntityFrameworkStores<OperumContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Password requirements
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                // Account lockout

                var durationMinutes = configuration.GetValue<int?>("Lockout:DurationMinutes");
                var maxFailedAttempts = configuration.GetValue<int?>("Lockout:MaxFailedAttempts");
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(durationMinutes ?? 15);
                options.Lockout.MaxFailedAccessAttempts = maxFailedAttempts ?? 5;
                options.Lockout.AllowedForNewUsers = true;
            });
        }
    }
}

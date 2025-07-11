using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Operum.Model;
using Operum.Model.Common;
using Operum.Model.Enums;
using Operum.Model.Models;
using System.Text;
using YourProject.Service.Mappings;

namespace Operum.API.Configuration
{
    public static class ServiceRegistrations
    {
        public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers(options =>
            {
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            });
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddCors(opt =>
            {
                var allowedHosts = configuration.GetValue<string?>("AllowedHosts");
                var origins = allowedHosts?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                  ?? [];

                opt.AddPolicy("CorsPolicy", policy =>
                {
                    policy
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials()
                       .WithOrigins(origins);
                });
            });
            services.AddAutoMapper(typeof(AutoMapperProfile));
            services.AddHttpContextAccessor();
        }

        public static void RegisterDatabase(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<OperumContext>(opt =>
            {
                opt.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                opt.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "backend"));
            });
        }

        public static void RegisterAuthServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"] ?? throw new Exception("Missing jwt configuration!"))),
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
                        context.Request.Cookies.TryGetValue("AuthToken", out var token);
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("Authentication failed: " + context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("Token validated successfully.");
                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        Console.WriteLine("Challenge issued.");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.HandleResponse();
                        await context.Response.WriteAsJsonAsync(new ApiResponse()
                        {
                            Messages = ["Unauthorized."],
                            StatusCode = StatusCodeEnum.Unauthorized
                        });
                    }
                };
            });
            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
            services.AddAuthorization();
            services.AddIdentityApiEndpoints<IdentityUser>()
                .AddEntityFrameworkStores<OperumContext>();
            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddEntityFrameworkStores<OperumContext>()
                .AddDefaultTokenProviders();
            services.Configure<IdentityOptions>(options =>
            {
                options.User.RequireUniqueEmail = true;
            });
        }
    }
}

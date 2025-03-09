using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Operum.API.Configuration;
using Operum.Model;
using Operum.Model.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRouting(options => options.LowercaseUrls = true);

var configuration = builder.Configuration;

builder.Services.AddCors(opt =>
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

string connectionString = builder.Configuration.GetConnectionString("Operum") ?? throw new Exception("Missing database connection configuration");

builder.Services.AddDbContext<OperumContext>(opt =>
{
    opt.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    opt.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "backend"));
});

builder.Services.AddAuthentication(options =>
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"] ?? throw new Exception("Missing jwt configuration!"))),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
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
        OnChallenge = context =>
        {
            Console.WriteLine("Challenge issued.");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build());
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<OperumContext>();

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<OperumContext>()
    .AddDefaultTokenProviders();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;
});

DependencyInjectionRegistrations.RegisterDependencyInjections(builder.Services);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.MapGroup("/api/auth").MapIdentityApi<IdentityUser>();

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

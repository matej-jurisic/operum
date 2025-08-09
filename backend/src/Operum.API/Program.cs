using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.API.Configuration;
using Operum.API.Seed;
using Operum.Model;
using Operum.Model.Models;
using Prometheus;
using Serilog;

namespace Operum.API;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;
        configuration.AddEnvironmentVariables();

        builder.Services.RegisterServices(configuration);
        builder.Services.RegisterAuthServices(configuration);
        builder.Services.RegisterDependencyInjections();
        builder.Services.RegisterValidations();

        string? connectionString = builder.Configuration.GetConnectionString("Operum");
        if (connectionString != null)
            builder.Services.RegisterDatabase(connectionString);

        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));

        var app = builder.Build();

        app.UseSerilogRequestLogging();

        if (!app.Environment.IsEnvironment("Testing"))
        {
            using var scope = app.Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            db.Database.Migrate();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            await DataSeeder.SeedRolesAsync(roleManager);
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await DataSeeder.SeedUsersAsync(userManager, configuration);
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors("CorsPolicy");

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers()
            .RequireRateLimiting("fixed");

        app.MapMetrics().AllowAnonymous();

        try
        {
            Log.Information("Starting web application");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

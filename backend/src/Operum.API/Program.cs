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

        WebServices.Configure(builder.Services, configuration);
        BusinessServices.Configure(builder.Services, configuration);
        Validations.Configure(builder.Services);

        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));

        var app = builder.Build();

        app.UseSerilogRequestLogging();

        if (!app.Environment.IsEnvironment("Testing"))
        {
            using var scope = app.Services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
            db.Database.Migrate();

            await DataSeeder.SeedTrackerTypesAsync(db);
            await DataSeeder.SeedUsersAsync(userManager, roleManager, configuration);
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors("CorsPolicy");

        app.UseAuthentication();

        app.UseHttpMetrics();

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

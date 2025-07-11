using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.API.Configuration;
using Operum.API.Seed;

namespace Operum.API;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        builder.Services.RegisterServices(configuration);
        builder.Services.RegisterAuthServices(configuration);
        builder.Services.RegisterDependencyInjections();

        string connectionString = builder.Configuration.GetConnectionString("Operum") ?? throw new Exception("Missing database connection configuration");
        builder.Services.RegisterDatabase(connectionString);

        var app = builder.Build();

        if (!app.Environment.IsEnvironment("Testing"))
        {
            using var scope = app.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            await DataSeeder.SeedRolesAsync(roleManager);
        }

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
    }
}

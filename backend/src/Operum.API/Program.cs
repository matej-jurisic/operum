using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Operum.API.Configuration;
using Operum.API.Seed;
using Operum.Model.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.RegisterServices(configuration);
builder.Services.RegisterAuthServices(configuration);
builder.Services.RegisterDependencyInjections();

string connectionString = builder.Configuration.GetConnectionString("Operum") ?? throw new Exception("Missing database connection configuration");
builder.Services.RegisterDatabase(connectionString);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    await RoleSeeder.SeedRolesAsync(userManager, roleManager);
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

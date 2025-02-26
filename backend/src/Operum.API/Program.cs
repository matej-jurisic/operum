using Microsoft.EntityFrameworkCore;
using Operum.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();

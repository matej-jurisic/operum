using Microsoft.EntityFrameworkCore;
using Operum.Model;

namespace Operum.API.Configuration
{
    public static class DatabaseConfiguration
    {
        public static void Configure(this IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterDatabase(configuration);
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
    }
}

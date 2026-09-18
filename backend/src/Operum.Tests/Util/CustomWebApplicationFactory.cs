using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Operum.API;
using Operum.API.Seed;
using Operum.Model;
using Operum.Model.Constants;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using Operum.Tests.Extensions;
using Operum.Tests.Mocks;
using System.Net;

namespace Operum.Tests.Util
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        // SQLite, not the in-memory provider: services rely on relational features the in-memory
        // provider refuses (ExecuteDelete above all). The database lives only while this connection
        // stays open, so the factory owns it and each factory gets its own.
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        public CustomWebApplicationFactory()
        {
            ClientOptions.BaseAddress = new Uri("http://localhost/api/");
            _connection.Open();
        }

        /// <summary>
        /// Configuration a subclass wants on top of the defaults -- feature flags above all,
        /// since a feature that ships dark answers 404 until its flag is set.
        /// </summary>
        protected virtual IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>();

        /// <summary>
        /// A hook for a subclass to replace services the default app registers, for anything
        /// a test must not really do -- reaching an external API, say.
        /// </summary>
        protected virtual void ConfigureTestServices(IServiceCollection services) { }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // Every test shares one in-process client address and a few seeded users, so the
            // real limit would throttle the suite. A subclass's Settings can still lower it.
            builder.UseSetting("RateLimiting:PermitLimit", int.MaxValue.ToString());

            // No appsettings.json for the Testing environment, so registration's confirmation
            // link needs ServerUrl set here. Before the Settings loop, so a subclass can override it.
            builder.UseSetting("ServerUrl", "https://operum.test");

            foreach (var (key, value) in Settings)
                builder.UseSetting(key, value);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<OperumContext>>();
                services.AddDbContext<OperumContext>(options => options
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .UseSqlite(_connection));
                services.AddScoped<IMailSender, MockMailSender>();

                ConfigureTestServices(services);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _connection.Dispose();
        }

        public HttpClient CreateClientWithCookies()
        {
            var cookieContainer = new CookieContainer();

            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = false
            };

            var client = new HttpClient(new RedirectHandler(handler, Server.CreateHandler()))
            {
                BaseAddress = new Uri("http://localhost/api/")
            };

            return client;
        }

        public async Task SeedDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<OperumContext>();
            var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scopedServices.GetRequiredService<UserManager<User>>();

            await db.Database.EnsureCreatedAsync();
            await DataSeeder.SeedUsersAsync(userManager, roleManager);
        }

        /// <summary>A client already logged in as one of the seeded users.</summary>
        public async Task<HttpClient> AuthenticatedClient(RegisterDto userData)
        {
            await SeedDatabaseAsync();
            var client = CreateClientWithCookies();
            await client.Authenticate(userData);
            return client;
        }

        /// <summary>
        /// Adds a second confirmed user and returns a client logged in as them. Registering through
        /// the API would leave the account unconfirmed and unable to log in, since the mock mail
        /// sender reports itself as configured, so the user is created directly instead.
        /// </summary>
        public async Task<(HttpClient Client, string UserName)> AuthenticatedClientForNewUser(string userNamePrefix)
        {
            await SeedDatabaseAsync();

            var credentials = TestDataHelper.CreateUniqueRegisterPayload();
            credentials.UserName = $"{userNamePrefix}_{credentials.UserName}";

            using (var scope = Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var user = new User(credentials.Email, credentials.UserName) { EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, credentials.Password);
                if (!result.Succeeded)
                    throw new Exception($"Could not seed user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                await userManager.AddToRoleAsync(user, RoleNames.User);
            }

            var client = CreateClientWithCookies();
            await client.Authenticate(credentials);
            return (client, credentials.UserName);
        }

        /// <summary>A client logged in as a brand new user, so a test doesn't run into another test's per-user limits on the shared database.</summary>
        public async Task<HttpClient> NewUserClient(string userNamePrefix) =>
            (await AuthenticatedClientForNewUser(userNamePrefix)).Client;
    }

    class RedirectHandler : DelegatingHandler
    {
        public RedirectHandler(HttpMessageHandler innerHandler, HttpMessageHandler testServerHandler)
        {
            InnerHandler = innerHandler;
            InnerHandler = new PassThroughHandler(innerHandler, testServerHandler);
        }

        private class PassThroughHandler : DelegatingHandler
        {
            public PassThroughHandler(HttpMessageHandler innerHandler, HttpMessageHandler testServerHandler)
            {
                InnerHandler = testServerHandler;
            }
        }
    }
}
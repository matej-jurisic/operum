using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Operum.API;
using Operum.API.Seed;
using Operum.Model;
using Operum.Model.Models;
using System.Net;

namespace Operum.Tests.Util
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = "TestDatabase_" + Guid.NewGuid().ToString();

        public CustomWebApplicationFactory()
        {
            ClientOptions.BaseAddress = new Uri("http://localhost/api/");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<OperumContext>>();
                services.AddDbContext<OperumContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });
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
            var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();

            await db.Database.EnsureCreatedAsync();
            await DataSeeder.SeedRolesAsync(roleManager);
            await DataSeeder.SeedUsersAsync(userManager);
        }
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
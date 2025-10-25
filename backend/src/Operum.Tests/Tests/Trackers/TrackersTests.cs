using Operum.Model.Constants;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Tests.Extensions;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Trackers
{
    public class TrackersTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        [Fact]
        public async Task CreateTracker_Valid_ReturnsOk()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();

            await client.Authenticate(DefaultUsers.TestUserData);

            CreateTrackerDto request = new()
            {
                Color = "red",
                Description = "test tracker",
                Name = "Test",
            };
            var response = await client.PostAsJsonAsync("trackers", request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}

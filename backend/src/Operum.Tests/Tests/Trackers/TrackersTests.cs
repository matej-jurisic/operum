using Operum.Model.Constants;
using Operum.Model.DTOs.Trackers;
using Operum.Tests.Extensions;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace Operum.Tests.Tests.Trackers
{
    public class TrackersTest(CustomWebApplicationFactory factory, ITestOutputHelper outputHelper) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;
        private readonly ITestOutputHelper _outputHelper = outputHelper;

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

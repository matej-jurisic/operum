using Operum.Tests.Util;
using System.Net;

namespace Operum.Tests.Tests.RateLimiting
{
    public class RateLimitingTests(RateLimitedFactory factory) : IClassFixture<RateLimitedFactory>
    {
        private readonly RateLimitedFactory _factory = factory;

        [Fact]
        public async Task EachUserHasTheirOwnRequestLimit()
        {
            var busy = await _factory.NewUserClient("busy");
            var quiet = await _factory.NewUserClient("quiet");

            for (var i = 0; i < RateLimitedFactory.PermitLimit; i++)
                Assert.Equal(HttpStatusCode.OK, (await busy.GetAsync("users/me")).StatusCode);

            Assert.Equal(HttpStatusCode.TooManyRequests, (await busy.GetAsync("users/me")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await quiet.GetAsync("users/me")).StatusCode);
        }
    }
}

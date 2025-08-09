using AwesomeAssertions;
using Operum.Model.DTOs.Auth.Requests;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Extensions
{
    public static class ClientExtensions
    {
        public static void Authenticate(this HttpClient client, HttpResponseMessage response)
        {
            response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders).Should().BeTrue();
            var authTokenHeader = setCookieHeaders!
                .FirstOrDefault(c => c.StartsWith("AuthToken=") && !c.StartsWith("AuthToken=;"))
                ?? throw new Exception("AuthToken cookie not found.");
            var parts = authTokenHeader.Split(';', StringSplitOptions.TrimEntries);
            var expiresPart = parts.FirstOrDefault(p => p.StartsWith("Expires=", StringComparison.OrdinalIgnoreCase));

            if (expiresPart is not null)
            {
                var expiresValue = expiresPart["Expires=".Length..];
                var expiresDate = DateTimeOffset.Parse(expiresValue, System.Globalization.CultureInfo.InvariantCulture);
                expiresDate.Should().BeAfter(DateTimeOffset.UtcNow);
            }

            response.Headers.TryGetValues("Set-Cookie", out var refreshTokenHeaders).Should().BeTrue();
            var refreshTokenHeader = refreshTokenHeaders!
                .FirstOrDefault(c => c.StartsWith("RefreshToken=") && !c.StartsWith("RefreshToken=;"))
                ?? throw new Exception("RefreshToken cookie not found.");

            client.DefaultRequestHeaders.Add("Cookie", authTokenHeader);
            client.DefaultRequestHeaders.Add("Cookie", refreshTokenHeader);
        }

        public static async Task Authenticate(this HttpClient client, RegisterRequestDto userData)
        {
            var loginPayload = new LoginRequestDto()
            {
                Credentials = userData.UserName,
                Password = userData.Password,
            };
            var loginResponse = await client.PostAsJsonAsync("auth/login", loginPayload);
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            client.Authenticate(loginResponse);
        }
    }
}

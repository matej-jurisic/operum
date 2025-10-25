using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Operum.Model;
using Operum.Model.Constants;
using Operum.Model.DTOs.Auth.Requests;
using Operum.Tests.Extensions;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace Operum.Tests.Tests.Auth;

public class AuthTests(CustomWebApplicationFactory factory, ITestOutputHelper outputHelper) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory = factory;
    private readonly ITestOutputHelper _outputHelper = outputHelper;

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await _factory.SeedDatabaseAsync();

        var loginPayload = new LoginDto()
        {
            Credentials = DefaultUsers.AdminUserData.UserName,
            Password = DefaultUsers.AdminUserData.Password,
        };
        var loginResponse = await client.PostAsJsonAsync("auth/login", loginPayload);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Register_ValidCredentials_ReturnsOkAndStoresUser()
    {
        var client = _factory.CreateClient();
        await _factory.SeedDatabaseAsync();

        var registerPayload = TestDataHelper.CreateUniqueRegisterPayload();
        var registerResponse = await client.PostAsJsonAsync("auth/register", registerPayload);
        await _outputHelper.PrintMessages(registerResponse);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OperumContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == registerPayload.Email);
        Assert.NotNull(user);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await _factory.SeedDatabaseAsync();

        var wrongCredentialsPayload = new LoginDto()
        {
            Credentials = "not-test@example.com",
            Password = DefaultUsers.AdminUserData.Password,
        };
        var loginResponse = await client.PostAsJsonAsync("auth/login", wrongCredentialsPayload);
        Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);

        var wrongPasswordPayload = new LoginDto()
        {
            Credentials = DefaultUsers.AdminUserData.Email,
            Password = "NotPassword123!"
        };
        loginResponse = await client.PostAsJsonAsync("auth/login", wrongPasswordPayload);
        Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_ValidToken_RemovesTokenAndReturnsOk()
    {
        var client = _factory.CreateClientWithCookies();
        await _factory.SeedDatabaseAsync();

        await client.Authenticate(DefaultUsers.TestUserData);

        var validResponse = await client.GetAsync("users/me");
        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);

        var logoutResponse = await client.PostAsJsonAsync("auth/logout", new { });
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
        client.DefaultRequestHeaders.Remove("Cookie");

        var failedResponse = await client.GetAsync("users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_Authenticated_ReturnsValidToken()
    {
        var client = _factory.CreateClientWithCookies();
        await _factory.SeedDatabaseAsync();

        await client.Authenticate(DefaultUsers.TestUserData);

        var refreshResponse = await client.PostAsync("auth/refresh", null);
        client.Authenticate(refreshResponse);

        var validResponse = await client.GetAsync("users/me");
        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_Authenticated_ReturnsOk()
    {
        var client = _factory.CreateClientWithCookies();
        await _factory.SeedDatabaseAsync();

        await client.Authenticate(DefaultUsers.TestUserData);

        var response = await client.GetAsync("users/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithoutRole_ReturnsForbidden()
    {
        var client = _factory.CreateClientWithCookies();
        await _factory.SeedDatabaseAsync();

        await client.Authenticate(DefaultUsers.TestUserData);

        var response = await client.GetAsync("users/roles");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithRole_ReturnsOk()
    {
        var client = _factory.CreateClientWithCookies();
        await _factory.SeedDatabaseAsync();

        await client.Authenticate(DefaultUsers.AdminUserData);

        var response = await client.GetAsync("users/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateCredentials_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await _factory.SeedDatabaseAsync();

        var originalUser = TestDataHelper.CreateUniqueRegisterPayload();
        await client.PostAsJsonAsync("auth/register", originalUser);

        var sameNameUser = TestDataHelper.CreateUniqueRegisterPayload();
        sameNameUser.UserName = originalUser.UserName;
        var sameNameResponse = await client.PostAsJsonAsync("auth/register", sameNameUser);
        Assert.Equal(HttpStatusCode.Conflict, sameNameResponse.StatusCode);

        var sameEmailUser = TestDataHelper.CreateUniqueRegisterPayload();
        sameEmailUser.Email = originalUser.Email;
        var sameEmailResponse = await client.PostAsJsonAsync("auth/register", sameEmailUser);
        Assert.Equal(HttpStatusCode.Conflict, sameEmailResponse.StatusCode);
    }

    [Theory]
    [InlineData("missingdigit")]
    [InlineData("short")]
    public async Task Register_WeakPasswords_ReturnsBadRequest(string weakPassword)
    {
        var client = _factory.CreateClient();
        await _factory.SeedDatabaseAsync();
        var user = TestDataHelper.CreateUniqueRegisterPayload();
        user.Password = weakPassword;

        var response = await client.PostAsJsonAsync("auth/register", user);
        await _outputHelper.PrintMessages(response);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

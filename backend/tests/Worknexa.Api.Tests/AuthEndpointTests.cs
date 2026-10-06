using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Worknexa.Application.Abstractions;

namespace Worknexa.Api.Tests;

public sealed class AuthEndpointTests : IClassFixture<AuthApplicationFactory>
{
    private readonly AuthApplicationFactory _factory;

    public AuthEndpointTests(AuthApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_RejectsUnknownPassword()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@worknexa.local", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal("UNAUTHORIZED", error?.Code);
        Assert.Equal("Email or password is incorrect.", error?.Message);
    }

    [Fact]
    public async Task Login_Me_Logout_RoundTrip()
    {
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@worknexa.local", password = "Worknexa_Dev123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.True(login.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, cookie => cookie.Contains("worknexa.session", StringComparison.Ordinal) && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var session = await me.Content.ReadFromJsonAsync<Envelope<SessionBody>>();
        Assert.Equal("Worknexa Admin", session?.Data?.DisplayName);
        Assert.Contains("Admin", session?.Data?.Roles ?? []);

        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Add("X-Worknexa-Request", "1");
        var loggedOut = await client.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, loggedOut.StatusCode);

        var after = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task Directory_AllowsAdminAndRejectsTeamMember()
    {
        var member = await AuthenticatedClient("member@worknexa.local");
        var denied = await member.GetAsync("/api/directory");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var admin = await AuthenticatedClient("admin@worknexa.local");
        var allowed = await admin.GetAsync("/api/directory");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task ProfileAndPassword_UpdateTheCurrentUser()
    {
        var client = await AuthenticatedClient("manager@worknexa.local");

        var profile = await client.PutAsJsonAsync("/api/auth/profile", new { displayName = "Updated Manager" });
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var updated = await profile.Content.ReadFromJsonAsync<Envelope<SessionBody>>();
        Assert.Equal("Updated Manager", updated?.Data?.DisplayName);

        var changed = await client.PostAsJsonAsync("/api/auth/password", new { currentPassword = "Worknexa_Dev123!", newPassword = "Changed123" });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var body = await changed.Content.ReadFromJsonAsync<Envelope<LoginBody>>();
        Assert.False(string.IsNullOrWhiteSpace(body?.Data?.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var oldPassword = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "manager@worknexa.local", password = "Worknexa_Dev123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
    }

    [Fact]
    public async Task CookieWrite_WithoutVerificationHeader_IsRejected()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "member@worknexa.local", password = "Worknexa_Dev123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var response = await client.PutAsJsonAsync("/api/auth/profile", new { displayName = "Should Fail" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_LooksLikeAWrongPassword()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@worknexa.local", password = "Worknexa_Dev123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal("Email or password is incorrect.", error?.Message);
    }

    [Theory]
    [InlineData("", "Worknexa_Dev123!")]
    [InlineData("not-an-email", "Worknexa_Dev123!")]
    [InlineData("admin@worknexa.local", "")]
    public async Task Login_InvalidInput_ReturnsValidationError(string email, string password)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal("VALIDATION_ERROR", error?.Code);
    }

    [Fact]
    public async Task Login_OversizedPassword_IsRejectedBeforeHashing()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@worknexa.local", password = new string('a', 10_000) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_DoesNotEchoThePasswordInErrors()
    {
        var client = _factory.CreateClient();
        const string secret = "Sup3r-secret-value!";

        var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@worknexa.local", password = secret });
        var invalid = await client.PostAsJsonAsync("/api/auth/login", new { email = "bad", password = secret });

        Assert.DoesNotContain(secret, await wrong.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.DoesNotContain(secret, await invalid.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChangePassword_WeakOrReusedPassword_ReturnsValidationError()
    {
        var client = await AuthenticatedClient("member@worknexa.local");

        var weak = await client.PostAsJsonAsync("/api/auth/password", new { currentPassword = "Worknexa_Dev123!", newPassword = "short" });
        var reused = await client.PostAsJsonAsync("/api/auth/password", new { currentPassword = "Worknexa_Dev123!", newPassword = "Worknexa_Dev123!" });

        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_IsUnauthorized()
    {
        var client = await AuthenticatedClient("member@worknexa.local");

        var response = await client.PostAsJsonAsync("/api/auth/password", new { currentPassword = "Wrong_Pass123", newPassword = "Another_Pass123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/auth/me")]
    [InlineData("PUT", "/api/auth/profile")]
    [InlineData("POST", "/api/auth/password")]
    public async Task AccountEndpoints_RequireAuthentication(string method, string path)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method != "GET")
        {
            request.Content = JsonContent.Create(new { displayName = "x", currentPassword = "a", newPassword = "b" });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TamperedToken_IsRejected()
    {
        var client = await AuthenticatedClient("member@worknexa.local");
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        var parts = token.Split('.');
        var forged = $"{parts[0]}.{parts[1]}.{new string('A', parts[2].Length)}";
        var forgedClient = _factory.CreateClient();
        forgedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        var response = await forgedClient.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CookieWrite_WithVerificationHeader_IsAllowed()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "member@worknexa.local", password = "Worknexa_Dev123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/auth/profile") { Content = JsonContent.Create(new { displayName = "Worknexa Member" }) };
        request.Headers.Add("X-Worknexa-Request", "1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cors_AllowsConfiguredOriginWithCredentials_AndIgnoresOthers()
    {
        var client = _factory.CreateClient();

        var allowed = new HttpRequestMessage(HttpMethod.Options, "/api/auth/me");
        allowed.Headers.Add("Origin", "http://localhost:5173");
        allowed.Headers.Add("Access-Control-Request-Method", "GET");
        var allowedResponse = await client.SendAsync(allowed);

        var foreign = new HttpRequestMessage(HttpMethod.Options, "/api/auth/me");
        foreign.Headers.Add("Origin", "https://evil.example");
        foreign.Headers.Add("Access-Control-Request-Method", "GET");
        var foreignResponse = await client.SendAsync(foreign);

        Assert.Equal("http://localhost:5173", allowedResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", allowedResponse.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        Assert.False(foreignResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private async Task<HttpClient> AuthenticatedClient(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Worknexa_Dev123!" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<Envelope<LoginBody>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body?.Data?.AccessToken);
        return client;
    }

    private sealed class Envelope<T>
    {
        public T? Data { get; set; }
    }

    private sealed class LoginBody
    {
        public string? AccessToken { get; set; }
    }

    private sealed class SessionBody
    {
        public string? DisplayName { get; set; }

        public string[] Roles { get; set; } = [];
    }

    private sealed class ErrorBody
    {
        public string? Code { get; set; }

        public string? Message { get; set; }
    }
}

public class AuthApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        // Suites sign in many times from the same test client address.
        builder.UseSetting("RateLimits:LoginPerMinute", "10000");
        builder.UseSetting("RateLimits:PasswordChangePerMinute", "10000");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAccountStore>();
            services.AddSingleton<IAccountStore>(provider => new InMemoryAccountStore(provider.GetRequiredService<IPasswordHasher>()));
        });
    }
}

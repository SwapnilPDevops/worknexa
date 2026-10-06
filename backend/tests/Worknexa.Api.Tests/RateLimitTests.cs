using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace Worknexa.Api.Tests;

public sealed class RateLimitTests : IClassFixture<ThrottledAuthApplicationFactory>
{
    private readonly ThrottledAuthApplicationFactory _factory;

    public RateLimitTests(ThrottledAuthApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_IsThrottledPerClientAfterTheLimit()
    {
        var client = _factory.CreateClient();
        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < ThrottledAuthApplicationFactory.LoginLimit + 1; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@worknexa.local", password = "wrong-password" });
            statuses.Add(response.StatusCode);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
                Assert.Equal("TOO_MANY_REQUESTS", error?.Code);
                Assert.Contains("sign-in attempts", error?.Message, StringComparison.Ordinal);
                Assert.True(response.Headers.Contains("Retry-After"));
            }
        }

        Assert.All(statuses.Take(ThrottledAuthApplicationFactory.LoginLimit), status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }

    [Fact]
    public async Task PasswordChange_IsThrottledPerPerson()
    {
        // Signs in through a separate factory so the login budget of the other test is untouched.
        await using var factory = new ThrottledAuthApplicationFactory();
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "member@worknexa.local", password = "Worknexa_Dev123!" });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<Envelope<LoginBody>>())!.Data!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage? last = null;
        for (var attempt = 0; attempt < ThrottledAuthApplicationFactory.PasswordLimit + 1; attempt++)
        {
            last = await client.PostAsJsonAsync("/api/auth/password", new { currentPassword = "Wrong_Pass123", newPassword = "Another_Pass123" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    private sealed class Envelope<T>
    {
        public T? Data { get; set; }
    }

    private sealed class LoginBody
    {
        public string? AccessToken { get; set; }
    }

    private sealed class ErrorBody
    {
        public string? Code { get; set; }

        public string? Message { get; set; }
    }
}

public sealed class ThrottledAuthApplicationFactory : AuthApplicationFactory
{
    public const int LoginLimit = 3;
    public const int PasswordLimit = 2;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimits:LoginPerMinute", LoginLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("RateLimits:PasswordChangePerMinute", PasswordLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}

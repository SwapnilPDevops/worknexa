using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Worknexa.Api.Tests;

public sealed class AnalyticsEndpointTests : IClassFixture<AuthApplicationFactory>
{
    private readonly AuthApplicationFactory _factory;

    public AnalyticsEndpointTests(AuthApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Analytics_RequiresASignedInUser()
    {
        var response = await _factory.CreateClient().GetAsync("/api/dashboard/analytics");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Analytics_ReturnsOneBucketPerWeekForEachSection()
    {
        var client = await AuthenticatedClient();

        var result = await client.GetFromJsonAsync<Envelope<ReportBody>>("/api/dashboard/analytics?weeks=4&offsetMinutes=330");

        var report = result?.Data;
        Assert.NotNull(report);
        Assert.Equal(4, report.Weeks);
        Assert.Equal(4, report.Personal?.Weeks.Length);
        Assert.Equal(4, report.Learning?.Weeks.Length);
        Assert.NotNull(report.Team);
        Assert.NotNull(report.Projects);
        Assert.InRange(report.Personal!.CompletionRate, 0, 100);
        Assert.True(report.Projects!.Projects.Length <= 8);
    }

    [Fact]
    public async Task Analytics_RejectsTooManyWeeks()
    {
        var client = await AuthenticatedClient();

        var response = await client.GetAsync("/api/dashboard/analytics?weeks=13");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> AuthenticatedClient(string email = "admin@worknexa.local")
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

    private sealed class ReportBody
    {
        public int Weeks { get; set; }

        public PersonalBody? Personal { get; set; }

        public object? Team { get; set; }

        public ProjectsBody? Projects { get; set; }

        public WeeksBody? Learning { get; set; }
    }

    private sealed class PersonalBody
    {
        public int CompletionRate { get; set; }

        public object[] Weeks { get; set; } = [];
    }

    private sealed class ProjectsBody
    {
        public object[] Projects { get; set; } = [];
    }

    private sealed class WeeksBody
    {
        public object[] Weeks { get; set; } = [];
    }
}

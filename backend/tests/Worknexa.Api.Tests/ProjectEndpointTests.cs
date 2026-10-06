using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Worknexa.Api.Tests;

public sealed class ProjectEndpointTests : IClassFixture<AuthApplicationFactory>
{
    private readonly AuthApplicationFactory _factory;

    public ProjectEndpointTests(AuthApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Projects_RequireASignedInUser()
    {
        var response = await _factory.CreateClient().GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Project_CreateUsesExistingTasksForProgress()
    {
        var client = await AuthenticatedClient();
        var code = Guid.NewGuid().ToString("N")[..8];
        var created = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "Launch",
            code,
            description = "Ship the workspace",
            status = "Planning",
            priority = "High",
            startDate = "2026-10-06",
            targetDate = "2026-10-20",
            technology = "PostgreSQL",
            repositoryUrl = "https://example.com/repo",
            documentationUrl = "https://example.com/docs"
        });
        created.EnsureSuccessStatusCode();
        var project = await created.Content.ReadFromJsonAsync<Envelope<ProjectBody>>();
        Assert.NotNull(project?.Data);
        Assert.Equal(code.ToUpperInvariant(), project.Data.Code);
        Assert.Equal("Planning", project.Data.Status);
        Assert.Equal(0, project.Data.Dashboard.Progress);
        Assert.Contains(project.Data.Members, member => member.Role == "Owner");

        var task = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Write the brief",
            description = "Reuse the task service",
            status = "Todo",
            priority = "Medium",
            projectId = project.Data.Id
        });
        task.EnsureSuccessStatusCode();

        var loaded = await client.GetAsync($"/api/projects/{project.Data.Id}");
        loaded.EnsureSuccessStatusCode();
        var detail = await loaded.Content.ReadFromJsonAsync<Envelope<ProjectBody>>();
        Assert.Equal(1, detail?.Data?.Dashboard.OpenTasks);
        Assert.Equal(0, detail?.Data?.Dashboard.Progress);

        var activity = await client.GetAsync($"/api/projects/{project.Data.Id}/activity");
        activity.EnsureSuccessStatusCode();
        var events = await activity.Content.ReadFromJsonAsync<Envelope<ActivityBody[]>>();
        Assert.Contains(events?.Data ?? [], item => item.Action == "Created");

        var member = await AuthenticatedClient("member@worknexa.local");
        var denied = await member.PostAsJsonAsync("/api/projects", new
        {
            name = "Nope",
            code = Guid.NewGuid().ToString("N")[..8],
            status = "Planning",
            priority = "Low"
        });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
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

    private sealed class ProjectBody
    {
        public Guid Id { get; set; }

        public string? Code { get; set; }

        public string? Status { get; set; }

        public ProjectMemberBody[] Members { get; set; } = [];

        public DashboardBody Dashboard { get; set; } = new();
    }

    private sealed class ProjectMemberBody
    {
        public string? Role { get; set; }
    }

    private sealed class DashboardBody
    {
        public int Progress { get; set; }

        public int OpenTasks { get; set; }
    }

    private sealed class ActivityBody
    {
        public string? Action { get; set; }
    }
}

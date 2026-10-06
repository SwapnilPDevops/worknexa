using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Worknexa.Api.Tests;

public sealed class SearchEndpointTests : IClassFixture<AuthApplicationFactory>
{
    private readonly AuthApplicationFactory _factory;

    public SearchEndpointTests(AuthApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_RequiresASignedInUser()
    {
        var response = await _factory.CreateClient().GetAsync("/api/search?q=TravelDesk");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_GroupsVisibleWorkAndHidesAnotherPersonsTodo()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var name = $"TravelDesk {token}";
        var admin = await AuthenticatedClient();
        var project = await admin.PostAsJsonAsync("/api/projects", new
        {
            name,
            code = token,
            status = "Active",
            priority = "Medium"
        });
        project.EnsureSuccessStatusCode();
        var task = await admin.PostAsJsonAsync("/api/tasks", new
        {
            title = $"{name} task",
            status = "Todo",
            priority = "Medium"
        });
        task.EnsureSuccessStatusCode();

        var member = await AuthenticatedClient("member@worknexa.local");
        var todo = await member.PostAsJsonAsync("/api/todos", new
        {
            title = $"{name} private",
            priority = "Low",
            recurrence = "None"
        });
        todo.EnsureSuccessStatusCode();

        var result = await admin.GetFromJsonAsync<Envelope<SearchGroup[]>>($"/api/search?q={Uri.EscapeDataString(name)}");
        var projects = result?.Data?.FirstOrDefault(group => group.Entity == "Project");
        var tasks = result?.Data?.FirstOrDefault(group => group.Entity == "Task");
        Assert.Contains(projects?.Hits ?? [], hit => hit.Title == name && hit.Route.StartsWith("/projects?project=", StringComparison.Ordinal));
        Assert.Contains(tasks?.Hits ?? [], hit => hit.Title == $"{name} task" && hit.Route.StartsWith("/tasks?task=", StringComparison.Ordinal));
        Assert.DoesNotContain(result?.Data ?? [], group => group.Entity == "Todo");

        var onlyProjects = await admin.GetFromJsonAsync<Envelope<SearchGroup[]>>($"/api/search?q={Uri.EscapeDataString(name)}&entities=Project");
        Assert.Equal(["Project"], (onlyProjects?.Data ?? []).Select(group => group.Entity).ToArray());
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

    private sealed class SearchGroup
    {
        public string Entity { get; set; } = "";

        public SearchHit[] Hits { get; set; } = [];
    }

    private sealed class SearchHit
    {
        public string Title { get; set; } = "";

        public string Route { get; set; } = "";
    }
}

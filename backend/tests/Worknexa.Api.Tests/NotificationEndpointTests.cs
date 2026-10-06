using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Worknexa.Api.Tests;

public sealed class NotificationEndpointTests : IClassFixture<AuthApplicationFactory>
{
    private static readonly Guid MemberId = Guid.Parse("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2f");

    private readonly AuthApplicationFactory _factory;

    public NotificationEndpointTests(AuthApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Notifications_RequireASignedInUser()
    {
        var response = await _factory.CreateClient().GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assignment_WritesANotificationAndAnActivitySentence()
    {
        var admin = await AuthenticatedClient();
        var member = await AuthenticatedClient("member@worknexa.local");
        var title = $"Notify {Guid.NewGuid():N}"[..18];
        var created = await admin.PostAsJsonAsync("/api/tasks", new
        {
            title,
            description = "Central activity",
            status = "Todo",
            priority = "Medium",
            assigneeId = MemberId
        });
        created.EnsureSuccessStatusCode();
        var task = await created.Content.ReadFromJsonAsync<Envelope<IdBody>>();
        Assert.NotNull(task?.Data);

        var inbox = await member.GetFromJsonAsync<Envelope<NotificationBody[]>>("/api/notifications");
        var assigned = inbox?.Data?.FirstOrDefault(item => item.Type == "TaskAssigned" && item.Body.Contains(title, StringComparison.Ordinal));
        Assert.NotNull(assigned);
        Assert.Contains("assigned", assigned.Body, StringComparison.OrdinalIgnoreCase);

        var activity = await admin.GetFromJsonAsync<Envelope<ActivityBody[]>>("/api/activity");
        var sentence = activity?.Data?.FirstOrDefault(item => item.Entity == "Task" && item.Summary.Contains(title, StringComparison.Ordinal));
        Assert.NotNull(sentence);
        Assert.Equal("Assigned", sentence.Action);
        Assert.Contains("assigned", sentence.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(sentence.Metadata));

        var unread = await member.PostAsync($"/api/notifications/{assigned.Id}/unread", null);
        unread.EnsureSuccessStatusCode();
        var afterUnread = await member.GetFromJsonAsync<Envelope<NotificationBody[]>>("/api/notifications");
        Assert.Contains(afterUnread?.Data ?? [], item => item.Id == assigned.Id && item.IsRead == false);

        var read = await member.PostAsync($"/api/notifications/{assigned.Id}/read", null);
        read.EnsureSuccessStatusCode();

        var muted = await member.PutAsJsonAsync("/api/notifications/preferences", new { type = "TaskAssigned", enabled = false });
        muted.EnsureSuccessStatusCode();
        var quietTitle = $"Quiet {Guid.NewGuid():N}"[..16];
        var quiet = await admin.PostAsJsonAsync("/api/tasks", new
        {
            title = quietTitle,
            status = "Todo",
            priority = "Low",
            assigneeId = MemberId
        });
        quiet.EnsureSuccessStatusCode();
        var afterMute = await member.GetFromJsonAsync<Envelope<NotificationBody[]>>("/api/notifications");
        Assert.DoesNotContain(afterMute?.Data ?? [], item => item.Type == "TaskAssigned" && item.Body.Contains(quietTitle, StringComparison.Ordinal));

        var restored = await member.PutAsJsonAsync("/api/notifications/preferences", new { type = "TaskAssigned", enabled = true });
        restored.EnsureSuccessStatusCode();

        var completed = await admin.PostAsync($"/api/tasks/{task.Data.Id}/complete", null);
        completed.EnsureSuccessStatusCode();
        var timeline = await admin.GetFromJsonAsync<Envelope<ActivityBody[]>>("/api/activity");
        Assert.Contains(timeline?.Data ?? [], item => item.Action == "Completed" && item.Summary.Contains(title, StringComparison.Ordinal));

        var code = Guid.NewGuid().ToString("N")[..8];
        var project = await admin.PostAsJsonAsync("/api/projects", new
        {
            name = "TravelDesk",
            code,
            status = "Planning",
            priority = "Medium"
        });
        project.EnsureSuccessStatusCode();
        var projectBody = await project.Content.ReadFromJsonAsync<Envelope<IdBody>>();
        var moved = await admin.PutAsJsonAsync($"/api/projects/{projectBody?.Data?.Id}", new
        {
            name = "TravelDesk",
            code,
            status = "Active",
            priority = "Medium"
        });
        moved.EnsureSuccessStatusCode();
        var projectActivity = await admin.GetFromJsonAsync<Envelope<ActivityBody[]>>($"/api/projects/{projectBody?.Data?.Id}/activity");
        Assert.Contains(projectActivity?.Data ?? [], item => item.Summary == "TravelDesk moved to Active.");
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

    private sealed class IdBody
    {
        public Guid Id { get; set; }
    }

    private sealed class NotificationBody
    {
        public Guid Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public bool IsRead { get; set; }
    }

    private sealed class ActivityBody
    {
        public string Action { get; set; } = string.Empty;

        public string Entity { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string? Metadata { get; set; }
    }
}

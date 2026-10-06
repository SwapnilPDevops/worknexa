using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Worknexa.Api.Tests;

public sealed class LearningEndpointTests : IClassFixture<AuthApplicationFactory>
{
    private readonly AuthApplicationFactory _factory;

    public LearningEndpointTests(AuthApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LearningTopics_RequireASignedInUser()
    {
        var response = await _factory.CreateClient().GetAsync("/api/learning/topics");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Learning_RollsUpProgressAndLinksNotes()
    {
        var admin = await AuthenticatedClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var parentResponse = await admin.PostAsJsonAsync("/api/learning/topics", new
        {
            name = $"Track {suffix}",
            category = "Practice",
            description = "A parent topic",
            status = "Learning",
            priority = "High",
            goal = $"Finish {suffix}",
            startDate = "2026-10-06",
            targetDate = "2026-10-20"
        });
        parentResponse.EnsureSuccessStatusCode();
        var parent = await parentResponse.Content.ReadFromJsonAsync<Envelope<TopicBody>>();
        Assert.Equal(25, parent?.Data?.Progress);
        Assert.Equal("Learning", parent?.Data?.Status);

        var childResponse = await admin.PostAsJsonAsync("/api/learning/topics", new
        {
            name = $"Lesson {suffix}",
            category = "Practice",
            status = "Practicing",
            progress = 80,
            priority = "Medium",
            parentTopicId = parent!.Data!.Id
        });
        childResponse.EnsureSuccessStatusCode();
        var child = await childResponse.Content.ReadFromJsonAsync<Envelope<TopicBody>>();
        Assert.Equal(parent.Data.Id, child?.Data?.ParentId);

        var listed = await admin.GetFromJsonAsync<Envelope<TopicBody[]>>($"/api/learning/topics?q={suffix}");
        var rolled = listed?.Data?.Single(item => item.Name == $"Track {suffix}");
        Assert.Equal(80, rolled?.Progress);

        var resource = await admin.PostAsJsonAsync($"/api/learning/topics/{child!.Data!.Id}/resources", new
        {
            title = $"Handbook {suffix}",
            url = "https://example.com/learn",
            type = "Article"
        });
        resource.EnsureSuccessStatusCode();
        var detailed = await resource.Content.ReadFromJsonAsync<Envelope<TopicBody>>();
        Assert.Contains(detailed?.Data?.Resources ?? [], item => item.Title == $"Handbook {suffix}");

        var dashboard = await admin.GetFromJsonAsync<Envelope<DashboardBody>>("/api/learning/dashboard?date=2026-10-06");
        Assert.Contains(dashboard?.Data?.CurrentTopics ?? [], item => item.Name == $"Lesson {suffix}");
        Assert.Contains(dashboard?.Data?.Goals ?? [], item => item.Goal == $"Finish {suffix}");
        Assert.Contains(dashboard?.Data?.UpcomingTargets ?? [], item => item.Name == $"Track {suffix}" && item.TargetDate == "2026-10-20");

        var note = await admin.PostAsJsonAsync("/api/notes", new
        {
            title = $"Note {suffix}",
            content = "```\nconst ready = true;\n```",
            isFavorite = true,
            isPinned = false,
            learningTopicId = child.Data.Id,
            tags = new[] { $"learn-{suffix}" }
        });
        note.EnsureSuccessStatusCode();

        var found = await admin.GetFromJsonAsync<Envelope<NoteBody[]>>($"/api/notes?learningOnly=true&favorites=true&q={suffix}");
        var saved = Assert.Single(found?.Data ?? [], item => item.Title == $"Note {suffix}");
        Assert.Equal(child.Data.Id, saved.LearningTopicId);
        Assert.Contains($"learn-{suffix}", saved.Tags ?? []);

        var example = await admin.PostAsync("/api/learning/roadmap/example", null);
        example.EnsureSuccessStatusCode();
        var roadmap = await example.Content.ReadFromJsonAsync<Envelope<RoadmapNode[]>>();
        var frontend = Find(roadmap?.Data ?? [], "Frontend");
        Assert.NotNull(frontend);
        Assert.Contains(frontend.Children, item => item.Name == "JavaScript");
    }

    private static RoadmapNode? Find(IEnumerable<RoadmapNode> nodes, string name)
    {
        foreach (var node in nodes)
        {
            if (node.Name == name)
            {
                return node;
            }

            var child = Find(node.Children, name);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private async Task<HttpClient> AuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@worknexa.local", password = "Worknexa_Dev123!" });
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

    private sealed class TopicBody
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Status { get; set; }

        public int Progress { get; set; }

        public Guid? ParentId { get; set; }

        public ResourceBody[]? Resources { get; set; }
    }

    private sealed class ResourceBody
    {
        public string? Title { get; set; }
    }

    private sealed class DashboardBody
    {
        public TopicBody[]? CurrentTopics { get; set; }

        public GoalBody[]? Goals { get; set; }

        public TargetBody[]? UpcomingTargets { get; set; }
    }

    private sealed class GoalBody
    {
        public string? Goal { get; set; }
    }

    private sealed class TargetBody
    {
        public string? Name { get; set; }

        public string? TargetDate { get; set; }
    }

    private sealed class NoteBody
    {
        public string? Title { get; set; }

        public Guid? LearningTopicId { get; set; }

        public string[]? Tags { get; set; }
    }

    private sealed class RoadmapNode
    {
        public string? Name { get; set; }

        public RoadmapNode[] Children { get; set; } = [];
    }
}

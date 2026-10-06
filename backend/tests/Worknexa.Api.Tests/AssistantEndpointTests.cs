using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Worknexa.Application.Assistant;
using Worknexa.Infrastructure.Assistant;

namespace Worknexa.Api.Tests;

public sealed class AssistantEndpointTests : IClassFixture<AssistantApplicationFactory>
{
    private const int Offset = 330;

    private readonly AssistantApplicationFactory _factory;

    public AssistantEndpointTests(AssistantApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Chat_RequiresASignedInUser()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/assistant/chat", Ask("Hello"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_ProposesStructuredIntentAndOnlyCreatesAfterConfirmation()
    {
        var client = await AuthenticatedClient();
        var token = Guid.NewGuid().ToString("N")[..6];
        var project = await CreateProject(client, $"Atlas {token}", $"AT{token}");
        var title = $"Atlas {token} API documentation";
        _factory.Model.Script = _ => Call("createTask", new { title, assignee = "Worknexa Member", due = "Friday" });

        var chat = await client.PostAsJsonAsync("/api/assistant/chat", Ask($"Create a task for Worknexa Member to complete {title} by Friday."));
        chat.EnsureSuccessStatusCode();
        var reply = (await chat.Content.ReadFromJsonAsync<Envelope<ReplyBody>>())!.Data!;

        var proposal = Assert.IsType<ProposalBody>(reply.Proposal);
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromMinutes(Offset)).DateTime);
        var friday = AssistantDates.Resolve("Friday", today)!.Value;
        Assert.Equal("createTask", proposal.Intent);
        Assert.Equal(title, Field(proposal, "Task"));
        Assert.Equal("Worknexa Member", Field(proposal, "Assignee"));
        Assert.Equal(AssistantDates.Format(friday), Field(proposal, "Due"));
        Assert.StartsWith($"Atlas {token}", Field(proposal, "Project"));
        Assert.Contains("Confirm", reply.Message);
        Assert.Empty(await ProjectTasks(client, project));

        var confirmed = await client.PostAsync($"/api/assistant/actions/{proposal.Id}/confirm", null);
        confirmed.EnsureSuccessStatusCode();
        var result = (await confirmed.Content.ReadFromJsonAsync<Envelope<ActionBody>>())!.Data!;
        Assert.StartsWith("/tasks?task=", result.Route);

        var task = Assert.Single(await ProjectTasks(client, project));
        Assert.Equal(title, task.Title);
        Assert.Equal("Worknexa Member", task.AssigneeName);
        Assert.Equal(friday, DateOnly.FromDateTime(task.DueAt!.Value.ToOffset(TimeSpan.FromMinutes(Offset)).DateTime));

        var again = await client.PostAsync($"/api/assistant/actions/{proposal.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task ReadTool_ResultsGoBackToTheModel()
    {
        var client = await AuthenticatedClient();
        var token = Guid.NewGuid().ToString("N")[..6];
        await CreateProject(client, $"Orbit {token}", $"OR{token}");
        _factory.Model.Script = request => request.Messages[^1].Role == ModelRole.Tool
            ? new ModelResponse(request.Messages[^1].Content, [])
            : Call("searchProjects", new { query = $"Orbit {token}" });

        var chat = await client.PostAsJsonAsync("/api/assistant/chat", Ask($"How is Orbit {token} doing?"));
        chat.EnsureSuccessStatusCode();
        var reply = (await chat.Content.ReadFromJsonAsync<Envelope<ReplyBody>>())!.Data!;

        Assert.Null(reply.Proposal);
        Assert.Equal(["searchProjects"], reply.ToolsUsed);
        Assert.Contains($"OR{token}".ToUpperInvariant(), reply.Message);
    }

    [Fact]
    public async Task UnknownNames_BecomeQuestionsInsteadOfProposals()
    {
        var client = await AuthenticatedClient();
        _factory.Model.Script = request => request.Messages[^1].Role == ModelRole.Tool
            ? new ModelResponse(request.Messages[^1].Content, [])
            : Call("createTask", new { title = "Write release notes", assignee = "Zebulon Qwerty", project = "none" });

        var chat = await client.PostAsJsonAsync("/api/assistant/chat", Ask("Create a task for Zebulon Qwerty to write release notes."));
        chat.EnsureSuccessStatusCode();
        var reply = (await chat.Content.ReadFromJsonAsync<Envelope<ReplyBody>>())!.Data!;

        Assert.Null(reply.Proposal);
        Assert.Contains("needs_input", reply.Message);
        Assert.Contains("Zebulon Qwerty", reply.Message);
    }

    [Fact]
    public async Task CancelledProposals_CannotBeConfirmed()
    {
        var client = await AuthenticatedClient();
        _factory.Model.Script = _ => Call("createReminder", new { title = "Review the roadmap", date = "tomorrow", time = "3pm" });

        var chat = await client.PostAsJsonAsync("/api/assistant/chat", Ask("Remind me tomorrow at 3pm to review the roadmap."));
        chat.EnsureSuccessStatusCode();
        var proposal = (await chat.Content.ReadFromJsonAsync<Envelope<ReplyBody>>())!.Data!.Proposal!;
        Assert.Equal("createReminder", proposal.Intent);
        Assert.EndsWith("at 15:00", Field(proposal, "When"));

        var other = await AuthenticatedClient("manager@worknexa.local");
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/assistant/actions/{proposal.Id}/confirm", null)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/assistant/actions/{proposal.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/assistant/actions/{proposal.Id}/confirm", null)).StatusCode);
    }

    [Fact]
    public async Task Status_ListsOnlyToolsThePersonMayUse()
    {
        var admin = await AuthenticatedClient();
        var member = await AuthenticatedClient("member@worknexa.local");

        var adminStatus = (await admin.GetFromJsonAsync<Envelope<StatusBody>>("/api/assistant/status"))!.Data!;
        var memberStatus = (await member.GetFromJsonAsync<Envelope<StatusBody>>("/api/assistant/status"))!.Data!;

        Assert.True(adminStatus.Enabled);
        Assert.Equal(14, adminStatus.Tools.Length);
        Assert.Contains(adminStatus.Tools, tool => tool.Name == "createTask" && tool.RequiresConfirmation);
        Assert.Contains(adminStatus.Tools, tool => tool.Name == "searchTasks" && !tool.RequiresConfirmation);
        Assert.All(memberStatus.Tools, tool => Assert.Contains(adminStatus.Tools, item => item.Name == tool.Name));
        Assert.All(adminStatus.Tools, tool => Assert.False(string.IsNullOrWhiteSpace(tool.Description)));
    }

    [Fact]
    public async Task Chat_ReportsUnavailableWhenNoModelIsConfigured()
    {
        using var disabled = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAssistantModel>();
            services.AddSingleton<IAssistantModel, DisabledAssistantModel>();
        }));
        var client = await AuthenticatedClient(factoryClient: disabled.CreateClient());

        var status = (await client.GetFromJsonAsync<Envelope<StatusBody>>("/api/assistant/status"))!.Data!;
        var chat = await client.PostAsJsonAsync("/api/assistant/chat", Ask("What is on my plate today?"));

        Assert.False(status.Enabled);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, chat.StatusCode);
        Assert.DoesNotContain("apiKey", await (await client.GetAsync("/api/assistant/status")).Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chat_RejectsConversationsThatDoNotEndWithTheUser()
    {
        var client = await AuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/assistant/chat", new { messages = new[] { new { role = "assistant", content = "Hi" } }, offsetMinutes = Offset });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static object Ask(string text) => new { messages = new[] { new { role = "user", content = text } }, offsetMinutes = Offset };

    private static ModelResponse Call(string tool, object arguments)
    {
        return new ModelResponse(null, [new ModelToolCall($"call_{Guid.NewGuid():N}", tool, JsonSerializer.Serialize(arguments))]);
    }

    private static string Field(ProposalBody proposal, string label) => Assert.Single(proposal.Fields, field => field.Label == label).Value;

    private static async Task<Guid> CreateProject(HttpClient client, string name, string code)
    {
        var created = await client.PostAsJsonAsync("/api/projects", new { name, code, status = "Active", priority = "Medium" });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<Envelope<IdBody>>())!.Data!.Id;
    }

    private static async Task<TaskBody[]> ProjectTasks(HttpClient client, Guid projectId)
    {
        return (await client.GetFromJsonAsync<Envelope<TaskBody[]>>($"/api/tasks?view=list&projectId={projectId}"))?.Data ?? [];
    }

    private async Task<HttpClient> AuthenticatedClient(string email = "admin@worknexa.local", HttpClient? factoryClient = null)
    {
        var client = factoryClient ?? _factory.CreateClient();
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

    private sealed class ReplyBody
    {
        public string Message { get; set; } = "";

        public string[] ToolsUsed { get; set; } = [];

        public ProposalBody? Proposal { get; set; }
    }

    private sealed class ProposalBody
    {
        public Guid Id { get; set; }

        public string Intent { get; set; } = "";

        public FieldBody[] Fields { get; set; } = [];
    }

    private sealed class FieldBody
    {
        public string Label { get; set; } = "";

        public string Value { get; set; } = "";
    }

    private sealed class ActionBody
    {
        public string? Route { get; set; }
    }

    private sealed class TaskBody
    {
        public string Title { get; set; } = "";

        public string? AssigneeName { get; set; }

        public DateTimeOffset? DueAt { get; set; }
    }

    private sealed class StatusBody
    {
        public bool Enabled { get; set; }

        public ToolBody[] Tools { get; set; } = [];
    }

    private sealed class ToolBody
    {
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        public bool RequiresConfirmation { get; set; }
    }
}

public sealed class AssistantApplicationFactory : AuthApplicationFactory
{
    public ScriptedAssistantModel Model { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAssistantModel>();
            services.AddSingleton<IAssistantModel>(Model);
        });
    }
}

/// <summary>Stands in for the language model, so tests decide exactly which tools get called.</summary>
public sealed class ScriptedAssistantModel : IAssistantModel
{
    public Func<ModelRequest, ModelResponse> Script { get; set; } = _ => new ModelResponse("Hello.", []);

    public string Provider => "Scripted";

    public bool IsConfigured => true;

    public Task<ModelResponse> CompleteAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Script(request));
    }
}

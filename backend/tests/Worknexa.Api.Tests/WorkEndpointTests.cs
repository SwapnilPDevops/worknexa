using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;

namespace Worknexa.Api.Tests;

public sealed class WorkEndpointTests : IClassFixture<WorkApplicationFactory>
{
    private readonly WorkApplicationFactory _factory;

    public WorkEndpointTests(WorkApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Tasks_RequireASignedInUser()
    {
        var response = await _factory.CreateClient().GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Tasks_CreateAndMoveThroughTheSharedStore()
    {
        var client = await AuthenticatedClient();

        var created = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Prepare the review",
            description = "Notes for the team",
            status = "Todo",
            priority = "High",
            tags = new[] { "planning" }
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var listed = await client.GetFromJsonAsync<Envelope<TaskBody[]>>("/api/tasks?view=mine");
        Assert.Contains(listed?.Data ?? [], task => task.Title == "Prepare the review" && task.Priority == "High");

        var id = listed!.Data!.Single(task => task.Title == "Prepare the review").Id;
        var completed = await client.PostAsync($"/api/tasks/{id}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var detail = await completed.Content.ReadFromJsonAsync<Envelope<TaskBody>>();
        Assert.Equal("Completed", detail?.Data?.Status);
    }

    [Fact]
    public async Task Tasks_AssignMovesTheTaskToAnotherPerson()
    {
        var client = await AuthenticatedClient();
        var created = await client.PostAsJsonAsync("/api/tasks", new { title = "Hand over the release", status = "Todo", priority = "Medium" });
        var task = (await created.Content.ReadFromJsonAsync<Envelope<TaskBody>>())!.Data!;
        var teammate = Guid.NewGuid();

        var assigned = await client.PutAsJsonAsync($"/api/tasks/{task.Id}/assignee", new { assigneeId = teammate });

        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        Assert.Equal(teammate, (await assigned.Content.ReadFromJsonAsync<Envelope<TaskBody>>())?.Data?.AssigneeId);
        var mine = await client.GetFromJsonAsync<Envelope<TaskBody[]>>("/api/tasks?view=mine");
        Assert.DoesNotContain(mine?.Data ?? [], item => item.Id == task.Id);

        var unassigned = await client.PutAsJsonAsync($"/api/tasks/{task.Id}/assignee", new { assigneeId = (Guid?)null });
        Assert.Null((await unassigned.Content.ReadFromJsonAsync<Envelope<TaskBody>>())?.Data?.AssigneeId);
    }

    [Fact]
    public async Task Tasks_CompleteAndReopenRoundTrip()
    {
        var client = await AuthenticatedClient();
        var created = await client.PostAsJsonAsync("/api/tasks", new { title = "Close the sprint", status = "InProgress", priority = "Low" });
        var task = (await created.Content.ReadFromJsonAsync<Envelope<TaskBody>>())!.Data!;

        var completed = await client.PostAsync($"/api/tasks/{task.Id}/complete", null);
        var reopened = await client.PostAsync($"/api/tasks/{task.Id}/reopen", null);

        Assert.Equal("Completed", (await completed.Content.ReadFromJsonAsync<Envelope<TaskBody>>())?.Data?.Status);
        Assert.Equal("Todo", (await reopened.Content.ReadFromJsonAsync<Envelope<TaskBody>>())?.Data?.Status);
    }

    [Theory]
    [InlineData("POST", "complete")]
    [InlineData("POST", "reopen")]
    [InlineData("PUT", "assignee")]
    public async Task Tasks_ActionsOnAMissingTaskReturnNotFound(string method, string action)
    {
        var client = await AuthenticatedClient();
        var request = new HttpRequestMessage(new HttpMethod(method), $"/api/tasks/{Guid.NewGuid()}/{action}")
        {
            Content = JsonContent.Create(new { assigneeId = Guid.NewGuid() })
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Tasks_RejectUnknownStatusOnStatusChange()
    {
        var client = await AuthenticatedClient();
        var created = await client.PostAsJsonAsync("/api/tasks", new { title = "Status guard", status = "Todo", priority = "Low" });
        var task = (await created.Content.ReadFromJsonAsync<Envelope<TaskBody>>())!.Data!;

        var response = await client.PostAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "Teleported" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Todos_CompletingARecurringItemSchedulesTheNextOne()
    {
        var client = await AuthenticatedClient();
        var due = new DateTimeOffset(2026, 10, 6, 17, 0, 0, TimeSpan.Zero);
        var remind = due.AddHours(-1);

        var created = await client.PostAsJsonAsync("/api/todos?date=2026-10-06&offsetMinutes=0", new
        {
            title = "Standup notes",
            priority = "Medium",
            dueAt = due,
            remindAt = remind,
            recurrence = "Daily",
            tags = new[] { "personal" }
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var todo = await created.Content.ReadFromJsonAsync<Envelope<TodoBody>>();

        var completed = await client.PostAsync($"/api/todos/{todo!.Data!.Id}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);

        var listed = await client.GetFromJsonAsync<Envelope<TodoBody[]>>("/api/todos?date=2026-10-06&offsetMinutes=0");
        var items = listed?.Data ?? [];
        Assert.Contains(items, item => item.Id == todo.Data.Id && item.IsCompleted && item.Bucket == "Completed");
        Assert.Contains(items, item => !item.IsCompleted && item.DueAt == due.AddDays(1) && item.RemindAt == remind.AddDays(1) && item.Bucket == "Tomorrow");
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

    private sealed class TaskBody
    {
        public Guid Id { get; set; }

        public string? Title { get; set; }

        public string? Status { get; set; }

        public string? Priority { get; set; }

        public Guid? AssigneeId { get; set; }
    }

    private sealed class TodoBody
    {
        public Guid Id { get; set; }

        public bool IsCompleted { get; set; }

        public DateTimeOffset? DueAt { get; set; }

        public DateTimeOffset? RemindAt { get; set; }

        public string? Bucket { get; set; }
    }
}

public sealed class WorkApplicationFactory : AuthApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITaskStore>();
            services.RemoveAll<ITodoStore>();
            services.AddSingleton<MemoryTaskStore>();
            services.AddSingleton<ITaskStore>(provider => provider.GetRequiredService<MemoryTaskStore>());
            services.AddSingleton<MemoryTodoStore>();
            services.AddSingleton<ITodoStore>(provider => provider.GetRequiredService<MemoryTodoStore>());
        });
    }
}

public sealed class MemoryTaskStore : ITaskStore
{
    private readonly List<TaskDetail> tasks = [];

    public Task<IReadOnlyList<TaskListItem>> ListAsync(Guid userId, TaskFilter filter, CancellationToken cancellationToken)
    {
        IEnumerable<TaskDetail> query = tasks;
        if (filter.Mine)
        {
            query = query.Where(task => task.AssigneeId == userId);
        }

        return Task.FromResult<IReadOnlyList<TaskListItem>>(query.Select(ToListItem).ToArray());
    }

    public Task<TaskDetail?> FindAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        return Task.FromResult(tasks.FirstOrDefault(task => task.Id == taskId));
    }

    public Task<WorkOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WorkOptions([], [], [], []));
    }

    public Task<TaskDetail> CreateAsync(Guid userId, TaskWriteRequest request, WorkTaskStatus status, WorkPriority priority, CancellationToken cancellationToken)
    {
        var task = Detail(Guid.NewGuid(), request.Title.Trim(), status.ToString(), priority.ToString(), userId);
        tasks.Add(task);
        return Task.FromResult(task);
    }

    public Task<TaskDetail?> UpdateAsync(Guid userId, Guid taskId, TaskWriteRequest request, WorkTaskStatus status, WorkPriority priority, CancellationToken cancellationToken)
    {
        return Replace(taskId, request.Title, status.ToString(), priority.ToString());
    }

    public Task<TaskDetail?> SetStatusAsync(Guid userId, Guid taskId, WorkTaskStatus status, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var current = tasks.FirstOrDefault(task => task.Id == taskId);
        return current is null
            ? Task.FromResult<TaskDetail?>(null)
            : Replace(taskId, current.Title, status.ToString(), current.Priority);
    }

    public Task<bool> DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        return Task.FromResult(tasks.RemoveAll(task => task.Id == taskId) > 0);
    }

    public Task<TaskDetail?> AssignAsync(Guid userId, Guid taskId, Guid? assigneeId, CancellationToken cancellationToken)
    {
        var index = tasks.FindIndex(task => task.Id == taskId);
        if (index < 0)
        {
            return Task.FromResult<TaskDetail?>(null);
        }

        tasks[index] = tasks[index] with { AssigneeId = assigneeId };
        return Task.FromResult<TaskDetail?>(tasks[index]);
    }

    public Task<TaskCommentItem?> CommentAsync(Guid userId, Guid taskId, string body, CancellationToken cancellationToken)
    {
        return Task.FromResult<TaskCommentItem?>(tasks.Any(task => task.Id == taskId)
            ? new TaskCommentItem(Guid.NewGuid(), body, "Worknexa Admin", DateTimeOffset.UtcNow)
            : null);
    }

    public Task<TaskDetail?> AddDependencyAsync(Guid userId, Guid taskId, Guid dependsOnTaskId, CancellationToken cancellationToken)
    {
        return Task.FromResult(tasks.FirstOrDefault(task => task.Id == taskId));
    }

    public Task<bool> RemoveDependencyAsync(Guid userId, Guid taskId, Guid dependencyId, CancellationToken cancellationToken)
    {
        return Task.FromResult(tasks.Any(task => task.Id == taskId));
    }

    public Task<TaskFile?> AddAttachmentAsync(Guid userId, Guid taskId, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        return Task.FromResult<TaskFile?>(tasks.Any(task => task.Id == taskId)
            ? new TaskFile(Guid.NewGuid(), fileName, contentType, 1)
            : null);
    }

    public Task<StoredAttachment?> OpenAttachmentAsync(Guid userId, Guid taskId, Guid attachmentId, CancellationToken cancellationToken)
    {
        return Task.FromResult<StoredAttachment?>(null);
    }

    public Task<bool> RemoveAttachmentAsync(Guid userId, Guid taskId, Guid attachmentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    private Task<TaskDetail?> Replace(Guid taskId, string title, string status, string priority)
    {
        var index = tasks.FindIndex(task => task.Id == taskId);
        if (index < 0)
        {
            return Task.FromResult<TaskDetail?>(null);
        }

        var current = tasks[index];
        var next = Detail(current.Id, title, status, priority, current.AssigneeId);
        tasks[index] = next;
        return Task.FromResult<TaskDetail?>(next);
    }

    private static TaskDetail Detail(Guid id, string title, string status, string priority, Guid? assigneeId)
    {
        return new TaskDetail(id, title, null, status, priority, null, null, assigneeId, null, null, null, null, null, null, null, null, [], [], [], [], [], null, null);
    }

    private static TaskListItem ToListItem(TaskDetail task)
    {
        return new TaskListItem(task.Id, task.Title, task.Status, task.Priority, task.StartAt, task.DueAt, task.AssigneeId, task.AssigneeName, task.ProjectId, task.ProjectName, task.ParentTaskId, task.TeamId, task.EstimatedHours, task.ActualHours, task.Tags);
    }
}

public sealed class MemoryTodoStore : ITodoStore
{
    private readonly List<TodoItemDto> items = [];

    public Task<IReadOnlyList<TodoItemDto>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<TodoItemDto>>(items.ToArray());
    }

    public Task<TodoItemDto> CreateAsync(Guid userId, TodoDraft draft, CancellationToken cancellationToken)
    {
        var item = Map(Guid.NewGuid(), draft, false);
        items.Add(item);
        return Task.FromResult(item);
    }

    public Task<TodoItemDto?> UpdateAsync(Guid userId, Guid todoId, TodoDraft draft, CancellationToken cancellationToken)
    {
        var index = items.FindIndex(item => item.Id == todoId);
        if (index < 0)
        {
            return Task.FromResult<TodoItemDto?>(null);
        }

        var next = Map(todoId, draft, items[index].IsCompleted);
        items[index] = next;
        return Task.FromResult<TodoItemDto?>(next);
    }

    public Task<TodoItemDto?> CompleteAsync(Guid userId, Guid todoId, TodoDraft? next, CancellationToken cancellationToken)
    {
        var index = items.FindIndex(item => item.Id == todoId);
        if (index < 0)
        {
            return Task.FromResult<TodoItemDto?>(null);
        }

        var completed = items[index] with { IsCompleted = true };
        items[index] = completed;
        if (next is not null)
        {
            items.Add(Map(Guid.NewGuid(), next, false));
        }

        return Task.FromResult<TodoItemDto?>(completed);
    }

    public Task<TodoItemDto?> ReopenAsync(Guid userId, Guid todoId, CancellationToken cancellationToken)
    {
        var index = items.FindIndex(item => item.Id == todoId);
        if (index < 0)
        {
            return Task.FromResult<TodoItemDto?>(null);
        }

        items[index] = items[index] with { IsCompleted = false };
        return Task.FromResult<TodoItemDto?>(items[index]);
    }

    public Task<bool> DeleteAsync(Guid userId, Guid todoId, CancellationToken cancellationToken)
    {
        return Task.FromResult(items.RemoveAll(item => item.Id == todoId) > 0);
    }

    private static TodoItemDto Map(Guid id, TodoDraft draft, bool completed)
    {
        return new TodoItemDto(id, draft.Title, completed, draft.Priority.ToString(), draft.Recurrence.ToString(), draft.DueAt, draft.RemindAt, "", draft.Tags);
    }
}

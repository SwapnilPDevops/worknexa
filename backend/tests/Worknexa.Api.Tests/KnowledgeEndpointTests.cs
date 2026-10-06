using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Worknexa.Application.Knowledge;
using Worknexa.Application.Meetings;

namespace Worknexa.Api.Tests;

public sealed class KnowledgeEndpointTests : IClassFixture<KnowledgeApplicationFactory>
{
    private readonly KnowledgeApplicationFactory _factory;

    public KnowledgeEndpointTests(KnowledgeApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MeetingNotes_RequireASignedInUser()
    {
        var response = await _factory.CreateClient().GetAsync("/api/meeting-notes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MeetingNotes_SaveSectionsAndConvertAnActionIntoATask()
    {
        var client = await AuthenticatedClient();
        var meetingId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var due = new DateTimeOffset(2026, 10, 9, 15, 0, 0, TimeSpan.Zero);
        var saved = await client.PutAsJsonAsync($"/api/meeting-notes/{meetingId}", new
        {
            agenda = "Review the board",
            discussion = "Walk the blockers\n- [ ] Confirm owners",
            decisions = "Ship the checklist",
            followUps = "Send the notes tomorrow",
            actions = new[]
            {
                new { title = "Send the notes", description = "Include the owners", assigneeId, dueAt = due, isCompleted = false }
            }
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var note = await saved.Content.ReadFromJsonAsync<Envelope<MeetingNoteBody>>();
        Assert.Equal("Walk the blockers\n- [ ] Confirm owners", note?.Data?.Discussion);
        Assert.Equal("Send the notes tomorrow", note?.Data?.FollowUps);
        var action = Assert.Single(note?.Data?.Actions ?? []);
        Assert.Equal("Include the owners", action.Description);

        var converted = await client.PostAsync($"/api/meeting-notes/{meetingId}/actions/{action.Id}/task", null);
        Assert.Equal(HttpStatusCode.OK, converted.StatusCode);
        var linked = await converted.Content.ReadFromJsonAsync<Envelope<MeetingNoteBody>>();
        var linkedAction = Assert.Single(linked?.Data?.Actions ?? []);
        Assert.NotNull(linkedAction.TaskId);
        Assert.Equal("Send the notes", linkedAction.Title);
        Assert.Equal(assigneeId, linkedAction.AssigneeId);
        Assert.Equal(due, linkedAction.DueAt);

        var store = _factory.Services.GetRequiredService<MemoryMeetingNoteStore>();
        var task = Assert.Single(store.Links, item => item.MeetingId == meetingId);
        Assert.Equal(linkedAction.TaskId, task.TaskId);
        Assert.Equal("Send the notes", task.Title);
        Assert.Equal("Include the owners", task.Description);
        Assert.Equal(assigneeId, task.AssigneeId);
        Assert.Equal(due, task.DueAt);
        Assert.Equal(note?.Data?.ProjectId, task.ProjectId);
    }

    [Fact]
    public async Task Notes_SaveSearchAndKeepFavoritePinnedTags()
    {
        var client = await AuthenticatedClient();
        var created = await client.PostAsJsonAsync("/api/notes", new
        {
            title = "Launch notes",
            content = "The lighthouse checklist stays with the team.",
            category = "Research",
            isFavorite = true,
            isPinned = true,
            tags = new[] { "launch" }
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var note = await created.Content.ReadFromJsonAsync<Envelope<NoteBody>>();
        Assert.True(note?.Data?.IsFavorite);
        Assert.True(note?.Data?.IsPinned);
        Assert.Contains("launch", note?.Data?.Tags ?? []);

        var found = await client.GetFromJsonAsync<Envelope<NoteBody[]>>("/api/notes?q=lighthouse");
        Assert.Contains(found?.Data ?? [], item => item.Title == "Launch notes" && item.Category == "Research");
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

    private sealed class MeetingNoteBody
    {
        public string? Discussion { get; set; }

        public string? FollowUps { get; set; }

        public Guid? ProjectId { get; set; }

        public MeetingActionBody[] Actions { get; set; } = [];
    }

    private sealed class MeetingActionBody
    {
        public Guid Id { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public Guid? AssigneeId { get; set; }

        public DateTimeOffset? DueAt { get; set; }

        public Guid? TaskId { get; set; }
    }

    private sealed class NoteBody
    {
        public string? Title { get; set; }

        public string? Category { get; set; }

        public bool IsFavorite { get; set; }

        public bool IsPinned { get; set; }

        public string[] Tags { get; set; } = [];
    }
}

public sealed class KnowledgeApplicationFactory : AuthApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMeetingNoteStore>();
            services.RemoveAll<INoteStore>();
            services.AddSingleton<MemoryMeetingNoteStore>();
            services.AddSingleton<IMeetingNoteStore>(provider => provider.GetRequiredService<MemoryMeetingNoteStore>());
            services.AddSingleton<MemoryNoteStore>();
            services.AddSingleton<INoteStore>(provider => provider.GetRequiredService<MemoryNoteStore>());
        });
    }
}

public sealed class MemoryMeetingNoteStore : IMeetingNoteStore
{
    private readonly List<StoredNote> items = [];

    public List<LinkedTask> Links { get; } = [];

    public Task<IReadOnlyList<MeetingNoteListItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        var rows = items.Where(item => item.UserId == userId).Select(item => item.Document);
        if (!string.IsNullOrEmpty(query))
        {
            rows = rows.Where(item => (item.Discussion?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) || item.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<MeetingNoteListItem>>(rows.Select(item => new MeetingNoteListItem(item.Id, item.Title, item.StartAt, item.UpdatedAt)).ToArray());
    }

    public Task<MeetingNoteDocument?> FindAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken)
    {
        return Task.FromResult(items.FirstOrDefault(item => item.UserId == userId && item.Document.Id == meetingId)?.Document);
    }

    public Task<MeetingNoteDocument?> SaveAsync(Guid userId, Guid meetingId, MeetingNoteWrite request, CancellationToken cancellationToken)
    {
        var existing = items.FirstOrDefault(item => item.UserId == userId && item.Document.Id == meetingId);
        var actions = (request.Actions ?? []).Select(action => new MeetingNoteAction(
            action.Id ?? Guid.NewGuid(),
            action.Title,
            action.Description,
            action.AssigneeId,
            null,
            action.DueAt,
            action.IsCompleted,
            null,
            null)).ToArray();
        var document = new MeetingNoteDocument(
            meetingId,
            existing?.Document.Title ?? "Meeting",
            existing?.Document.StartAt ?? DateTimeOffset.UtcNow,
            existing?.Document.ProjectId ?? Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            null,
            request.Agenda,
            request.Discussion,
            request.Decisions,
            request.FollowUps,
            request.FollowUpAt,
            actions,
            DateTimeOffset.UtcNow);
        if (existing is null)
        {
            items.Add(new StoredNote(userId, document));
        }
        else
        {
            var index = items.IndexOf(existing);
            items[index] = new StoredNote(userId, document);
        }

        return Task.FromResult<MeetingNoteDocument?>(document);
    }

    public Task<MeetingNoteDocument?> ConvertAsync(Guid userId, Guid meetingId, Guid actionId, CancellationToken cancellationToken)
    {
        var existing = items.FirstOrDefault(item => item.UserId == userId && item.Document.Id == meetingId);
        if (existing is null)
        {
            return Task.FromResult<MeetingNoteDocument?>(null);
        }

        var action = existing.Document.Actions.FirstOrDefault(item => item.Id == actionId);
        if (action is null)
        {
            return Task.FromResult<MeetingNoteDocument?>(null);
        }

        if (action.TaskId is null)
        {
            var taskId = Guid.NewGuid();
            Links.Add(new LinkedTask(taskId, meetingId, action.Title, action.Description, action.AssigneeId, action.DueAt, existing.Document.ProjectId));
            action = action with { TaskId = taskId, TaskTitle = action.Title };
        }

        var actions = existing.Document.Actions.Select(item => item.Id == action.Id ? action : item).ToArray();
        var document = existing.Document with { Actions = actions, UpdatedAt = DateTimeOffset.UtcNow };
        items[items.IndexOf(existing)] = new StoredNote(userId, document);
        return Task.FromResult<MeetingNoteDocument?>(document);
    }

    private sealed record StoredNote(Guid UserId, MeetingNoteDocument Document);
}

public sealed class MemoryNoteStore : INoteStore
{
    private readonly List<Stored> items = [];

    public Task<NoteOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new NoteOptions([], [], [], [], []));
    }

    public Task<IReadOnlyList<NoteItem>> ListAsync(Guid userId, string? query, Guid? learningTopicId, bool learningOnly, bool favoritesOnly, CancellationToken cancellationToken)
    {
        var rows = items.Where(item => item.UserId == userId).Select(item => item.Note);
        if (learningTopicId is Guid topicId)
        {
            rows = rows.Where(item => item.LearningTopicId == topicId);
        }

        if (learningOnly)
        {
            rows = rows.Where(item => item.LearningTopicId is not null);
        }

        if (favoritesOnly)
        {
            rows = rows.Where(item => item.IsFavorite);
        }

        if (!string.IsNullOrEmpty(query))
        {
            rows = rows.Where(item =>
                item.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Content.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (item.Category?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                || item.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        return Task.FromResult<IReadOnlyList<NoteItem>>(rows.OrderByDescending(item => item.IsPinned).ThenByDescending(item => item.UpdatedAt).ToArray());
    }

    public Task<NoteItem?> FindAsync(Guid userId, Guid noteId, CancellationToken cancellationToken)
    {
        return Task.FromResult(items.FirstOrDefault(item => item.UserId == userId && item.Note.Id == noteId)?.Note);
    }

    public Task<NoteItem> CreateAsync(Guid userId, NoteWrite request, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var note = new NoteItem(Guid.NewGuid(), request.Title, request.Content, request.Category, request.IsFavorite, request.IsPinned, request.ProjectId, null, request.TaskId, null, request.MeetingId, null, request.LearningTopicId, null, tags, now, now);
        items.Add(new Stored(userId, note));
        return Task.FromResult(note);
    }

    public Task<NoteItem?> UpdateAsync(Guid userId, Guid noteId, NoteWrite request, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        var index = items.FindIndex(item => item.UserId == userId && item.Note.Id == noteId);
        if (index < 0)
        {
            return Task.FromResult<NoteItem?>(null);
        }

        var current = items[index].Note;
        var note = current with
        {
            Title = request.Title,
            Content = request.Content,
            Category = request.Category,
            IsFavorite = request.IsFavorite,
            IsPinned = request.IsPinned,
            Tags = tags,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        items[index] = new Stored(userId, note);
        return Task.FromResult<NoteItem?>(note);
    }

    public Task<bool> DeleteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken)
    {
        return Task.FromResult(items.RemoveAll(item => item.UserId == userId && item.Note.Id == noteId) > 0);
    }

    private sealed record Stored(Guid UserId, NoteItem Note);
}

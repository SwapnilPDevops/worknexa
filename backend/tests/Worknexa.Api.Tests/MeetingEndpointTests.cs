using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Worknexa.Application.Meetings;
using Worknexa.Domain.Enums;

namespace Worknexa.Api.Tests;

public sealed class MeetingEndpointTests : IClassFixture<MeetingApplicationFactory>
{
    private readonly MeetingApplicationFactory _factory;

    public MeetingEndpointTests(MeetingApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Meetings_RequireASignedInUser()
    {
        var response = await _factory.CreateClient().GetAsync("/api/meetings?view=today&date=2026-10-06&offsetMinutes=0");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Meetings_CreateAnUpcomingMeetingWithItsDuration()
    {
        var client = await AuthenticatedClient();
        var start = new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.FromMinutes(330));
        var created = await client.PostAsJsonAsync("/api/meetings", new
        {
            title = "Client roadmap",
            meetingType = "Client",
            startAt = start,
            durationMinutes = 45,
            location = "Studio",
            meetingLink = "https://meet.example/roadmap",
            agenda = "Walk the milestones",
            notes = "Bring the deck",
            decisions = "Ship the outline",
            participantIds = Array.Empty<Guid>(),
            actionItems = new[] { new { title = "Send the outline", isCompleted = false } }
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var meeting = await created.Content.ReadFromJsonAsync<Envelope<MeetingBody>>();
        Assert.Equal(45, meeting?.Data?.DurationMinutes);
        Assert.Equal(start.AddMinutes(45), meeting?.Data?.EndAt);

        var listed = await client.GetFromJsonAsync<Envelope<MeetingBody[]>>("/api/meetings?view=upcoming&date=2026-10-06&offsetMinutes=330");
        Assert.Contains(listed?.Data ?? [], item => item.Title == "Client roadmap" && item.DurationMinutes == 45);
    }

    [Fact]
    public async Task Standups_SaveLoadAndSearch()
    {
        var client = await AuthenticatedClient();
        var saved = await client.PostAsJsonAsync("/api/standups", new
        {
            date = "2026-10-07",
            yesterday = "Shipped the notes",
            today = "Review the plan",
            blockers = "Waiting on design",
            notes = "Bring the checklist",
            status = "Blocked"
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var board = await client.GetFromJsonAsync<Envelope<BoardBody>>("/api/standups?date=2026-10-07");
        Assert.Contains(board?.Data?.Lines ?? [], line => line.Mine && line.Notes == "Bring the checklist" && line.Status == "Blocked");

        var search = await client.GetFromJsonAsync<Envelope<HitBody[]>>("/api/standups/search?q=checklist");
        Assert.Contains(search?.Data ?? [], hit => hit.Date == "2026-10-07" && hit.Blockers == "Waiting on design");
    }

    [Fact]
    public async Task Standups_AttachToAStandupMeeting()
    {
        var client = await AuthenticatedClient();
        var start = new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.FromMinutes(330));
        var created = await client.PostAsJsonAsync("/api/meetings", new
        {
            title = "Morning standup",
            meetingType = "Standup",
            startAt = start,
            durationMinutes = 15,
            participantIds = Array.Empty<Guid>(),
            actionItems = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var board = await client.GetFromJsonAsync<Envelope<BoardBody>>("/api/standups?date=2026-11-02");
        Assert.Equal("Morning standup", board?.Data?.MeetingTitle);
        Assert.NotNull(board?.Data?.MeetingId);
    }

    [Fact]
    public async Task Standups_LetATeamMemberWriteWithoutManagingMeetings()
    {
        var client = await AuthenticatedClient("member@worknexa.local");
        var denied = await client.PostAsJsonAsync("/api/meetings", new
        {
            title = "Should stay closed",
            meetingType = "Team",
            startAt = DateTimeOffset.UtcNow.AddDays(1),
            durationMinutes = 30
        });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var saved = await client.PostAsJsonAsync("/api/standups", new
        {
            date = "2026-10-08",
            today = "Pair on the form",
            status = "Clear"
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
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

    private sealed class MeetingBody
    {
        public string? Title { get; set; }

        public int DurationMinutes { get; set; }

        public DateTimeOffset EndAt { get; set; }
    }

    private sealed class BoardBody
    {
        public Guid? MeetingId { get; set; }

        public string? MeetingTitle { get; set; }

        public LineBody[] Lines { get; set; } = [];
    }

    private sealed class LineBody
    {
        public bool Mine { get; set; }

        public string? Notes { get; set; }

        public string? Status { get; set; }
    }

    private sealed class HitBody
    {
        public string? Date { get; set; }

        public string? Blockers { get; set; }
    }
}

public sealed class MeetingApplicationFactory : AuthApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMeetingStore>();
            services.RemoveAll<IStandupStore>();
            services.AddSingleton<MemoryMeetingStore>();
            services.AddSingleton<IMeetingStore>(provider => provider.GetRequiredService<MemoryMeetingStore>());
            services.AddSingleton<MemoryStandupStore>();
            services.AddSingleton<IStandupStore>(provider => provider.GetRequiredService<MemoryStandupStore>());
        });
    }
}

public sealed class MemoryMeetingStore : IMeetingStore
{
    private readonly List<StoredMeeting> items = [];

    public Task<IReadOnlyList<MeetingListItem>> ListAsync(Guid userId, string view, DateOnly date, int offsetMinutes, CancellationToken cancellationToken)
    {
        var offset = TimeSpan.FromMinutes(offsetMinutes);
        var dayStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, offset);
        var dayEnd = dayStart.AddDays(1);
        var monthStart = new DateTimeOffset(date.Year, date.Month, 1, 0, 0, 0, offset);
        var visible = items.Where(item => item.UserId == userId).Select(item => item.Detail);
        visible = view switch
        {
            "today" => visible.Where(item => item.StartAt < dayEnd && item.EndAt > dayStart),
            "past" => visible.Where(item => item.EndAt <= dayStart),
            "calendar" => visible.Where(item => item.StartAt < monthStart.AddMonths(1) && item.EndAt > monthStart),
            _ => visible.Where(item => item.StartAt >= dayEnd)
        };
        return Task.FromResult<IReadOnlyList<MeetingListItem>>(visible.Select(ListItem).ToArray());
    }

    public Task<MeetingDetail?> FindAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken)
    {
        return Task.FromResult(items.FirstOrDefault(item => item.UserId == userId && item.Detail.Id == meetingId)?.Detail);
    }

    public Task<MeetingDetail> CreateAsync(Guid userId, MeetingWrite request, MeetingType meetingType, DateTimeOffset end, CancellationToken cancellationToken)
    {
        var detail = Map(Guid.NewGuid(), request, meetingType, end);
        items.Add(new StoredMeeting(userId, detail));
        return Task.FromResult(detail);
    }

    public Task<MeetingDetail?> UpdateAsync(Guid userId, Guid meetingId, MeetingWrite request, MeetingType meetingType, DateTimeOffset end, CancellationToken cancellationToken)
    {
        var index = items.FindIndex(item => item.UserId == userId && item.Detail.Id == meetingId);
        if (index < 0)
        {
            return Task.FromResult<MeetingDetail?>(null);
        }

        var detail = Map(meetingId, request, meetingType, end);
        items[index] = new StoredMeeting(userId, detail);
        return Task.FromResult<MeetingDetail?>(detail);
    }

    public Task<bool> DeleteAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken)
    {
        return Task.FromResult(items.RemoveAll(item => item.UserId == userId && item.Detail.Id == meetingId) > 0);
    }

    private static MeetingDetail Map(Guid id, MeetingWrite request, MeetingType meetingType, DateTimeOffset end)
    {
        return new MeetingDetail(
            id,
            request.Title,
            meetingType.ToString(),
            request.StartAt,
            end,
            MeetingTime.Minutes(request.StartAt, end),
            request.Location,
            request.MeetingLink,
            request.Agenda,
            request.Notes,
            request.Decisions,
            request.FollowUpAt,
            request.ProjectId,
            null,
            request.TeamId,
            null,
            [],
            (request.ActionItems ?? []).Select(item => new MeetingActionItemDto(item.Id ?? Guid.NewGuid(), item.Title, item.AssigneeId, null, item.IsCompleted, item.DueAt, item.Description, null)).ToArray(),
            true);
    }

    private static MeetingListItem ListItem(MeetingDetail detail)
    {
        return new MeetingListItem(detail.Id, detail.Title, detail.MeetingType, detail.StartAt, detail.EndAt, detail.DurationMinutes, detail.Location, detail.MeetingLink, detail.ProjectId, detail.ProjectName, detail.TeamId, detail.TeamName, detail.Participants);
    }

    private sealed record StoredMeeting(Guid UserId, MeetingDetail Detail);
}

public sealed class MemoryStandupStore : IStandupStore
{
    private readonly List<StoredBoard> boards = [];

    public Task<StandupBoard> BoardAsync(Guid userId, DateOnly date, Guid? teamId, CancellationToken cancellationToken)
    {
        var board = boards.FirstOrDefault(item => item.UserId == userId && item.TeamId == teamId && item.Date == date);
        var lines = board?.Lines ?? [];
        return Task.FromResult(new StandupBoard(date, board?.Id, teamId, null, board?.MeetingId, board?.MeetingTitle, lines));
    }

    public Task<StandupBoard> SaveAsync(Guid userId, StandupWrite request, StandupStatus status, CancellationToken cancellationToken)
    {
        var board = Find(userId, request.TeamId, request.Date);
        var line = new StandupLine(Guid.NewGuid(), userId, "You", request.Yesterday, request.Today, request.Blockers, request.Notes, status.ToString(), true);
        board.Lines.RemoveAll(item => item.UserId == userId);
        board.Lines.Add(line);
        return BoardAsync(userId, request.Date, request.TeamId, cancellationToken);
    }

    public Task<IReadOnlyList<StandupHit>> SearchAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        var hits = boards.Where(board => board.UserId == userId)
            .SelectMany(board => board.Lines.Select(line => new StandupHit(board.Date, line.MemberName, line.Yesterday, line.Today, line.Blockers, line.Notes, line.Status, null)))
            .Where(hit => query is null || Contains(hit, query))
            .ToArray();
        return Task.FromResult<IReadOnlyList<StandupHit>>(hits);
    }

    public Task LinkMeetingAsync(Guid userId, Guid? teamId, DateOnly date, Guid meetingId, string meetingTitle, CancellationToken cancellationToken)
    {
        var board = Find(userId, teamId, date);
        board.MeetingId = meetingId;
        board.MeetingTitle = meetingTitle;
        return Task.CompletedTask;
    }

    private StoredBoard Find(Guid userId, Guid? teamId, DateOnly date)
    {
        var board = boards.FirstOrDefault(item => item.UserId == userId && item.TeamId == teamId && item.Date == date);
        if (board is not null)
        {
            return board;
        }

        board = new StoredBoard { Id = Guid.NewGuid(), UserId = userId, TeamId = teamId, Date = date };
        boards.Add(board);
        return board;
    }

    private static bool Contains(StandupHit hit, string query)
    {
        return (hit.Yesterday?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || (hit.Today?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || (hit.Blockers?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || (hit.Notes?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || hit.MemberName.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StoredBoard
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid? TeamId { get; set; }

        public DateOnly Date { get; set; }

        public Guid? MeetingId { get; set; }

        public string? MeetingTitle { get; set; }

        public List<StandupLine> Lines { get; } = [];
    }
}

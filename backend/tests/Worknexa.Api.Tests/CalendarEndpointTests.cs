using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Worknexa.Application.Calendar;
using Worknexa.Domain.Enums;

namespace Worknexa.Api.Tests;

public sealed class CalendarEndpointTests : IClassFixture<CalendarApplicationFactory>
{
    private readonly CalendarApplicationFactory _factory;

    public CalendarEndpointTests(CalendarApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Calendar_RequiresASignedInUser()
    {
        var response = await _factory.CreateClient().GetAsync("/api/calendar?from=2026-10-06T00:00:00Z&to=2026-10-07T00:00:00Z");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Calendar_RepeatsAWeeklyEventInsideTheRange()
    {
        var client = await AuthenticatedClient();
        var start = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);
        var created = await client.PostAsJsonAsync("/api/calendar/events", new
        {
            title = "Planning hour",
            description = "Look ahead",
            kind = "Meeting",
            startAt = start,
            endAt = start.AddHours(1),
            isAllDay = false,
            location = "Room 2",
            meetingLink = "https://meet.example/planning",
            recurrence = "Weekly",
            participantIds = Array.Empty<Guid>()
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var from = Uri.EscapeDataString("2026-10-12T00:00:00Z");
        var to = Uri.EscapeDataString("2026-10-20T00:00:00Z");
        var listed = await client.GetFromJsonAsync<Envelope<EntryBody[]>>($"/api/calendar?from={from}&to={to}");
        var matches = listed?.Data?.Where(entry => entry.Title == "Planning hour").ToArray() ?? [];
        Assert.Equal(2, matches.Length);
        Assert.All(matches, entry => Assert.Equal("Meeting", entry.Kind));
        Assert.Contains(matches, entry => entry.StartAt == start.AddDays(7));
    }

    [Fact]
    public async Task Reminders_DispatchACustomReminderIntoNotifications()
    {
        var client = await AuthenticatedClient();
        var remindAt = DateTimeOffset.UtcNow.AddHours(-1);
        var created = await client.PostAsJsonAsync("/api/reminders", new
        {
            title = "Send the notes",
            description = "After the review",
            remindAt,
            recurrence = "Custom",
            recurrenceInterval = 2,
            priority = "High"
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var listed = await client.GetFromJsonAsync<Envelope<ReminderBody[]>>("/api/reminders");
        var reminder = listed?.Data?.Single(item => item.Title == "Send the notes");
        Assert.NotNull(reminder);
        Assert.True(reminder!.RemindAt > DateTimeOffset.UtcNow);
        Assert.Equal(remindAt.AddDays(2), reminder.RemindAt);

        var notifications = await client.GetFromJsonAsync<Envelope<NotificationBody[]>>("/api/notifications");
        Assert.Contains(notifications?.Data ?? [], item => item.Title == "Send the notes" && item.Link == "/reminders");
    }

    [Fact]
    public async Task Reminders_CompletedItemsDoNotNotify()
    {
        var client = await AuthenticatedClient();
        var created = await client.PostAsJsonAsync("/api/reminders", new
        {
            title = "Already handled",
            remindAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            recurrence = "None",
            priority = "Low"
        });
        var reminder = await created.Content.ReadFromJsonAsync<Envelope<ReminderBody>>();
        var completed = await client.PostAsync($"/api/reminders/{reminder!.Data!.Id}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);

        var notifications = await client.GetFromJsonAsync<Envelope<NotificationBody[]>>("/api/notifications");
        Assert.DoesNotContain(notifications?.Data ?? [], item => item.Title == "Already handled");
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

    private sealed class EntryBody
    {
        public string? Title { get; set; }

        public string? Kind { get; set; }

        public DateTimeOffset StartAt { get; set; }
    }

    private sealed class ReminderBody
    {
        public Guid Id { get; set; }

        public string? Title { get; set; }

        public DateTimeOffset RemindAt { get; set; }
    }

    private sealed class NotificationBody
    {
        public string? Title { get; set; }

        public string? Link { get; set; }
    }
}

public sealed class CalendarApplicationFactory : AuthApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICalendarStore>();
            services.RemoveAll<IReminderStore>();
            services.RemoveAll<INotificationStore>();
            services.AddSingleton<MemoryCalendarStore>();
            services.AddSingleton<ICalendarStore>(provider => provider.GetRequiredService<MemoryCalendarStore>());
            services.AddSingleton<MemoryReminderStore>();
            services.AddSingleton<IReminderStore>(provider => provider.GetRequiredService<MemoryReminderStore>());
            services.AddSingleton<MemoryNotificationStore>();
            services.AddSingleton<INotificationStore>(provider => provider.GetRequiredService<MemoryNotificationStore>());
        });
    }
}

public sealed class MemoryCalendarStore : ICalendarStore
{
    private readonly List<EventSeries> series = [];
    private readonly List<CalendarEventDetail> details = [];

    public Task<IReadOnlyList<EventSeries>> SeriesAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<EventSeries>>(series.Where(item => item.OrganizerId == userId).ToArray());
    }

    public Task<IReadOnlyList<CalendarTaskSpan>> TasksAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<CalendarTaskSpan>>([]);
    }

    public Task<IReadOnlyList<CalendarMeetingSpan>> MeetingsAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<CalendarMeetingSpan>>([]);
    }

    public Task<CalendarEventDetail?> FindAsync(Guid userId, Guid eventId, CancellationToken cancellationToken)
    {
        return Task.FromResult(details.FirstOrDefault(item => item.Id == eventId));
    }

    public Task<CalendarEventDetail> CreateAsync(Guid userId, CalendarEventWrite request, CalendarItemKind kind, Recurrence recurrence, CancellationToken cancellationToken)
    {
        var detail = Detail(Guid.NewGuid(), userId, request, kind, recurrence);
        details.Add(detail);
        series.Add(Series(detail, userId));
        return Task.FromResult(detail);
    }

    public Task<CalendarEventDetail?> UpdateAsync(Guid userId, Guid eventId, CalendarEventWrite request, CalendarItemKind kind, Recurrence recurrence, CancellationToken cancellationToken)
    {
        var index = details.FindIndex(item => item.Id == eventId);
        if (index < 0)
        {
            return Task.FromResult<CalendarEventDetail?>(null);
        }

        var detail = Detail(eventId, userId, request, kind, recurrence);
        details[index] = detail;
        series.RemoveAll(item => item.Id == eventId);
        series.Add(Series(detail, userId));
        return Task.FromResult<CalendarEventDetail?>(detail);
    }

    public Task<bool> DeleteAsync(Guid userId, Guid eventId, CancellationToken cancellationToken)
    {
        series.RemoveAll(item => item.Id == eventId);
        return Task.FromResult(details.RemoveAll(item => item.Id == eventId) > 0);
    }

    private static CalendarEventDetail Detail(Guid id, Guid userId, CalendarEventWrite request, CalendarItemKind kind, Recurrence recurrence)
    {
        return new CalendarEventDetail(
            id,
            request.Title,
            request.Description,
            kind.ToString(),
            request.StartAt,
            request.EndAt,
            request.IsAllDay,
            request.Location,
            request.MeetingLink,
            request.ProjectId,
            null,
            recurrence.ToString(),
            request.RecurrenceInterval,
            [],
            request.RemindAt,
            true);
    }

    private static EventSeries Series(CalendarEventDetail detail, Guid userId)
    {
        return new EventSeries(
            detail.Id,
            userId,
            detail.Title,
            detail.Description,
            detail.Kind,
            detail.StartAt,
            detail.EndAt,
            detail.IsAllDay,
            detail.Location,
            detail.MeetingLink,
            detail.ProjectId,
            detail.ProjectName,
            detail.Recurrence,
            detail.RecurrenceInterval);
    }
}

public sealed class MemoryReminderStore : IReminderStore
{
    private readonly List<ReminderItem> items = [];
    private readonly HashSet<Guid> sent = [];

    public Task<IReadOnlyList<ReminderItem>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ReminderItem>>(items.ToArray());
    }

    public Task<ReminderLinks> LinksAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ReminderLinks([], []));
    }

    public Task<ReminderItem> CreateAsync(Guid userId, ReminderDraft draft, CancellationToken cancellationToken)
    {
        var item = Map(Guid.NewGuid(), draft);
        items.Add(item);
        return Task.FromResult(item);
    }

    public Task<ReminderItem?> UpdateAsync(Guid userId, Guid reminderId, ReminderDraft draft, CancellationToken cancellationToken)
    {
        return Replace(reminderId, Map(reminderId, draft));
    }

    public Task<ReminderItem?> SetCompletedAsync(Guid userId, Guid reminderId, bool completed, CancellationToken cancellationToken)
    {
        var current = items.FirstOrDefault(item => item.Id == reminderId);
        return current is null ? Task.FromResult<ReminderItem?>(null) : Replace(reminderId, current with { IsCompleted = completed });
    }

    public Task<bool> DeleteAsync(Guid userId, Guid reminderId, CancellationToken cancellationToken)
    {
        return Task.FromResult(items.RemoveAll(item => item.Id == reminderId) > 0);
    }

    public Task<IReadOnlyList<ReminderItem>> DueAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ReminderItem>>(items.Where(item => !item.IsCompleted && !sent.Contains(item.Id) && item.RemindAt <= now).ToArray());
    }

    public Task SettleAsync(Guid userId, Guid reminderId, DateTimeOffset remindAt, bool isSent, CancellationToken cancellationToken)
    {
        var index = items.FindIndex(item => item.Id == reminderId);
        if (index >= 0)
        {
            items[index] = items[index] with { RemindAt = remindAt };
            if (isSent)
            {
                sent.Add(reminderId);
            }
            else
            {
                sent.Remove(reminderId);
            }
        }

        return Task.CompletedTask;
    }

    private Task<ReminderItem?> Replace(Guid reminderId, ReminderItem next)
    {
        var index = items.FindIndex(item => item.Id == reminderId);
        if (index < 0)
        {
            return Task.FromResult<ReminderItem?>(null);
        }

        items[index] = next;
        return Task.FromResult<ReminderItem?>(next);
    }

    private static ReminderItem Map(Guid id, ReminderDraft draft)
    {
        return new ReminderItem(id, draft.Title, draft.Description, draft.RemindAt, draft.Recurrence.ToString(), draft.RecurrenceInterval, draft.Priority.ToString(), false, draft.TaskId, null, draft.MeetingId, null, null);
    }
}

public sealed class MemoryNotificationStore : INotificationStore
{
    private readonly List<NotificationItem> items = [];

    public Task AddAsync(Guid userId, NotificationDraft draft, CancellationToken cancellationToken)
    {
        items.Add(new NotificationItem(Guid.NewGuid(), draft.Title, draft.Body, false, null, draft.Link, DateTimeOffset.UtcNow, draft.Type.ToString()));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NotificationItem>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<NotificationItem>>(items.ToArray());
    }

    public Task<bool> MarkReadAsync(Guid userId, Guid notificationId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var index = items.FindIndex(item => item.Id == notificationId);
        if (index < 0)
        {
            return Task.FromResult(false);
        }

        items[index] = items[index] with { IsRead = true, ReadAt = readAt };
        return Task.FromResult(true);
    }

    public Task MarkAllReadAsync(Guid userId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        for (var index = 0; index < items.Count; index++)
        {
            items[index] = items[index] with { IsRead = true, ReadAt = readAt };
        }

        return Task.CompletedTask;
    }

    public Task<bool> MarkUnreadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var index = items.FindIndex(item => item.Id == notificationId);
        if (index < 0)
        {
            return Task.FromResult(false);
        }

        items[index] = items[index] with { IsRead = false, ReadAt = null };
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<NotificationPreferenceItem>> PreferencesAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<NotificationPreferenceItem> preferences = Enum.GetValues<Worknexa.Domain.Enums.NotificationType>()
            .Select(type => new NotificationPreferenceItem(type.ToString(), true))
            .ToArray();
        return Task.FromResult(preferences);
    }

    public Task SetPreferenceAsync(Guid userId, Worknexa.Domain.Enums.NotificationType type, bool enabled, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task CollectSignalsAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

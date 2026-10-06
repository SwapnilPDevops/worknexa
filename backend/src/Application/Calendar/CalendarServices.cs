using FluentValidation;
using Worknexa.Application.Common;
using Worknexa.Application.Validation;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;

namespace Worknexa.Application.Calendar;

/// <summary>
/// A source of entries for the Worknexa calendar view. External calendars (Outlook, Google) do not
/// plug in here: they synchronize into local events through Integrations.ICalendarProvider.
/// </summary>
public interface ICalendarSource
{
    string Key { get; }

    bool CanWrite { get; }

    Task<IReadOnlyList<CalendarEntry>> ReadAsync(CalendarRead read, CancellationToken cancellationToken);
}

public interface ICalendarStore
{
    Task<IReadOnlyList<EventSeries>> SeriesAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<IReadOnlyList<CalendarTaskSpan>> TasksAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<IReadOnlyList<CalendarMeetingSpan>> MeetingsAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<CalendarEventDetail?> FindAsync(Guid userId, Guid eventId, CancellationToken cancellationToken);

    Task<CalendarEventDetail> CreateAsync(Guid userId, CalendarEventWrite request, CalendarItemKind kind, Recurrence recurrence, CancellationToken cancellationToken);

    Task<CalendarEventDetail?> UpdateAsync(Guid userId, Guid eventId, CalendarEventWrite request, CalendarItemKind kind, Recurrence recurrence, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid eventId, CancellationToken cancellationToken);
}

public interface ICalendarService
{
    Task<IReadOnlyList<CalendarEntry>> ListAsync(CalendarRead read, CancellationToken cancellationToken);

    Task<CalendarEventDetail> GetAsync(Guid userId, Guid eventId, CancellationToken cancellationToken);

    Task<CalendarEventDetail> CreateAsync(Guid userId, CalendarEventWrite request, CancellationToken cancellationToken);

    Task<CalendarEventDetail> UpdateAsync(Guid userId, Guid eventId, CalendarEventWrite request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid eventId, CancellationToken cancellationToken);
}

public sealed class LocalCalendarSource(ICalendarStore store) : ICalendarSource
{
    public string Key => CalendarSources.Local;

    public bool CanWrite => true;

    public async Task<IReadOnlyList<CalendarEntry>> ReadAsync(CalendarRead read, CancellationToken cancellationToken)
    {
        var entries = new List<CalendarEntry>();
        foreach (var series in await store.SeriesAsync(read.UserId, read.From, read.To, cancellationToken))
        {
            var recurrence = Enum.TryParse<Recurrence>(series.Recurrence, out var parsed) ? parsed : Recurrence.None;
            foreach (var (start, end) in DueSchedule.Occurrences(series.StartAt, series.EndAt, recurrence, series.RecurrenceInterval, read.From, read.To))
            {
                entries.Add(new CalendarEntry(
                    Key,
                    "event",
                    series.Id,
                    series.Kind,
                    series.Title,
                    series.Description,
                    start,
                    end,
                    series.IsAllDay,
                    series.Location,
                    series.MeetingLink,
                    series.ProjectId,
                    series.ProjectName,
                    series.OrganizerId == read.UserId && !series.ReadOnly));
            }
        }

        if (read.IncludeTasks)
        {
            foreach (var task in await store.TasksAsync(read.UserId, read.From, read.To, cancellationToken))
            {
                entries.Add(new CalendarEntry(
                    Key,
                    "task",
                    task.Id,
                    task.Deadline ? nameof(CalendarItemKind.Deadline) : nameof(CalendarItemKind.Task),
                    task.Title,
                    task.Description,
                    task.StartAt,
                    task.EndAt,
                    false,
                    null,
                    null,
                    task.ProjectId,
                    task.ProjectName,
                    false));
            }
        }

        if (read.IncludeMeetings)
        {
            foreach (var meeting in await store.MeetingsAsync(read.UserId, read.From, read.To, cancellationToken))
            {
                entries.Add(new CalendarEntry(
                    Key,
                    "meeting",
                    meeting.Id,
                    nameof(CalendarItemKind.Meeting),
                    meeting.Title,
                    meeting.Description,
                    meeting.StartAt,
                    meeting.EndAt,
                    false,
                    meeting.Location,
                    meeting.MeetingLink,
                    meeting.ProjectId,
                    meeting.ProjectName,
                    false));
            }
        }

        return entries
            .OrderBy(entry => entry.StartAt)
            .ThenBy(entry => entry.Title)
            .Take(500)
            .ToArray();
    }
}

public sealed class CalendarService(IEnumerable<ICalendarSource> sources, ICalendarStore store) : ICalendarService
{
    public async Task<IReadOnlyList<CalendarEntry>> ListAsync(CalendarRead read, CancellationToken cancellationToken)
    {
        if (read.To <= read.From || read.To - read.From > TimeSpan.FromDays(62))
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("to", "Choose a range of 62 days or less.")]);
        }

        read = read with { From = read.From.ToUniversalTime(), To = read.To.ToUniversalTime() };

        var lists = await Task.WhenAll(sources.Select(source => source.ReadAsync(read, cancellationToken)));
        return lists
            .SelectMany(items => items)
            .OrderBy(entry => entry.StartAt)
            .ThenBy(entry => entry.Title)
            .Take(500)
            .ToArray();
    }

    public async Task<CalendarEventDetail> GetAsync(Guid userId, Guid eventId, CancellationToken cancellationToken)
    {
        return await store.FindAsync(userId, eventId, cancellationToken) ?? throw new NotFoundException("Calendar event was not found.");
    }

    public Task<CalendarEventDetail> CreateAsync(Guid userId, CalendarEventWrite request, CancellationToken cancellationToken)
    {
        var (kind, recurrence) = Parse(request);
        return store.CreateAsync(userId, Normalize(request), kind, recurrence, cancellationToken);
    }

    public async Task<CalendarEventDetail> UpdateAsync(Guid userId, Guid eventId, CalendarEventWrite request, CancellationToken cancellationToken)
    {
        var (kind, recurrence) = Parse(request);
        return await store.UpdateAsync(userId, eventId, Normalize(request), kind, recurrence, cancellationToken)
            ?? throw new NotFoundException("Calendar event was not found.");
    }

    public async Task DeleteAsync(Guid userId, Guid eventId, CancellationToken cancellationToken)
    {
        if (!await store.DeleteAsync(userId, eventId, cancellationToken))
        {
            throw new NotFoundException("Calendar event was not found.");
        }
    }

    private static (CalendarItemKind Kind, Recurrence Recurrence) Parse(CalendarEventWrite request)
    {
        return (
            TaskService.ParseRequired<CalendarItemKind>(request.Kind, "kind"),
            TaskService.ParseRequired<Recurrence>(request.Recurrence, "recurrence"));
    }

    private static CalendarEventWrite Normalize(CalendarEventWrite request)
    {
        var title = request.Title.Trim();
        var start = request.StartAt;
        var end = request.EndAt;
        if (request.IsAllDay && end <= start)
        {
            end = start.AddDays(1);
        }

        return request with
        {
            Title = title,
            Description = Blank(request.Description),
            Location = Blank(request.Location),
            MeetingLink = Blank(request.MeetingLink),
            StartAt = start,
            EndAt = end,
            ParticipantIds = request.ParticipantIds?.Distinct().Take(24).ToArray() ?? []
        };
    }

    private static string? Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class CalendarEventWriteValidator : AbstractValidator<CalendarEventWrite>
{
    public CalendarEventWriteValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Description).MaximumLength(8000);
        RuleFor(request => request.Kind).NotEmpty();
        RuleFor(request => request.Recurrence).NotEmpty();
        RuleFor(request => request.Location).MaximumLength(300);
        RuleFor(request => request.MeetingLink).MaximumLength(500).WebUrl();
        RuleFor(request => request.EndAt).GreaterThanOrEqualTo(request => request.StartAt);
        RuleFor(request => request.RecurrenceInterval)
            .NotNull()
            .InclusiveBetween(1, 366)
            .When(request => request.Recurrence == nameof(Recurrence.Custom));
    }
}

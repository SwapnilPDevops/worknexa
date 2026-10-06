using Worknexa.Domain.Enums;

namespace Worknexa.Application.Calendar;

public static class CalendarSources
{
    public const string Local = "local";
}

public sealed record CalendarRead(
    Guid UserId,
    DateTimeOffset From,
    DateTimeOffset To,
    bool IncludeTasks,
    bool IncludeMeetings);

public sealed record CalendarEntry(
    string Source,
    string Origin,
    Guid OriginId,
    string Kind,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    bool IsAllDay,
    string? Location,
    string? MeetingLink,
    Guid? ProjectId,
    string? ProjectName,
    bool Editable);

public sealed record CalendarEventDetail(
    Guid Id,
    string Title,
    string? Description,
    string Kind,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    bool IsAllDay,
    string? Location,
    string? MeetingLink,
    Guid? ProjectId,
    string? ProjectName,
    string Recurrence,
    int? RecurrenceInterval,
    IReadOnlyList<Work.NamedRef> Participants,
    DateTimeOffset? RemindAt,
    bool Editable);

public sealed record CalendarEventWrite(
    string Title,
    string? Description,
    string Kind,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    bool IsAllDay,
    string? Location,
    string? MeetingLink,
    Guid? ProjectId,
    string Recurrence,
    int? RecurrenceInterval,
    IReadOnlyList<Guid>? ParticipantIds,
    DateTimeOffset? RemindAt);

public sealed record EventSeries(
    Guid Id,
    Guid OrganizerId,
    string Title,
    string? Description,
    string Kind,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    bool IsAllDay,
    string? Location,
    string? MeetingLink,
    Guid? ProjectId,
    string? ProjectName,
    string Recurrence,
    int? RecurrenceInterval,
    bool ReadOnly = false);

public sealed record CalendarTaskSpan(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    bool Deadline,
    Guid? ProjectId,
    string? ProjectName);

public sealed record CalendarMeetingSpan(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? Location,
    string? MeetingLink,
    Guid? ProjectId,
    string? ProjectName);

public sealed record ReminderItem(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset RemindAt,
    string Recurrence,
    int? RecurrenceInterval,
    string Priority,
    bool IsCompleted,
    Guid? TaskId,
    string? TaskTitle,
    Guid? MeetingId,
    string? MeetingTitle,
    Guid? CalendarEventId);

public sealed record ReminderWrite(
    string Title,
    string? Description,
    DateTimeOffset RemindAt,
    string Recurrence,
    int? RecurrenceInterval,
    string Priority,
    Guid? TaskId,
    Guid? MeetingId);

public sealed record ReminderDraft(
    string Title,
    string? Description,
    DateTimeOffset RemindAt,
    Domain.Enums.Recurrence Recurrence,
    int? RecurrenceInterval,
    Domain.Enums.WorkPriority Priority,
    Guid? TaskId,
    Guid? MeetingId);

public sealed record ReminderLinks(IReadOnlyList<Work.NamedRef> Tasks, IReadOnlyList<Work.NamedRef> Meetings);

public sealed record NotificationItem(
    Guid Id,
    string Title,
    string Body,
    bool IsRead,
    DateTimeOffset? ReadAt,
    string? Link,
    DateTimeOffset CreatedAt,
    string Type);

public sealed record NotificationDraft(
    string Title,
    string Body,
    string? Link,
    Guid? ReminderId,
    NotificationType Type = NotificationType.ReminderDue,
    Guid? SourceId = null);

public sealed record NotificationPreferenceItem(string Type, bool Enabled);

public sealed record NotificationPreferenceWrite(string Type, bool Enabled);

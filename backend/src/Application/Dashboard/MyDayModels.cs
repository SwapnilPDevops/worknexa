using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;

namespace Worknexa.Application.Dashboard;

public sealed record MyDayWindow(DateOnly LocalDate, DateTimeOffset DayStart, DateTimeOffset DayEnd)
{
    public static MyDayWindow Create(DateOnly localDate, int offsetMinutes)
    {
        var offset = TimeSpan.FromMinutes(offsetMinutes);
        var start = new DateTimeOffset(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, offset);
        return new MyDayWindow(localDate, start.ToUniversalTime(), start.AddDays(1).ToUniversalTime());
    }
}

public sealed record MyDayScope(bool Tasks, bool Meetings, bool Calendar, bool Teams, bool Projects, bool Learning)
{
    public static MyDayScope From(IReadOnlyList<string> permissions)
    {
        return new MyDayScope(
            permissions.Contains(PermissionCatalog.TasksRead),
            permissions.Contains(PermissionCatalog.MeetingsRead),
            permissions.Contains(PermissionCatalog.CalendarRead),
            permissions.Contains(PermissionCatalog.TeamsRead),
            permissions.Contains(PermissionCatalog.ProjectsRead),
            permissions.Contains(PermissionCatalog.LearningRead));
    }
}

public sealed record MyDayTaskRow(
    Guid Id,
    string Title,
    WorkTaskStatus Status,
    WorkPriority Priority,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt,
    string? ProjectName);

public sealed record MyDayTodoRow(Guid Id, string Title, WorkPriority Priority, DateTimeOffset? DueAt);

public sealed record MyDayMeetingRow(
    Guid Id,
    string Title,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? ProjectName,
    Guid? CalendarEventId,
    string OrganizerName,
    IReadOnlyList<string> ParticipantNames);

public sealed record MyDayEventRow(Guid Id, string Title, DateTimeOffset StartAt, DateTimeOffset EndAt);

public sealed record MyDayReminderRow(Guid Id, string Message, DateTimeOffset RemindAt);

public sealed record MyDayTeamTaskRow(Guid Id, string Title, string? AssigneeName, string? ProjectName, DateTimeOffset? CompletedAt);

public sealed record MyDayProjectRow(
    Guid Id,
    string Name,
    ProjectStatus Status,
    DateOnly? EndDate,
    int TotalTasks,
    int DoneTasks,
    int OverdueTasks);

public sealed record MyDayLearningRow(
    Guid Id,
    string Title,
    string? Description,
    LearningStatus Status,
    int Progress,
    string? Goal,
    int ResourceCount,
    int NoteCount,
    string? NextResourceTitle);

public sealed record MyDayTeamBundle(
    IReadOnlyList<MyDayTeamTaskRow> Blocked,
    IReadOnlyList<MyDayTeamTaskRow> WaitingForReview,
    IReadOnlyList<MyDayTeamTaskRow> NeedsOwner,
    IReadOnlyList<MyDayTeamTaskRow> RecentlyCompleted);

public sealed class MyDaySnapshot
{
    public IReadOnlyList<MyDayTaskRow> Tasks { get; init; } = [];

    public IReadOnlyList<MyDayTodoRow> Todos { get; init; } = [];

    public IReadOnlyList<MyDayMeetingRow> Meetings { get; init; } = [];

    public IReadOnlyList<MyDayEventRow> Events { get; init; } = [];

    public IReadOnlyList<MyDayReminderRow> Reminders { get; init; } = [];

    public MyDayTeamBundle Team { get; init; } = new([], [], [], []);

    public IReadOnlyList<MyDayProjectRow> Projects { get; init; } = [];

    public MyDayLearningRow? Learning { get; init; }
}

public sealed record MyDayCounts(int Tasks, int Completed, int Overdue, int Meetings, int Reminders, int TeamAttention);

public sealed record MyDayTaskItem(Guid Id, string Kind, string Title, string Priority, DateTimeOffset? DueAt, string? ProjectName);

public sealed record MyDayTaskGroups(
    IReadOnlyList<MyDayTaskItem> Overdue,
    IReadOnlyList<MyDayTaskItem> HighPriority,
    IReadOnlyList<MyDayTaskItem> Today,
    IReadOnlyList<MyDayTaskItem> Upcoming);

public sealed record MyDayTimelineItem(string Kind, Guid Id, string Title, DateTimeOffset At, DateTimeOffset? EndAt);

public sealed record MyDayMeetingItem(
    Guid Id,
    string Title,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? ProjectName,
    IReadOnlyList<string> Participants);

public sealed record MyDayMeetings(IReadOnlyList<MyDayMeetingItem> Today, MyDayMeetingItem? Upcoming);

public sealed record MyDayTeamItem(Guid Id, string Title, string? AssigneeName, string? ProjectName);

public sealed record MyDayTeam(
    IReadOnlyList<MyDayTeamItem> Blocked,
    IReadOnlyList<MyDayTeamItem> WaitingForReview,
    IReadOnlyList<MyDayTeamItem> NeedsOwner,
    IReadOnlyList<MyDayTeamItem> RecentlyCompleted);

public sealed record MyDayProjectItem(Guid Id, string Name, int Progress, DateOnly? Deadline, bool AtRisk);

public sealed record MyDayProjects(
    IReadOnlyList<MyDayProjectItem> Active,
    IReadOnlyList<MyDayProjectItem> UpcomingDeadlines,
    IReadOnlyList<MyDayProjectItem> AtRisk);

public sealed record MyDayLearning(Guid TopicId, string Title, int Progress, string Goal);

public sealed record MyDayResponse(
    string Summary,
    MyDayCounts Counts,
    IReadOnlyList<MyDayTimelineItem> Timeline,
    MyDayTaskGroups Tasks,
    MyDayMeetings Meetings,
    MyDayTeam Team,
    MyDayProjects Projects,
    MyDayLearning? Learning);

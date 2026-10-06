using FluentValidation;
using FluentValidation.Results;
using Worknexa.Application.Calendar;
using Worknexa.Application.Learning;
using Worknexa.Application.Organization;
using Worknexa.Application.Projects;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;

namespace Worknexa.Application.Analytics;

public sealed record AnalyticsQuery(int Weeks, int OffsetMinutes, Guid? TeamId);

public sealed record PersonalWeek(DateOnly WeekStart, int Completed, int Due, double? MeetingHours, double? FocusHours, double? LearningHours);

public sealed record PersonalAnalytics(
    int TasksCompleted,
    int TasksOverdue,
    int DueInPeriod,
    int CompletionRate,
    double? MeetingHours,
    double? FocusHours,
    double? LearningHours,
    IReadOnlyList<PersonalWeek> Weeks);

public sealed record TeamAnalytics(
    Guid? TeamId,
    IReadOnlyList<TeamListItem> Teams,
    int Assigned,
    int Completed,
    int Overdue,
    int Blocked,
    IReadOnlyList<TeamWorkloadRow> Members);

public sealed record ProjectProgressRow(
    Guid Id,
    string Name,
    string Status,
    int Progress,
    int? Elapsed,
    int OpenTasks,
    int BlockedTasks,
    DateOnly? TargetDate);

public sealed record ProjectTaskFlag(Guid TaskId, string Title, Guid ProjectId, string ProjectName, string? AssigneeName, DateTimeOffset? DueAt);

public sealed record ProjectAnalytics(
    int ActiveProjects,
    int CompletedTasks,
    int CountedTasks,
    int CompletionRate,
    int BlockedTasks,
    IReadOnlyList<ProjectProgressRow> Projects,
    IReadOnlyList<ProjectTaskFlag> Blocked,
    IReadOnlyList<ProjectTaskFlag> Deadlines);

public sealed record LearningWeek(DateOnly WeekStart, int ActiveDays);

public sealed record LearningAnalytics(
    int TopicsCompleted,
    int TopicsTotal,
    int Progress,
    int ActiveDays,
    int PeriodDays,
    IReadOnlyList<LearningWeek> Weeks);

public sealed record AnalyticsReport(
    DateOnly From,
    DateOnly To,
    int Weeks,
    PersonalAnalytics? Personal,
    TeamAnalytics? Team,
    ProjectAnalytics? Projects,
    LearningAnalytics? Learning);

public sealed record AnalyticsTaskRow(WorkTaskStatus Status, DateTimeOffset? DueAt, DateTimeOffset? CompletedAt);

public sealed record AnalyticsProjectTask(Guid ProjectId, ProjectTaskRow Task);

public interface IAnalyticsStore
{
    Task<IReadOnlyList<AnalyticsTaskRow>> AssignedTasksAsync(Guid userId, DateTimeOffset from, CancellationToken cancellationToken);

    Task<IReadOnlyList<AnalyticsProjectTask>> ProjectTasksAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<DateTimeOffset>> LearningActivityAsync(Guid userId, DateTimeOffset from, CancellationToken cancellationToken);
}

public interface IAnalyticsService
{
    Task<AnalyticsReport> GetAsync(Guid userId, IReadOnlyCollection<string> permissions, AnalyticsQuery query, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class AnalyticsService(
    IAnalyticsStore store,
    ICalendarService calendar,
    ITeamService teams,
    IProjectService projects,
    ILearningService learning) : IAnalyticsService
{
    private const int ListSize = 8;

    public async Task<AnalyticsReport> GetAsync(Guid userId, IReadOnlyCollection<string> permissions, AnalyticsQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        Validate(query);
        var period = AnalyticsPeriod.Create(now, query.Weeks, query.OffsetMinutes);
        var canCalendar = permissions.Contains(PermissionCatalog.CalendarRead);
        var entries = canCalendar ? await CalendarAsync(userId, period.Start, now, cancellationToken) : [];

        var personal = permissions.Contains(PermissionCatalog.TasksRead)
            ? AnalyticsMath.Personal(await store.AssignedTasksAsync(userId, period.Start, cancellationToken), canCalendar ? entries : null, period, now)
            : null;
        var team = permissions.Contains(PermissionCatalog.TeamsRead) ? await TeamAsync(userId, query.TeamId, cancellationToken) : null;
        var project = permissions.Contains(PermissionCatalog.ProjectsRead) ? await ProjectsAsync(userId, period, now, cancellationToken) : null;
        var learn = permissions.Contains(PermissionCatalog.LearningRead) ? await LearningAsync(userId, entries, period, cancellationToken) : null;

        return new AnalyticsReport(period.WeekStarts[0], period.Today, query.Weeks, personal, team, project, learn);
    }

    private async Task<List<CalendarEntry>> CalendarAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var entries = new List<CalendarEntry>();
        for (var cursor = from; cursor < to; cursor = cursor.AddDays(28))
        {
            var end = cursor.AddDays(28) < to ? cursor.AddDays(28) : to;
            entries.AddRange(await calendar.ListAsync(new CalendarRead(userId, cursor, end, false, true), cancellationToken));
        }

        return entries;
    }

    private async Task<TeamAnalytics> TeamAsync(Guid userId, Guid? teamId, CancellationToken cancellationToken)
    {
        var visible = await teams.ListAsync(userId, cancellationToken);
        var selected = visible.FirstOrDefault(team => team.Id == teamId) ?? visible.FirstOrDefault();
        var rows = selected is null ? [] : await teams.WorkloadAsync(userId, selected.Id, cancellationToken);
        return new TeamAnalytics(
            selected?.Id,
            visible,
            rows.Sum(row => row.Assigned),
            rows.Sum(row => row.Completed),
            rows.Sum(row => row.Overdue),
            rows.Sum(row => row.Blocked),
            rows);
    }

    private async Task<ProjectAnalytics> ProjectsAsync(Guid userId, AnalyticsPeriod period, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = (await projects.ListAsync(userId, null, cancellationToken))
            .Where(project => project.Status is not (nameof(ProjectStatus.Completed) or nameof(ProjectStatus.Cancelled)))
            .ToArray();
        var tasks = (await store.ProjectTasksAsync(active.Select(project => project.Id).ToArray(), cancellationToken))
            .GroupBy(row => row.ProjectId)
            .ToDictionary(group => group.Key, group => group.Select(row => row.Task).ToArray());

        var rows = new List<ProjectProgressRow>();
        var blocked = new List<(ProjectTaskFlag Flag, DateTimeOffset UpdatedAt)>();
        var deadlines = new List<ProjectTaskFlag>();
        int completed = 0, counted = 0;
        foreach (var project in active)
        {
            var owned = tasks.GetValueOrDefault(project.Id) ?? [];
            var metrics = ProjectMetrics.Build(owned, now);
            completed += metrics.CompletedTasks;
            counted += owned.Count(task => task.Status != WorkTaskStatus.Cancelled);
            rows.Add(new ProjectProgressRow(
                project.Id,
                project.Name,
                project.Status,
                project.Progress,
                AnalyticsMath.Elapsed(project.StartDate, project.TargetDate, period.Today),
                project.OpenTasks,
                metrics.BlockedTasks,
                project.TargetDate));
            blocked.AddRange(owned
                .Where(task => task.Status == WorkTaskStatus.Blocked)
                .Select(task => (new ProjectTaskFlag(task.Id, task.Title, project.Id, project.Name, task.AssigneeName, task.DueAt), task.UpdatedAt)));
            deadlines.AddRange(metrics.UpcomingDeadlines
                .Select(item => new ProjectTaskFlag(item.TaskId, item.Title, project.Id, project.Name, item.AssigneeName, item.DueAt)));
        }

        return new ProjectAnalytics(
            active.Length,
            completed,
            counted,
            ProjectMetrics.Percent(completed, counted),
            blocked.Count,
            rows.OrderByDescending(row => (row.Elapsed ?? row.Progress) - row.Progress)
                .ThenByDescending(row => row.BlockedTasks)
                .ThenBy(row => row.Progress)
                .Take(ListSize)
                .ToArray(),
            blocked.OrderBy(item => item.UpdatedAt).Take(ListSize).Select(item => item.Flag).ToArray(),
            deadlines.OrderBy(item => item.DueAt).Take(ListSize).ToArray());
    }

    private async Task<LearningAnalytics> LearningAsync(Guid userId, IReadOnlyList<CalendarEntry> entries, AnalyticsPeriod period, CancellationToken cancellationToken)
    {
        var dashboard = await learning.DashboardAsync(userId, period.Today, cancellationToken);
        var topics = await learning.ListAsync(userId, null, cancellationToken);
        var activity = (await store.LearningActivityAsync(userId, period.Start, cancellationToken))
            .Concat(entries.Where(entry => entry.Kind == nameof(CalendarItemKind.Learning)).Select(entry => entry.StartAt))
            .ToArray();
        var (activeDays, weeks) = AnalyticsMath.Consistency(activity, period);
        return new LearningAnalytics(
            topics.Count(topic => topic.Status == nameof(LearningStatus.Completed)),
            topics.Count,
            dashboard.Progress,
            activeDays,
            period.Days,
            weeks);
    }

    private static void Validate(AnalyticsQuery query)
    {
        if (query.Weeks is < 1 or > 12)
        {
            throw new ValidationException([new ValidationFailure("weeks", "Choose between 1 and 12 weeks.")]);
        }

        if (query.OffsetMinutes is < -840 or > 840)
        {
            throw new ValidationException([new ValidationFailure("offsetMinutes", "Offset is out of range.")]);
        }
    }
}

/// <summary>Weeks start on Monday in the user's local time zone; the last week is the current one.</summary>
public sealed record AnalyticsPeriod(DateOnly Today, TimeSpan Offset, IReadOnlyList<DateOnly> WeekStarts)
{
    public DateTimeOffset Start => Local(WeekStarts[0]);

    public int Days => Today.DayNumber - WeekStarts[0].DayNumber + 1;

    public static AnalyticsPeriod Create(DateTimeOffset now, int weeks, int offsetMinutes)
    {
        var offset = TimeSpan.FromMinutes(offsetMinutes);
        var today = DateOnly.FromDateTime(now.ToOffset(offset).DateTime);
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var starts = Enumerable.Range(0, weeks).Select(index => monday.AddDays(-7 * (weeks - 1 - index))).ToArray();
        return new AnalyticsPeriod(today, offset, starts);
    }

    public DateTimeOffset Local(DateOnly date)
    {
        return new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, Offset).ToUniversalTime();
    }

    public DateOnly LocalDate(DateTimeOffset stamp)
    {
        return DateOnly.FromDateTime(stamp.ToOffset(Offset).DateTime);
    }

    public int WeekIndex(DateTimeOffset stamp)
    {
        var date = LocalDate(stamp);
        if (date < WeekStarts[0] || date > Today)
        {
            return -1;
        }

        return (date.DayNumber - WeekStarts[0].DayNumber) / 7;
    }
}

public static class AnalyticsMath
{
    /// <summary>
    /// Completion rate covers tasks that fell due in the period. Focus time is calendar time blocked as
    /// Task or Personal; all-day items carry no duration and are excluded from every hour total.
    /// </summary>
    public static PersonalAnalytics Personal(
        IReadOnlyList<AnalyticsTaskRow> tasks,
        IReadOnlyList<CalendarEntry>? entries,
        AnalyticsPeriod period,
        DateTimeOffset now)
    {
        var counted = tasks.Where(task => task.Status != WorkTaskStatus.Cancelled).ToArray();
        var weeks = period.WeekStarts.Count;
        var completed = new int[weeks];
        var due = new int[weeks];
        foreach (var task in counted)
        {
            if (task.Status == WorkTaskStatus.Completed && task.CompletedAt is DateTimeOffset doneAt && period.WeekIndex(doneAt) is var doneWeek and >= 0)
            {
                completed[doneWeek]++;
            }

            if (task.DueAt is DateTimeOffset dueAt && dueAt <= now && period.WeekIndex(dueAt) is var dueWeek and >= 0)
            {
                due[dueWeek]++;
            }
        }

        var dueInPeriod = counted.Where(task => task.DueAt is DateTimeOffset dueAt && dueAt >= period.Start && dueAt <= now).ToArray();
        var overdue = counted.Count(task => task.Status != WorkTaskStatus.Completed && task.DueAt is DateTimeOffset dueAt && dueAt < now);

        var meeting = new double[weeks];
        var focus = new double[weeks];
        var learning = new double[weeks];
        foreach (var entry in entries ?? [])
        {
            var target = entry.Kind switch
            {
                nameof(CalendarItemKind.Meeting) => meeting,
                nameof(CalendarItemKind.Learning) => learning,
                nameof(CalendarItemKind.Task) or nameof(CalendarItemKind.Personal) => focus,
                _ => null
            };
            var index = period.WeekIndex(entry.StartAt);
            if (target is null || entry.IsAllDay || index < 0)
            {
                continue;
            }

            target[index] += Hours(entry.StartAt, entry.EndAt, now);
        }

        var hasCalendar = entries is not null;
        return new PersonalAnalytics(
            completed.Sum(),
            overdue,
            dueInPeriod.Length,
            ProjectMetrics.Percent(dueInPeriod.Count(task => task.Status == WorkTaskStatus.Completed), dueInPeriod.Length),
            hasCalendar ? Round(meeting.Sum()) : null,
            hasCalendar ? Round(focus.Sum()) : null,
            hasCalendar ? Round(learning.Sum()) : null,
            period.WeekStarts.Select((start, index) => new PersonalWeek(
                start,
                completed[index],
                due[index],
                hasCalendar ? Round(meeting[index]) : null,
                hasCalendar ? Round(focus[index]) : null,
                hasCalendar ? Round(learning[index]) : null)).ToArray());
    }

    public static (int ActiveDays, IReadOnlyList<LearningWeek> Weeks) Consistency(IEnumerable<DateTimeOffset> activity, AnalyticsPeriod period)
    {
        var days = activity
            .Where(stamp => period.WeekIndex(stamp) >= 0)
            .Select(period.LocalDate)
            .ToHashSet();
        var weeks = period.WeekStarts
            .Select(start => new LearningWeek(start, days.Count(day => day >= start && day < start.AddDays(7))))
            .ToArray();
        return (days.Count, weeks);
    }

    /// <summary>Share of the schedule already used, from start date to target date.</summary>
    public static int? Elapsed(DateOnly? start, DateOnly? target, DateOnly today)
    {
        if (start is not DateOnly from || target is not DateOnly to || to <= from)
        {
            return null;
        }

        var share = (today.DayNumber - from.DayNumber) * 100d / (to.DayNumber - from.DayNumber);
        return (int)Math.Round(Math.Clamp(share, 0, 100));
    }

    private static double Hours(DateTimeOffset start, DateTimeOffset end, DateTimeOffset now)
    {
        var finish = end < now ? end : now;
        return finish <= start ? 0 : Math.Min((finish - start).TotalHours, 24);
    }

    private static double Round(double hours)
    {
        return Math.Round(hours, 1);
    }
}

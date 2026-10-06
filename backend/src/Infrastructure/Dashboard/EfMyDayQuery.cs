using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Dashboard;
using Worknexa.Domain.Enums;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Dashboard;

public sealed class EfMyDayQuery(WorknexaDbContext context) : IMyDayQuery
{
    public async Task<MyDaySnapshot> LoadAsync(Guid userId, MyDayWindow window, MyDayScope scope, CancellationToken cancellationToken)
    {
        var horizon = window.DayEnd.AddDays(7);
        var meetings = scope.Meetings
            ? await LoadMeetingsAsync(userId, window, cancellationToken)
            : [];
        var excludedEvents = meetings
            .Where(meeting => meeting.CalendarEventId is not null)
            .Select(meeting => meeting.CalendarEventId!.Value)
            .ToArray();

        return new MyDaySnapshot
        {
            Tasks = scope.Tasks ? await LoadTasksAsync(userId, window, horizon, cancellationToken) : [],
            Todos = scope.Tasks ? await LoadTodosAsync(userId, horizon, cancellationToken) : [],
            Meetings = meetings,
            Events = scope.Calendar ? await LoadEventsAsync(userId, window, excludedEvents, cancellationToken) : [],
            Reminders = await LoadRemindersAsync(userId, window, cancellationToken),
            Team = scope.Teams ? await LoadTeamAsync(userId, window, cancellationToken) : new MyDayTeamBundle([], [], [], []),
            Projects = scope.Projects ? await LoadProjectsAsync(userId, window, cancellationToken) : [],
            Learning = scope.Learning ? await LoadLearningAsync(userId, cancellationToken) : null
        };
    }

    private async Task<IReadOnlyList<MyDayTaskRow>> LoadTasksAsync(
        Guid userId,
        MyDayWindow window,
        DateTimeOffset horizon,
        CancellationToken cancellationToken)
    {
        return await context.Tasks.AsNoTracking()
            .Where(task => task.AssigneeId == userId && task.Status != WorkTaskStatus.Cancelled)
            .Where(task =>
                (task.Status != WorkTaskStatus.Completed && (task.Priority == WorkPriority.High || task.Priority == WorkPriority.Critical))
                || (task.Status != WorkTaskStatus.Completed && task.DueAt != null && task.DueAt < horizon)
                || (task.CompletedAt != null && task.CompletedAt >= window.DayStart && task.CompletedAt < window.DayEnd))
            .OrderBy(task => task.DueAt)
            .Take(80)
            .Select(task => new MyDayTaskRow(
                task.Id,
                task.Title,
                task.Status,
                task.Priority,
                task.DueAt,
                task.CompletedAt,
                task.Project != null ? task.Project.Name : null))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<MyDayTodoRow>> LoadTodosAsync(Guid userId, DateTimeOffset horizon, CancellationToken cancellationToken)
    {
        return await context.TodoItems.AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsCompleted && item.DueAt != null && item.DueAt < horizon)
            .OrderBy(item => item.DueAt)
            .Take(40)
            .Select(item => new MyDayTodoRow(item.Id, item.Title, item.Priority, item.DueAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<MyDayMeetingRow>> LoadMeetingsAsync(Guid userId, MyDayWindow window, CancellationToken cancellationToken)
    {
        var until = window.DayEnd.AddDays(14);
        return await context.Meetings.AsNoTracking()
            .Where(meeting =>
                (meeting.OrganizerId == userId || meeting.Participants.Any(participant => participant.UserId == userId))
                && meeting.EndAt > window.DayStart
                && meeting.StartAt < until)
            .OrderBy(meeting => meeting.StartAt)
            .Take(15)
            .Select(meeting => new MyDayMeetingRow(
                meeting.Id,
                meeting.Title,
                meeting.StartAt,
                meeting.EndAt,
                meeting.Project != null ? meeting.Project.Name : null,
                meeting.CalendarEventId,
                meeting.Organizer.DisplayName,
                meeting.Participants.Select(participant => participant.User.DisplayName).ToList()))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<MyDayEventRow>> LoadEventsAsync(
        Guid userId,
        MyDayWindow window,
        IReadOnlyCollection<Guid> excludedEventIds,
        CancellationToken cancellationToken)
    {
        var events = context.CalendarEvents.AsNoTracking()
            .Where(item =>
                (item.OrganizerId == userId || item.Participants.Any(participant => participant.UserId == userId))
                && item.StartAt < window.DayEnd
                && item.EndAt > window.DayStart);
        if (excludedEventIds.Count > 0)
        {
            events = events.Where(item => !excludedEventIds.Contains(item.Id));
        }

        return await events
            .OrderBy(item => item.StartAt)
            .Take(20)
            .Select(item => new MyDayEventRow(item.Id, item.Title, item.StartAt, item.EndAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<MyDayReminderRow>> LoadRemindersAsync(Guid userId, MyDayWindow window, CancellationToken cancellationToken)
    {
        return await context.Reminders.AsNoTracking()
            .Where(reminder => reminder.UserId == userId && !reminder.IsCompleted && reminder.RemindAt >= window.DayStart && reminder.RemindAt < window.DayEnd)
            .OrderBy(reminder => reminder.RemindAt)
            .Take(20)
            .Select(reminder => new MyDayReminderRow(
                reminder.Id,
                reminder.Title != "" ? reminder.Title : reminder.Message ?? "Reminder",
                reminder.RemindAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<MyDayTeamBundle> LoadTeamAsync(Guid userId, MyDayWindow window, CancellationToken cancellationToken)
    {
        var teamIds = await context.TeamMembers.AsNoTracking()
            .Where(member => member.UserId == userId)
            .Select(member => member.TeamId)
            .ToListAsync(cancellationToken);
        if (teamIds.Count == 0)
        {
            return new MyDayTeamBundle([], [], [], []);
        }

        var recentStart = window.DayStart.AddDays(-1);
        var open = context.Tasks.AsNoTracking()
            .Where(task => task.TeamId != null && teamIds.Contains(task.TeamId.Value))
            .Where(task => task.Status != WorkTaskStatus.Completed && task.Status != WorkTaskStatus.Cancelled);

        var blocked = await ProjectTeam(open.Where(task =>
                task.Status == WorkTaskStatus.Blocked
                || task.Dependencies.Any(dependency =>
                    dependency.DependsOnTask.Status != WorkTaskStatus.Completed
                    && dependency.DependsOnTask.Status != WorkTaskStatus.Cancelled)))
            .ToListAsync(cancellationToken);
        var review = await ProjectTeam(open.Where(task =>
                task.Status == WorkTaskStatus.Review
                || (task.Status == WorkTaskStatus.InProgress
                    && task.CreatedBy == userId
                    && task.AssigneeId != null
                    && task.AssigneeId != userId)))
            .ToListAsync(cancellationToken);
        var needsOwner = await ProjectTeam(open.Where(task => task.AssigneeId == null))
            .ToListAsync(cancellationToken);
        var completed = await context.Tasks.AsNoTracking()
            .Where(task => task.TeamId != null && teamIds.Contains(task.TeamId.Value))
            .Where(task => task.Status == WorkTaskStatus.Completed && task.CompletedAt >= recentStart && task.CompletedAt < window.DayEnd)
            .OrderByDescending(task => task.CompletedAt)
            .Take(8)
            .Select(task => new MyDayTeamTaskRow(
                task.Id,
                task.Title,
                task.Assignee != null ? task.Assignee.DisplayName : null,
                task.Project != null ? task.Project.Name : null,
                task.CompletedAt))
            .ToListAsync(cancellationToken);

        return new MyDayTeamBundle(blocked, review, needsOwner, completed);
    }

    private async Task<IReadOnlyList<MyDayProjectRow>> LoadProjectsAsync(Guid userId, MyDayWindow window, CancellationToken cancellationToken)
    {
        return await context.Projects.AsNoTracking()
            .Where(project =>
                project.Members.Any(member => member.UserId == userId)
                && (project.Status == ProjectStatus.Active || project.Status == ProjectStatus.Planning))
            .OrderBy(project => project.EndDate)
            .Take(20)
            .Select(project => new MyDayProjectRow(
                project.Id,
                project.Name,
                project.Status,
                project.EndDate,
                project.Tasks.Count(task => task.Status != WorkTaskStatus.Cancelled),
                project.Tasks.Count(task => task.Status == WorkTaskStatus.Completed),
                project.Tasks.Count(task =>
                    task.Status != WorkTaskStatus.Completed
                    && task.Status != WorkTaskStatus.Cancelled
                    && task.DueAt != null
                    && task.DueAt < window.DayStart)))
            .ToListAsync(cancellationToken);
    }

    private async Task<MyDayLearningRow?> LoadLearningAsync(Guid userId, CancellationToken cancellationToken)
    {
        var topic = await context.LearningTopics.AsNoTracking()
            .Where(item => item.UserId == userId && item.Status != LearningStatus.Completed)
            .OrderBy(item => item.Status == LearningStatus.Learning || item.Status == LearningStatus.Practicing ? 0 : 1)
            .ThenByDescending(item => item.UpdatedAt)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Description,
                item.Status,
                item.Progress,
                item.Goal,
                ResourceCount = item.Resources.Count(),
                NoteCount = item.Notes.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (topic is null)
        {
            return null;
        }

        var nextResource = await context.LearningResources.AsNoTracking()
            .Where(resource => resource.LearningTopicId == topic.Id)
            .OrderBy(resource => resource.CreatedAt)
            .Select(resource => resource.Title)
            .FirstOrDefaultAsync(cancellationToken);

        var linkedNotes = await context.Notes.CountAsync(note => note.UserId == userId && note.LearningTopicId == topic.Id, cancellationToken);
        return new MyDayLearningRow(topic.Id, topic.Title, topic.Description, topic.Status, topic.Progress, topic.Goal, topic.ResourceCount, topic.NoteCount + linkedNotes, nextResource);
    }

    private static IQueryable<MyDayTeamTaskRow> ProjectTeam(IQueryable<Domain.Productivity.WorkTask> tasks)
    {
        return tasks
            .OrderBy(task => task.DueAt)
            .Take(8)
            .Select(task => new MyDayTeamTaskRow(
                task.Id,
                task.Title,
                task.Assignee != null ? task.Assignee.DisplayName : null,
                task.Project != null ? task.Project.Name : null,
                task.CompletedAt));
    }
}

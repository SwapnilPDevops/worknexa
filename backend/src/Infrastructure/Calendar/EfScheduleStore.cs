using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Calendar;
using Worknexa.Application.Common;
using Worknexa.Application.Integrations;
using Worknexa.Application.Work;
using Worknexa.Infrastructure.Integrations;
using Worknexa.Domain.Calendar;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Learning;
using Worknexa.Domain.Meetings;
using Worknexa.Domain.Platform;
using Worknexa.Domain.Productivity;
using Worknexa.Domain.Projects;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Calendar;

public sealed class EfCalendarStore(WorknexaDbContext context) : ICalendarStore
{
    public async Task<IReadOnlyList<EventSeries>> SeriesAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        return await context.CalendarEvents.AsNoTracking()
            .Where(item =>
                (item.OrganizerId == userId || item.Participants.Any(participant => participant.UserId == userId))
                && item.StartAt < to
                && (item.Recurrence != Recurrence.None || item.EndAt > from))
            .OrderBy(item => item.StartAt)
            .Take(200)
            .Select(item => new EventSeries(
                item.Id,
                item.OrganizerId,
                item.Title,
                item.Description,
                item.Kind.ToString(),
                item.StartAt,
                item.EndAt,
                item.IsAllDay,
                item.Location,
                item.MeetingLink,
                item.ProjectId,
                item.Project != null ? item.Project.Name : null,
                item.Recurrence.ToString(),
                item.RecurrenceInterval,
                context.ExternalLinks.Any(link =>
                    link.EntityType == EfCalendarSyncStore.EntityType
                    && link.LocalId == item.Id
                    && link.Origin == LinkOrigin.External
                    && link.Connection.Direction != SyncDirection.TwoWay)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarTaskSpan>> TasksAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        var projectIds = context.ProjectMembers.Where(member => member.UserId == userId).Select(member => member.ProjectId);
        var tasks = await context.Tasks.AsNoTracking()
            .Where(task =>
                task.Status != WorkTaskStatus.Cancelled
                && (task.StartAt != null || task.DueAt != null)
                && (task.AssigneeId == userId
                    || task.CreatedBy == userId
                    || (task.TeamId != null && teamIds.Contains(task.TeamId.Value))
                    || (task.ProjectId != null && projectIds.Contains(task.ProjectId.Value)))
                && ((task.DueAt != null && task.DueAt > from.AddDays(-31) && task.DueAt < to.AddDays(31))
                    || (task.StartAt != null && task.StartAt < to && (task.DueAt == null || task.DueAt > from))))
            .OrderBy(task => task.DueAt)
            .Take(200)
            .Select(task => new
            {
                task.Id,
                task.Title,
                task.Description,
                task.StartAt,
                task.DueAt,
                task.ProjectId,
                ProjectName = task.Project != null ? task.Project.Name : null
            })
            .ToListAsync(cancellationToken);

        return tasks
            .Select(task =>
            {
                var start = task.StartAt ?? task.DueAt!.Value;
                var end = task.DueAt ?? task.StartAt!.Value;
                if (end < start)
                {
                    end = start;
                }

                return new CalendarTaskSpan(task.Id, task.Title, task.Description, start, end, task.StartAt is null, task.ProjectId, task.ProjectName);
            })
            .Where(task => task.StartAt < to && task.EndAt > from)
            .ToArray();
    }

    public async Task<IReadOnlyList<CalendarMeetingSpan>> MeetingsAsync(Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        return await context.Meetings.AsNoTracking()
            .Where(meeting =>
                meeting.CalendarEventId == null
                && meeting.StartAt < to
                && meeting.EndAt > from
                && (meeting.OrganizerId == userId
                    || meeting.Participants.Any(participant => participant.UserId == userId)
                    || (meeting.TeamId != null && context.TeamMembers.Any(member => member.UserId == userId && member.TeamId == meeting.TeamId))))
            .OrderBy(meeting => meeting.StartAt)
            .Take(200)
            .Select(meeting => new CalendarMeetingSpan(
                meeting.Id,
                meeting.Title,
                meeting.Agenda ?? meeting.Description,
                meeting.StartAt,
                meeting.EndAt,
                meeting.Location,
                meeting.MeetingLink,
                meeting.ProjectId,
                meeting.Project != null ? meeting.Project.Name : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<CalendarEventDetail?> FindAsync(Guid userId, Guid eventId, CancellationToken cancellationToken)
    {
        var item = await context.CalendarEvents.AsNoTracking()
            .Where(calendarEvent => calendarEvent.Id == eventId && (calendarEvent.OrganizerId == userId || calendarEvent.Participants.Any(participant => participant.UserId == userId)))
            .Select(calendarEvent => new
            {
                calendarEvent.Id,
                calendarEvent.OrganizerId,
                calendarEvent.Title,
                calendarEvent.Description,
                Kind = calendarEvent.Kind.ToString(),
                calendarEvent.StartAt,
                calendarEvent.EndAt,
                calendarEvent.IsAllDay,
                calendarEvent.Location,
                calendarEvent.MeetingLink,
                calendarEvent.ProjectId,
                ProjectName = calendarEvent.Project != null ? calendarEvent.Project.Name : null,
                Recurrence = calendarEvent.Recurrence.ToString(),
                calendarEvent.RecurrenceInterval,
                Participants = calendarEvent.Participants.Select(participant => new NamedRef(participant.UserId, participant.User.DisplayName)).ToList(),
                ReadOnly = context.ExternalLinks.Any(link =>
                    link.EntityType == EfCalendarSyncStore.EntityType
                    && link.LocalId == calendarEvent.Id
                    && link.Origin == LinkOrigin.External
                    && link.Connection.Direction != SyncDirection.TwoWay)
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return null;
        }

        var remindAt = await context.Reminders.AsNoTracking()
            .Where(reminder => reminder.CalendarEventId == eventId && reminder.UserId == userId && !reminder.IsCompleted)
            .OrderBy(reminder => reminder.RemindAt)
            .Select(reminder => (DateTimeOffset?)reminder.RemindAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new CalendarEventDetail(
            item.Id,
            item.Title,
            item.Description,
            item.Kind,
            item.StartAt,
            item.EndAt,
            item.IsAllDay,
            item.Location,
            item.MeetingLink,
            item.ProjectId,
            item.ProjectName,
            item.Recurrence,
            item.RecurrenceInterval,
            item.Participants,
            remindAt,
            item.OrganizerId == userId && !item.ReadOnly);
    }

    public async Task<CalendarEventDetail> CreateAsync(Guid userId, CalendarEventWrite request, CalendarItemKind kind, Recurrence recurrence, CancellationToken cancellationToken)
    {
        await EnsureLinksAsync(request, cancellationToken);
        var calendarEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Kind = kind,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            IsAllDay = request.IsAllDay,
            Location = request.Location,
            MeetingLink = request.MeetingLink,
            ProjectId = request.ProjectId,
            Recurrence = recurrence,
            RecurrenceInterval = recurrence == Recurrence.Custom ? request.RecurrenceInterval : null,
            OrganizerId = userId
        };
        AddParticipants(calendarEvent, userId, request.ParticipantIds);
        context.CalendarEvents.Add(calendarEvent);
        await ApplyReminderAsync(userId, calendarEvent, request.RemindAt, recurrence, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return (await FindAsync(userId, calendarEvent.Id, cancellationToken))!;
    }

    public async Task<CalendarEventDetail?> UpdateAsync(Guid userId, Guid eventId, CalendarEventWrite request, CalendarItemKind kind, Recurrence recurrence, CancellationToken cancellationToken)
    {
        var calendarEvent = await context.CalendarEvents
            .Include(item => item.Participants)
            .FirstOrDefaultAsync(item => item.Id == eventId && item.OrganizerId == userId, cancellationToken);
        if (calendarEvent is null)
        {
            return null;
        }

        await EnsureWritableAsync(eventId, cancellationToken);
        await EnsureLinksAsync(request, cancellationToken);
        calendarEvent.Title = request.Title;
        calendarEvent.Description = request.Description;
        calendarEvent.Kind = kind;
        calendarEvent.StartAt = request.StartAt;
        calendarEvent.EndAt = request.EndAt;
        calendarEvent.IsAllDay = request.IsAllDay;
        calendarEvent.Location = request.Location;
        calendarEvent.MeetingLink = request.MeetingLink;
        calendarEvent.ProjectId = request.ProjectId;
        calendarEvent.Recurrence = recurrence;
        calendarEvent.RecurrenceInterval = recurrence == Recurrence.Custom ? request.RecurrenceInterval : null;
        SyncParticipants(calendarEvent, userId, request.ParticipantIds);
        await ApplyReminderAsync(userId, calendarEvent, request.RemindAt, recurrence, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, eventId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid eventId, CancellationToken cancellationToken)
    {
        var calendarEvent = await context.CalendarEvents.FirstOrDefaultAsync(item => item.Id == eventId && item.OrganizerId == userId, cancellationToken);
        if (calendarEvent is null)
        {
            return false;
        }

        await EnsureWritableAsync(eventId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        calendarEvent.DeletedAt = now;
        calendarEvent.DeletedBy = userId;
        var reminders = await context.Reminders.Where(reminder => reminder.CalendarEventId == eventId).ToListAsync(cancellationToken);
        foreach (var reminder in reminders)
        {
            reminder.DeletedAt = now;
            reminder.DeletedBy = userId;
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureWritableAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var owner = await context.ExternalLinks.AsNoTracking()
            .Where(link =>
                link.EntityType == EfCalendarSyncStore.EntityType
                && link.LocalId == eventId
                && link.Origin == LinkOrigin.External
                && link.Connection.Direction != SyncDirection.TwoWay)
            .Select(link => link.Connection.Provider)
            .FirstOrDefaultAsync(cancellationToken);
        if (owner is not null)
        {
            var name = IntegrationCatalog.Find(owner)?.Name ?? "an external calendar";
            throw new ConflictException($"This event is synced from {name} and is read-only here. Edit it there, or switch the connection to two-way sync.");
        }
    }

    private async Task EnsureLinksAsync(CalendarEventWrite request, CancellationToken cancellationToken)
    {
        if (request.ProjectId is Guid projectId && !await context.Projects.AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            throw new ConflictException("Choose a project that exists.");
        }

        var people = request.ParticipantIds?.Distinct().ToArray() ?? [];
        if (people.Length == 0)
        {
            return;
        }

        var found = await context.Users.CountAsync(user => people.Contains(user.Id) && user.IsActive, cancellationToken);
        if (found != people.Length)
        {
            throw new ConflictException("Choose people who are active in the workspace.");
        }
    }

    private static void AddParticipants(CalendarEvent calendarEvent, Guid organizerId, IReadOnlyList<Guid>? participantIds)
    {
        foreach (var userId in (participantIds ?? []).Distinct().Where(id => id != organizerId))
        {
            calendarEvent.Participants.Add(new EventParticipant
            {
                Id = Guid.NewGuid(),
                UserId = userId
            });
        }
    }

    private void SyncParticipants(CalendarEvent calendarEvent, Guid organizerId, IReadOnlyList<Guid>? participantIds)
    {
        var desired = (participantIds ?? []).Distinct().Where(id => id != organizerId).ToHashSet();
        foreach (var participant in calendarEvent.Participants.Where(participant => !desired.Contains(participant.UserId)).ToList())
        {
            context.EventParticipants.Remove(participant);
        }

        foreach (var userId in desired.Where(id => calendarEvent.Participants.All(participant => participant.UserId != id)))
        {
            calendarEvent.Participants.Add(new EventParticipant
            {
                Id = Guid.NewGuid(),
                UserId = userId
            });
        }
    }

    private async Task ApplyReminderAsync(Guid userId, CalendarEvent calendarEvent, DateTimeOffset? remindAt, Recurrence recurrence, CancellationToken cancellationToken)
    {
        var existing = await context.Reminders.Where(reminder => reminder.CalendarEventId == calendarEvent.Id && reminder.UserId == userId).ToListAsync(cancellationToken);
        if (remindAt is null)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var reminder in existing)
            {
                reminder.DeletedAt = now;
                reminder.DeletedBy = userId;
            }

            return;
        }

        var current = existing.FirstOrDefault(reminder => reminder.DeletedAt is null) ?? existing.FirstOrDefault();
        if (current is null)
        {
            context.Reminders.Add(new Reminder
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CalendarEventId = calendarEvent.Id,
                Title = calendarEvent.Title,
                RemindAt = remindAt.Value,
                Recurrence = recurrence,
                RecurrenceInterval = calendarEvent.RecurrenceInterval,
                Priority = WorkPriority.Medium
            });
            return;
        }

        current.DeletedAt = null;
        current.DeletedBy = null;
        current.Title = calendarEvent.Title;
        current.RemindAt = remindAt.Value;
        current.Recurrence = recurrence;
        current.RecurrenceInterval = calendarEvent.RecurrenceInterval;
        current.IsSent = false;
        current.IsCompleted = false;
    }
}

public sealed class EfReminderStore(WorknexaDbContext context) : IReminderStore
{
    public async Task<IReadOnlyList<ReminderItem>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var query = context.Reminders.AsNoTracking()
            .Where(reminder => reminder.UserId == userId)
            .OrderBy(reminder => reminder.IsCompleted)
            .ThenBy(reminder => reminder.RemindAt)
            .Take(200);
        return await Project(query).ToListAsync(cancellationToken);
    }

    public async Task<ReminderLinks> LinksAsync(Guid userId, CancellationToken cancellationToken)
    {
        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        var projectIds = context.ProjectMembers.Where(member => member.UserId == userId).Select(member => member.ProjectId);
        var tasks = await context.Tasks.AsNoTracking()
            .Where(task =>
                task.Status != WorkTaskStatus.Completed
                && task.Status != WorkTaskStatus.Cancelled
                && (task.AssigneeId == userId
                    || task.CreatedBy == userId
                    || (task.TeamId != null && teamIds.Contains(task.TeamId.Value))
                    || (task.ProjectId != null && projectIds.Contains(task.ProjectId.Value))))
            .OrderBy(task => task.Title)
            .Take(100)
            .Select(task => new NamedRef(task.Id, task.Title))
            .ToListAsync(cancellationToken);
        var meetings = await context.Meetings.AsNoTracking()
            .Where(meeting => meeting.OrganizerId == userId || meeting.Participants.Any(participant => participant.UserId == userId))
            .OrderByDescending(meeting => meeting.StartAt)
            .Take(100)
            .Select(meeting => new NamedRef(meeting.Id, meeting.Title))
            .ToListAsync(cancellationToken);
        return new ReminderLinks(tasks, meetings);
    }

    public async Task<ReminderItem> CreateAsync(Guid userId, ReminderDraft draft, CancellationToken cancellationToken)
    {
        await EnsureLinksAsync(userId, draft, cancellationToken);
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = draft.Title,
            Description = draft.Description,
            Message = draft.Title,
            RemindAt = draft.RemindAt,
            Recurrence = draft.Recurrence,
            RecurrenceInterval = draft.RecurrenceInterval,
            Priority = draft.Priority,
            TaskId = draft.TaskId,
            MeetingId = draft.MeetingId
        };
        context.Reminders.Add(reminder);
        await context.SaveChangesAsync(cancellationToken);
        return (await Project(context.Reminders.AsNoTracking().Where(item => item.Id == reminder.Id)).FirstAsync(cancellationToken));
    }

    public async Task<ReminderItem?> UpdateAsync(Guid userId, Guid reminderId, ReminderDraft draft, CancellationToken cancellationToken)
    {
        var reminder = await context.Reminders.FirstOrDefaultAsync(item => item.Id == reminderId && item.UserId == userId, cancellationToken);
        if (reminder is null)
        {
            return null;
        }

        await EnsureLinksAsync(userId, draft, cancellationToken);
        reminder.Title = draft.Title;
        reminder.Description = draft.Description;
        reminder.Message = draft.Title;
        reminder.RemindAt = draft.RemindAt;
        reminder.Recurrence = draft.Recurrence;
        reminder.RecurrenceInterval = draft.RecurrenceInterval;
        reminder.Priority = draft.Priority;
        reminder.TaskId = draft.TaskId;
        reminder.MeetingId = draft.MeetingId;
        reminder.IsSent = false;
        await context.SaveChangesAsync(cancellationToken);
        return await Project(context.Reminders.AsNoTracking().Where(item => item.Id == reminderId)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ReminderItem?> SetCompletedAsync(Guid userId, Guid reminderId, bool completed, CancellationToken cancellationToken)
    {
        var reminder = await context.Reminders.FirstOrDefaultAsync(item => item.Id == reminderId && item.UserId == userId, cancellationToken);
        if (reminder is null)
        {
            return null;
        }

        reminder.IsCompleted = completed;
        if (!completed)
        {
            reminder.IsSent = false;
        }

        await context.SaveChangesAsync(cancellationToken);
        return await Project(context.Reminders.AsNoTracking().Where(item => item.Id == reminderId)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid reminderId, CancellationToken cancellationToken)
    {
        var reminder = await context.Reminders.FirstOrDefaultAsync(item => item.Id == reminderId && item.UserId == userId, cancellationToken);
        if (reminder is null)
        {
            return false;
        }

        reminder.DeletedAt = DateTimeOffset.UtcNow;
        reminder.DeletedBy = userId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ReminderItem>> DueAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var query = context.Reminders.AsNoTracking()
            .Where(reminder => reminder.UserId == userId && !reminder.IsCompleted && !reminder.IsSent && reminder.RemindAt <= now)
            .OrderBy(reminder => reminder.RemindAt)
            .Take(30);
        return await Project(query).ToListAsync(cancellationToken);
    }

    public async Task SettleAsync(Guid userId, Guid reminderId, DateTimeOffset remindAt, bool isSent, CancellationToken cancellationToken)
    {
        var reminder = await context.Reminders.FirstOrDefaultAsync(item => item.Id == reminderId && item.UserId == userId, cancellationToken);
        if (reminder is null)
        {
            return;
        }

        reminder.RemindAt = remindAt;
        reminder.IsSent = isSent;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureLinksAsync(Guid userId, ReminderDraft draft, CancellationToken cancellationToken)
    {
        if (draft.TaskId is Guid taskId)
        {
            var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
            var projectIds = context.ProjectMembers.Where(member => member.UserId == userId).Select(member => member.ProjectId);
            var visible = await context.Tasks.AnyAsync(task =>
                task.Id == taskId
                && (task.AssigneeId == userId
                    || task.CreatedBy == userId
                    || (task.TeamId != null && teamIds.Contains(task.TeamId.Value))
                    || (task.ProjectId != null && projectIds.Contains(task.ProjectId.Value))), cancellationToken);
            if (!visible)
            {
                throw new ConflictException("Choose a task you can see.");
            }
        }

        if (draft.MeetingId is Guid meetingId)
        {
            var visible = await context.Meetings.AnyAsync(meeting =>
                meeting.Id == meetingId
                && (meeting.OrganizerId == userId || meeting.Participants.Any(participant => participant.UserId == userId)), cancellationToken);
            if (!visible)
            {
                throw new ConflictException("Choose a meeting you are part of.");
            }
        }
    }

    private static IQueryable<ReminderItem> Project(IQueryable<Reminder> reminders)
    {
        return reminders.Select(reminder => new ReminderItem(
            reminder.Id,
            reminder.Title != "" ? reminder.Title : reminder.Message ?? "Reminder",
            reminder.Description,
            reminder.RemindAt,
            reminder.Recurrence.ToString(),
            reminder.RecurrenceInterval,
            reminder.Priority.ToString(),
            reminder.IsCompleted,
            reminder.TaskId,
            reminder.Task != null ? reminder.Task.Title : null,
            reminder.MeetingId,
            reminder.Meeting != null ? reminder.Meeting.Title : null,
            reminder.CalendarEventId));
    }
}

public sealed class EfNotificationStore(WorknexaDbContext context) : INotificationStore
{
    public Task AddAsync(Guid userId, NotificationDraft draft, CancellationToken cancellationToken)
    {
        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = draft.Title,
            Body = draft.Body,
            Link = draft.Link,
            ReminderId = draft.ReminderId,
            Type = draft.Type,
            SourceId = draft.SourceId
        });
        return context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationItem>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.Notifications.AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(50)
            .Select(notification => new NotificationItem(
                notification.Id,
                notification.Title,
                notification.Body,
                notification.IsRead,
                notification.ReadAt,
                notification.Link,
                notification.CreatedAt,
                notification.Type.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var notification = await context.Notifications.FirstOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId, cancellationToken);
        if (notification is null)
        {
            return false;
        }

        notification.IsRead = true;
        notification.ReadAt = readAt;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkUnreadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await context.Notifications.FirstOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId, cancellationToken);
        if (notification is null)
        {
            return false;
        }

        notification.IsRead = false;
        notification.ReadAt = null;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<NotificationPreferenceItem>> PreferencesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var stored = await context.NotificationPreferences.AsNoTracking()
            .Where(preference => preference.UserId == userId)
            .ToListAsync(cancellationToken);
        return Enum.GetValues<NotificationType>()
            .Select(type => new NotificationPreferenceItem(type.ToString(), stored.FirstOrDefault(preference => preference.Type == type)?.IsEnabled ?? true))
            .ToArray();
    }

    public async Task SetPreferenceAsync(Guid userId, NotificationType type, bool enabled, CancellationToken cancellationToken)
    {
        var preference = await context.NotificationPreferences.FirstOrDefaultAsync(item => item.UserId == userId && item.Type == type, cancellationToken);
        if (preference is null)
        {
            context.NotificationPreferences.Add(new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                IsEnabled = enabled
            });
        }
        else
        {
            preference.IsEnabled = enabled;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CollectSignalsAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var disabled = await context.NotificationPreferences.AsNoTracking()
            .Where(preference => preference.UserId == userId && !preference.IsEnabled)
            .Select(preference => preference.Type)
            .ToListAsync(cancellationToken);
        var since = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var recent = await context.Notifications.AsNoTracking()
            .Where(notification => notification.UserId == userId && notification.CreatedAt >= since && notification.SourceId != null)
            .Select(notification => new { notification.Type, notification.SourceId })
            .ToListAsync(cancellationToken);
        var seen = recent.Select(notification => (notification.Type, notification.SourceId!.Value)).ToHashSet();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var tomorrow = today.AddDays(1);
        var soon = now.AddHours(1);

        var tasks = await context.Tasks.AsNoTracking()
            .Where(task => task.DueAt < now
                && task.Status != WorkTaskStatus.Completed
                && task.Status != WorkTaskStatus.Cancelled
                && (task.AssigneeId == userId || (task.AssigneeId == null && task.CreatedBy == userId)))
            .OrderBy(task => task.DueAt)
            .Select(task => new { task.Id, task.Title })
            .Take(20)
            .ToListAsync(cancellationToken);
        foreach (var task in tasks)
        {
            AddSignal(userId, NotificationType.TaskOverdue, task.Id, "Task overdue", $"{task.Title} is overdue.", "/tasks", disabled, seen);
        }

        var meetings = await context.Meetings.AsNoTracking()
            .Where(meeting => meeting.StartAt > now && meeting.StartAt <= soon
                && (meeting.OrganizerId == userId || meeting.Participants.Any(participant => participant.UserId == userId)))
            .OrderBy(meeting => meeting.StartAt)
            .Select(meeting => new { meeting.Id, meeting.Title, meeting.StartAt })
            .Take(20)
            .ToListAsync(cancellationToken);
        foreach (var meeting in meetings)
        {
            AddSignal(userId, NotificationType.MeetingApproaching, meeting.Id, "Meeting approaching", $"{meeting.Title} starts soon.", "/meetings", disabled, seen);
        }

        var projects = await context.Projects.AsNoTracking()
            .Where(project => project.EndDate != null
                && (project.EndDate == today || project.EndDate == tomorrow)
                && project.Status != ProjectStatus.Completed
                && project.Status != ProjectStatus.Cancelled
                && (project.OwnerId == userId || project.Members.Any(member => member.UserId == userId)))
            .OrderBy(project => project.EndDate)
            .Select(project => new { project.Id, project.Name, project.EndDate })
            .Take(20)
            .ToListAsync(cancellationToken);
        foreach (var project in projects)
        {
            AddSignal(userId, NotificationType.ProjectDeadline, project.Id, "Project deadline", $"{project.Name} is due {project.EndDate:yyyy-MM-dd}.", "/projects", disabled, seen);
        }

        var topics = await context.LearningTopics.AsNoTracking()
            .Where(topic => topic.UserId == userId
                && topic.TargetDate != null
                && (topic.TargetDate == today || topic.TargetDate == tomorrow)
                && topic.Status != LearningStatus.Completed)
            .OrderBy(topic => topic.TargetDate)
            .Select(topic => new { topic.Id, topic.Title, topic.TargetDate })
            .Take(20)
            .ToListAsync(cancellationToken);
        foreach (var topic in topics)
        {
            AddSignal(userId, NotificationType.LearningReminder, topic.Id, "Learning reminder", $"{topic.Title} is targeted for {topic.TargetDate:yyyy-MM-dd}.", "/learning", disabled, seen);
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private void AddSignal(
        Guid userId,
        NotificationType type,
        Guid sourceId,
        string title,
        string body,
        string link,
        IReadOnlyCollection<NotificationType> disabled,
        HashSet<(NotificationType Type, Guid SourceId)> seen)
    {
        if (disabled.Contains(type) || !seen.Add((type, sourceId)))
        {
            return;
        }

        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Body = body,
            Link = link,
            Type = type,
            SourceId = sourceId
        });
    }

    public async Task MarkAllReadAsync(Guid userId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var unread = await context.Notifications.Where(item => item.UserId == userId && !item.IsRead).ToListAsync(cancellationToken);
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = readAt;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

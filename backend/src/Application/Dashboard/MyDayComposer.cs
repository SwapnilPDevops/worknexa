using Worknexa.Domain.Enums;
using Worknexa.Domain.Productivity;

namespace Worknexa.Application.Dashboard;

public static class MyDayComposer
{
    private const int ListLimit = 5;

    public static MyDayResponse Compose(MyDaySnapshot snapshot, MyDayWindow window, DateTimeOffset now)
    {
        var horizon = window.DayEnd.AddDays(7);
        var tasks = new List<MyDayTaskItem>();
        var high = new List<MyDayTaskItem>();
        var today = new List<MyDayTaskItem>();
        var upcoming = new List<MyDayTaskItem>();
        var overdueCount = 0;
        var dueTodayCount = 0;
        var completedCount = 0;

        foreach (var task in snapshot.Tasks.OrderBy(task => task.DueAt))
        {
            var open = WorkConventions.IsOpen(task.Status);
            if (task.Status == WorkTaskStatus.Completed && task.CompletedAt >= window.DayStart && task.CompletedAt < window.DayEnd)
            {
                completedCount++;
            }

            var item = new MyDayTaskItem(task.Id, "task", task.Title, task.Priority.ToString(), task.DueAt, task.ProjectName);
            switch (BucketFor(open, task.Priority, task.DueAt, window, horizon))
            {
                case Bucket.Overdue:
                    overdueCount++;
                    tasks.Add(item);
                    break;
                case Bucket.High:
                    if (task.DueAt >= window.DayStart && task.DueAt < window.DayEnd)
                    {
                        dueTodayCount++;
                    }

                    high.Add(item);
                    break;
                case Bucket.Today:
                    dueTodayCount++;
                    today.Add(item);
                    break;
                case Bucket.Upcoming:
                    upcoming.Add(item);
                    break;
            }
        }

        foreach (var todo in snapshot.Todos.OrderBy(todo => todo.DueAt))
        {
            var item = new MyDayTaskItem(todo.Id, "todo", todo.Title, todo.Priority.ToString(), todo.DueAt, null);
            switch (BucketFor(true, todo.Priority, todo.DueAt, window, horizon))
            {
                case Bucket.Overdue:
                    overdueCount++;
                    tasks.Add(item);
                    break;
                case Bucket.Today:
                    dueTodayCount++;
                    today.Add(item);
                    break;
                case Bucket.Upcoming:
                    upcoming.Add(item);
                    break;
            }
        }

        var meetingsToday = snapshot.Meetings
            .Where(meeting => meeting.StartAt < window.DayEnd && meeting.EndAt > window.DayStart)
            .OrderBy(meeting => meeting.StartAt)
            .Select(ToMeeting)
            .ToArray();
        var upcomingMeeting = snapshot.Meetings
            .Where(meeting => meeting.StartAt >= now)
            .OrderBy(meeting => meeting.StartAt)
            .Select(ToMeeting)
            .FirstOrDefault();

        var timeline = new List<MyDayTimelineItem>();
        foreach (var meeting in meetingsToday)
        {
            timeline.Add(new MyDayTimelineItem("meeting", meeting.Id, meeting.Title, meeting.StartAt, meeting.EndAt));
        }

        foreach (var calendarEvent in snapshot.Events.OrderBy(item => item.StartAt))
        {
            timeline.Add(new MyDayTimelineItem("event", calendarEvent.Id, calendarEvent.Title, calendarEvent.StartAt, calendarEvent.EndAt));
        }

        foreach (var task in today.Concat(high).Where(task => task.DueAt >= window.DayStart && task.DueAt < window.DayEnd))
        {
            timeline.Add(new MyDayTimelineItem("deadline", task.Id, task.Title, task.DueAt!.Value, null));
        }

        foreach (var task in tasks.Where(task => task.DueAt >= window.DayStart && task.DueAt < window.DayEnd))
        {
            timeline.Add(new MyDayTimelineItem("deadline", task.Id, task.Title, task.DueAt!.Value, null));
        }

        foreach (var reminder in snapshot.Reminders.OrderBy(reminder => reminder.RemindAt))
        {
            timeline.Add(new MyDayTimelineItem("reminder", reminder.Id, reminder.Message, reminder.RemindAt, null));
        }

        var blockedIds = snapshot.Team.Blocked.Select(item => item.Id).ToHashSet();
        var reviewIds = snapshot.Team.WaitingForReview.Select(item => item.Id).ToHashSet();
        var blocked = TakeTeam(snapshot.Team.Blocked);
        var review = TakeTeam(snapshot.Team.WaitingForReview.Where(item => !blockedIds.Contains(item.Id)));
        var needsOwner = TakeTeam(snapshot.Team.NeedsOwner.Where(item => !blockedIds.Contains(item.Id) && !reviewIds.Contains(item.Id)));
        var completedTeam = TakeTeam(snapshot.Team.RecentlyCompleted);
        var projects = snapshot.Projects.Select(project => ToProject(project, window.LocalDate)).ToArray();
        var counts = new MyDayCounts(
            dueTodayCount,
            completedCount,
            overdueCount,
            meetingsToday.Length,
            snapshot.Reminders.Count,
            blocked.Count + review.Count + needsOwner.Count);

        return new MyDayResponse(
            Summarize(counts),
            counts,
            timeline.OrderBy(item => item.At).ThenBy(item => item.Title).ToArray(),
            new MyDayTaskGroups(Take(tasks), Take(high), Take(today), Take(upcoming)),
            new MyDayMeetings(meetingsToday, upcomingMeeting),
            new MyDayTeam(blocked, review, needsOwner, completedTeam),
            new MyDayProjects(
                projects.Where(project => snapshot.Projects.First(row => row.Id == project.Id).Status == ProjectStatus.Active).Take(ListLimit).ToArray(),
                projects.Where(project => project.Deadline >= window.LocalDate && project.Deadline <= window.LocalDate.AddDays(14)).Take(ListLimit).ToArray(),
                projects.Where(project => project.AtRisk).Take(ListLimit).ToArray()),
            ToLearning(snapshot.Learning));
    }

    public static string Summarize(MyDayCounts counts)
    {
        var parts = new List<string>();
        if (counts.Overdue > 0)
        {
            parts.Add(counts.Overdue == 1 ? "1 task is overdue" : $"{counts.Overdue} tasks are overdue");
        }

        if (counts.Tasks > 0)
        {
            parts.Add(counts.Tasks == 1 ? "1 task is due today" : $"{counts.Tasks} tasks are due today");
        }

        if (counts.Meetings > 0)
        {
            parts.Add(counts.Meetings == 1 ? "1 meeting is scheduled" : $"{counts.Meetings} meetings are scheduled");
        }

        if (counts.Reminders > 0)
        {
            parts.Add(counts.Reminders == 1 ? "1 reminder is set" : $"{counts.Reminders} reminders are set");
        }

        if (counts.TeamAttention > 0)
        {
            parts.Add(counts.TeamAttention == 1 ? "1 team item needs you" : $"{counts.TeamAttention} team items need you");
        }

        if (parts.Count == 0)
        {
            return counts.Completed == 0
                ? "You are clear for today."
                : counts.Completed == 1
                    ? "You finished 1 task today, and nothing else is waiting."
                    : $"You finished {counts.Completed} tasks today, and nothing else is waiting.";
        }

        return Join(parts) + ".";
    }

    private static MyDayLearning? ToLearning(MyDayLearningRow? topic)
    {
        if (topic is null)
        {
            return null;
        }

        var progress = Math.Clamp(topic.Progress, 0, 100);
        var goal = string.IsNullOrWhiteSpace(topic.Goal)
            ? topic.NextResourceTitle ?? (string.IsNullOrWhiteSpace(topic.Description) ? $"Continue {topic.Title}" : Trim(topic.Description))
            : topic.Goal.Trim();
        return new MyDayLearning(topic.Id, topic.Title, progress, goal);
    }

    private static MyDayProjectItem ToProject(MyDayProjectRow project, DateOnly localDate)
    {
        var progress = project.TotalTasks == 0 ? 0 : (int)Math.Round(project.DoneTasks * 100d / project.TotalTasks);
        var dueSoon = project.EndDate is not null && project.EndDate <= localDate.AddDays(7);
        var pastDue = project.EndDate is not null && project.EndDate < localDate;
        var atRisk = project.Status == ProjectStatus.Active && (pastDue || project.OverdueTasks > 0 || (dueSoon && progress < 70));
        return new MyDayProjectItem(project.Id, project.Name, progress, project.EndDate, atRisk);
    }

    private static MyDayMeetingItem ToMeeting(MyDayMeetingRow meeting)
    {
        var participants = new List<string>();
        if (!string.IsNullOrWhiteSpace(meeting.OrganizerName))
        {
            participants.Add(meeting.OrganizerName);
        }

        foreach (var name in meeting.ParticipantNames)
        {
            if (!participants.Contains(name, StringComparer.Ordinal))
            {
                participants.Add(name);
            }
        }

        return new MyDayMeetingItem(meeting.Id, meeting.Title, meeting.StartAt, meeting.EndAt, meeting.ProjectName, participants);
    }

    private static Bucket BucketFor(bool open, WorkPriority? priority, DateTimeOffset? dueAt, MyDayWindow window, DateTimeOffset horizon)
    {
        if (!open)
        {
            return Bucket.None;
        }

        if (dueAt is not null && dueAt < window.DayStart)
        {
            return Bucket.Overdue;
        }

        if (priority is WorkPriority value && WorkConventions.IsUrgent(value))
        {
            return Bucket.High;
        }

        if (dueAt is not null && dueAt < window.DayEnd)
        {
            return Bucket.Today;
        }

        if (dueAt is not null && dueAt < horizon)
        {
            return Bucket.Upcoming;
        }

        return Bucket.None;
    }

    private static IReadOnlyList<MyDayTaskItem> Take(List<MyDayTaskItem> items)
    {
        return items.Take(ListLimit).ToArray();
    }

    private static IReadOnlyList<MyDayTeamItem> TakeTeam(IEnumerable<MyDayTeamTaskRow> items)
    {
        return items.Take(ListLimit).Select(item => new MyDayTeamItem(item.Id, item.Title, item.AssigneeName, item.ProjectName)).ToArray();
    }

    private static string Join(IReadOnlyList<string> parts)
    {
        if (parts.Count == 1)
        {
            return parts[0];
        }

        if (parts.Count == 2)
        {
            return $"{parts[0]} and {parts[1]}";
        }

        return string.Join(", ", parts.Take(parts.Count - 1)) + ", and " + parts[^1];
    }

    private static string Trim(string value)
    {
        var compact = value.Trim();
        return compact.Length <= 160 ? compact : compact[..157] + "...";
    }

    private enum Bucket
    {
        None,
        Overdue,
        High,
        Today,
        Upcoming
    }
}

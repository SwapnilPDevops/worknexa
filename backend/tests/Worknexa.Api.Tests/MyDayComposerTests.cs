using Worknexa.Application.Dashboard;
using Worknexa.Domain.Enums;

namespace Worknexa.Api.Tests;

public sealed class MyDayComposerTests
{
    private static readonly MyDayWindow Window = MyDayWindow.Create(new DateOnly(2026, 10, 6), 0);

    private static readonly DateTimeOffset Now = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Compose_GroupsAttentionWithoutDuplicatingATask()
    {
        var overdueHigh = Task("Ship the fix", WorkPriority.High, Window.DayStart.AddHours(-2));
        var high = Task("Review the proposal", WorkPriority.High, Window.DayEnd.AddDays(2));
        var dueToday = Task("Write the brief", WorkPriority.Medium, Window.DayStart.AddHours(15));
        var later = Task("Plan next week", WorkPriority.Low, Window.DayEnd.AddDays(2));
        var done = new MyDayTaskRow(Guid.NewGuid(), "Close yesterday", WorkTaskStatus.Completed, WorkPriority.Medium, Window.DayStart.AddHours(-5), Window.DayStart.AddHours(1), null);

        var response = MyDayComposer.Compose(new MyDaySnapshot
        {
            Tasks = [overdueHigh, high, dueToday, later, done]
        }, Window, Now);

        Assert.Equal([overdueHigh.Id], response.Tasks.Overdue.Select(item => item.Id));
        Assert.Equal([high.Id], response.Tasks.HighPriority.Select(item => item.Id));
        Assert.Equal([dueToday.Id], response.Tasks.Today.Select(item => item.Id));
        Assert.Equal([later.Id], response.Tasks.Upcoming.Select(item => item.Id));
        Assert.Equal(1, response.Counts.Tasks);
        Assert.Equal(1, response.Counts.Overdue);
        Assert.Equal(1, response.Counts.Completed);
        Assert.Contains("1 task is overdue", response.Summary);
        Assert.Contains("1 task is due today", response.Summary);
    }

    [Fact]
    public void Compose_BuildsTheDayFromMeetingsEventsDeadlinesAndReminders()
    {
        var sharedEventId = Guid.NewGuid();
        var meetingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var reminderId = Guid.NewGuid();
        var task = Task("Send the notes", WorkPriority.Medium, Window.DayStart.AddHours(11));

        var response = MyDayComposer.Compose(new MyDaySnapshot
        {
            Tasks = [task],
            Meetings =
            [
                new MyDayMeetingRow(meetingId, "Standup", Window.DayStart.AddHours(9), Window.DayStart.AddHours(9).AddMinutes(15), "Platform", sharedEventId, "Worknexa Admin", ["Worknexa Member"])
            ],
            Events =
            [
                new MyDayEventRow(eventId, "Focus block", Window.DayStart.AddHours(13), Window.DayStart.AddHours(14))
            ],
            Reminders = [new MyDayReminderRow(reminderId, "Leave for the client", Window.DayStart.AddHours(16))]
        }, Window, Now);

        Assert.Equal([meetingId, task.Id, eventId, reminderId], response.Timeline.Select(item => item.Id));
        Assert.Equal("Platform", response.Meetings.Today[0].ProjectName);
        Assert.Equal(["Worknexa Admin", "Worknexa Member"], response.Meetings.Today[0].Participants);
        Assert.Equal(meetingId, response.Meetings.Upcoming?.Id);
        Assert.Equal(1, response.Counts.Meetings);
        Assert.Equal(1, response.Counts.Reminders);
    }

    [Fact]
    public void Compose_MarksProjectsThatNeedAttention()
    {
        var risky = new MyDayProjectRow(Guid.NewGuid(), "Launch", ProjectStatus.Active, new DateOnly(2026, 10, 8), 10, 2, 0);
        var overdueWork = new MyDayProjectRow(Guid.NewGuid(), "Migration", ProjectStatus.Active, new DateOnly(2026, 12, 1), 4, 4, 1);
        var calm = new MyDayProjectRow(Guid.NewGuid(), "Archive", ProjectStatus.Active, new DateOnly(2026, 12, 1), 10, 9, 0);

        var response = MyDayComposer.Compose(new MyDaySnapshot
        {
            Projects = [risky, overdueWork, calm]
        }, Window, Now);

        Assert.Equal([risky.Id, overdueWork.Id], response.Projects.AtRisk.Select(project => project.Id));
        Assert.Equal(20, response.Projects.Active.Single(project => project.Id == risky.Id).Progress);
        Assert.Contains(response.Projects.UpcomingDeadlines, project => project.Id == risky.Id);
        Assert.DoesNotContain(response.Projects.UpcomingDeadlines, project => project.Id == calm.Id);
    }

    [Fact]
    public void Summarize_SpeaksPlainlyWhenTheDayIsClear()
    {
        Assert.Equal("You are clear for today.", MyDayComposer.Summarize(new MyDayCounts(0, 0, 0, 0, 0, 0)));
        Assert.Equal(
            "You finished 2 tasks today, and nothing else is waiting.",
            MyDayComposer.Summarize(new MyDayCounts(0, 2, 0, 0, 0, 0)));
    }

    private static MyDayTaskRow Task(string title, WorkPriority priority, DateTimeOffset dueAt)
    {
        return new MyDayTaskRow(Guid.NewGuid(), title, WorkTaskStatus.Todo, priority, dueAt, null, null);
    }
}

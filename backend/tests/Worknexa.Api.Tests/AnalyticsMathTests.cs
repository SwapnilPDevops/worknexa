using Worknexa.Application.Analytics;
using Worknexa.Application.Calendar;
using Worknexa.Domain.Enums;

namespace Worknexa.Api.Tests;

public sealed class AnalyticsMathTests
{
    private static readonly TimeSpan India = TimeSpan.FromMinutes(330);
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Period_StartsOnLocalMondays()
    {
        var period = AnalyticsPeriod.Create(Now, 2, 330);

        Assert.Equal([new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 5)], period.WeekStarts);
        Assert.Equal(new DateOnly(2026, 10, 6), period.Today);
        Assert.Equal(9, period.Days);
        Assert.Equal(new DateTimeOffset(2026, 9, 27, 18, 30, 0, TimeSpan.Zero), period.Start);
    }

    [Fact]
    public void Personal_CountsWeeklyCompletionAndCalendarHours()
    {
        var period = AnalyticsPeriod.Create(Now, 2, 330);
        AnalyticsTaskRow[] tasks =
        [
            new(WorkTaskStatus.Completed, Local(2026, 10, 4, 18), Local(2026, 10, 5, 9)),
            new(WorkTaskStatus.InProgress, Local(2026, 10, 1, 18), null),
            new(WorkTaskStatus.Cancelled, Local(2026, 10, 5, 18), null),
            new(WorkTaskStatus.Todo, Local(2026, 10, 20, 18), null)
        ];
        CalendarEntry[] entries =
        [
            Entry(nameof(CalendarItemKind.Meeting), Local(2026, 10, 5, 10), 1),
            Entry(nameof(CalendarItemKind.Personal), Local(2026, 9, 29, 9), 2),
            Entry(nameof(CalendarItemKind.Learning), Local(2026, 10, 5, 0), 24, allDay: true),
            Entry(nameof(CalendarItemKind.Learning), Local(2026, 10, 5, 14), 1.5),
            Entry(nameof(CalendarItemKind.Deadline), Local(2026, 10, 5, 17), 1)
        ];

        var result = AnalyticsMath.Personal(tasks, entries, period, Now);

        Assert.Equal(1, result.TasksCompleted);
        Assert.Equal(1, result.TasksOverdue);
        Assert.Equal(2, result.DueInPeriod);
        Assert.Equal(50, result.CompletionRate);
        Assert.Equal(1, result.MeetingHours);
        Assert.Equal(2, result.FocusHours);
        Assert.Equal(1.5, result.LearningHours);
        Assert.Equal([0, 1], result.Weeks.Select(week => week.Completed));
        Assert.Equal([2, 0], result.Weeks.Select(week => week.Due));
        Assert.Equal(2, result.Weeks[0].FocusHours);
    }

    [Fact]
    public void Personal_LeavesHoursEmptyWithoutCalendarAccess()
    {
        var result = AnalyticsMath.Personal([], null, AnalyticsPeriod.Create(Now, 1, 0), Now);

        Assert.Null(result.MeetingHours);
        Assert.All(result.Weeks, week => Assert.Null(week.FocusHours));
    }

    [Fact]
    public void Consistency_CountsDistinctLocalDays()
    {
        var period = AnalyticsPeriod.Create(Now, 2, 330);

        var (activeDays, weeks) = AnalyticsMath.Consistency(
            [Local(2026, 9, 29, 9), Local(2026, 9, 29, 21), Local(2026, 10, 6, 8), Local(2026, 9, 1, 8)],
            period);

        Assert.Equal(2, activeDays);
        Assert.Equal([1, 1], weeks.Select(week => week.ActiveDays));
    }

    [Theory]
    [InlineData(2026, 10, 6, 50)]
    [InlineData(2026, 9, 1, 0)]
    [InlineData(2026, 12, 1, 100)]
    public void Elapsed_ClampsToTheSchedule(int year, int month, int day, int expected)
    {
        Assert.Equal(expected, AnalyticsMath.Elapsed(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 11), new DateOnly(year, month, day)));
    }

    [Fact]
    public void Elapsed_NeedsBothDates()
    {
        Assert.Null(AnalyticsMath.Elapsed(null, new DateOnly(2026, 10, 11), new DateOnly(2026, 10, 6)));
    }

    private static DateTimeOffset Local(int year, int month, int day, int hour)
    {
        return new DateTimeOffset(year, month, day, hour, 0, 0, India);
    }

    private static CalendarEntry Entry(string kind, DateTimeOffset start, double hours, bool allDay = false)
    {
        return new CalendarEntry(CalendarSources.Local, "event", Guid.NewGuid(), kind, kind, null, start, start.AddHours(hours), allDay, null, null, null, null, true);
    }
}

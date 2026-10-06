using Worknexa.Application.Dashboard;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;

namespace Worknexa.Api.Tests;

public sealed class WorkScheduleTests
{
    private static readonly MyDayWindow Window = MyDayWindow.Create(new DateOnly(2026, 10, 6), 0);

    [Fact]
    public void Bucket_PlacesUndatedWorkInTodayAndCompletedWorkAside()
    {
        Assert.Equal(DueBucket.Today, DueSchedule.Bucket(null, false, Window));
        Assert.Equal(DueBucket.Completed, DueSchedule.Bucket(Window.DayStart, true, Window));
    }

    [Fact]
    public void Bucket_SplitsTheWeekFromTuesday()
    {
        Assert.Equal(DueBucket.Overdue, DueSchedule.Bucket(Window.DayStart.AddMinutes(-1), false, Window));
        Assert.Equal(DueBucket.Today, DueSchedule.Bucket(Window.DayStart.AddHours(9), false, Window));
        Assert.Equal(DueBucket.Tomorrow, DueSchedule.Bucket(Window.DayEnd.AddHours(1), false, Window));
        Assert.Equal(DueBucket.ThisWeek, DueSchedule.Bucket(Window.DayEnd.AddDays(2), false, Window));
        Assert.Equal(DueBucket.Upcoming, DueSchedule.Bucket(Window.DayEnd.AddDays(6), false, Window));
    }

    [Theory]
    [InlineData(Recurrence.Daily, 1)]
    [InlineData(Recurrence.Weekly, 7)]
    public void Next_StepsByTheRecurrence(Recurrence recurrence, int days)
    {
        var due = new DateTimeOffset(2026, 10, 6, 17, 0, 0, TimeSpan.Zero);
        Assert.Equal(due.AddDays(days), DueSchedule.Next(due, recurrence));
    }

    [Fact]
    public void Next_StepsACustomInterval()
    {
        var due = new DateTimeOffset(2026, 10, 6, 17, 0, 0, TimeSpan.Zero);
        Assert.Equal(due.AddDays(3), DueSchedule.Next(due, Recurrence.Custom, 3));
        Assert.False(DueSchedule.CanAdvance(Recurrence.Custom));
    }

    [Fact]
    public void Occurrences_RepeatAcrossTheRequestedRange()
    {
        var start = new DateTimeOffset(2026, 10, 6, 9, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(1);
        var rangeStart = new DateTimeOffset(2026, 10, 8, 0, 0, 0, TimeSpan.Zero);
        var rangeEnd = new DateTimeOffset(2026, 10, 11, 0, 0, 0, TimeSpan.Zero);

        var daily = DueSchedule.Occurrences(start, end, Recurrence.Daily, null, rangeStart, rangeEnd);
        Assert.Equal(3, daily.Count);
        Assert.Equal(new DateTimeOffset(2026, 10, 8, 9, 0, 0, TimeSpan.Zero), daily[0].Start);

        var once = DueSchedule.Occurrences(start, end, Recurrence.None, null, rangeStart, rangeEnd);
        Assert.Empty(once);
    }

    [Fact]
    public void Next_StepsAMonth()
    {
        var due = new DateTimeOffset(2026, 1, 31, 9, 0, 0, TimeSpan.Zero);
        Assert.Equal(due.AddMonths(1), DueSchedule.Next(due, Recurrence.Monthly));
    }

    [Fact]
    public void Normalize_TrimsDuplicatesAndCapsTheList()
    {
        var names = Enumerable.Range(0, 20).Select(index => $"  Tag {index}  ").Append("tag 1");
        var normalized = TagNames.Normalize(names);
        Assert.Equal(12, normalized.Count);
        Assert.Equal("Tag 0", normalized[0]);
        Assert.DoesNotContain(normalized, name => name != name.Trim());
    }

    [Fact]
    public void CreatesCycle_DetectsALoopThroughEitherKindOfLink()
    {
        var origin = Guid.NewGuid();
        var next = Guid.NewGuid();
        var links = new Dictionary<Guid, IReadOnlyList<Guid>>
        {
            [next] = [origin]
        };

        Assert.True(WorkGraphs.CreatesCycle(origin, next, links));
        Assert.True(WorkGraphs.CreatesCycle(origin, origin, links));
        Assert.False(WorkGraphs.CreatesCycle(origin, Guid.NewGuid(), links));
    }
}

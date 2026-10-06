using Worknexa.Application.Leaves;
using Worknexa.Application.Organization;
using Worknexa.Domain.Enums;

namespace Worknexa.Api.Tests;

public sealed class TeamLeaveTests
{
    [Fact]
    public void LeaveDays_AreInclusive()
    {
        Assert.Equal(1, LeaveCalendar.Days(new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 6)));
        Assert.Equal(5, LeaveCalendar.Days(new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 10)));
    }

    [Fact]
    public void Workload_CountsOpenTaskStates()
    {
        var member = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
        var rows = TeamWorkload.Build(
            [(member, "Grace")],
            [
                (member, WorkTaskStatus.InProgress, now.AddDays(-1)),
                (member, WorkTaskStatus.Completed, now.AddDays(-2)),
                (member, WorkTaskStatus.Blocked, now.AddDays(2)),
                (member, WorkTaskStatus.Cancelled, now.AddDays(-1))
            ],
            now);

        var grace = Assert.Single(rows);
        Assert.Equal(3, grace.Assigned);
        Assert.Equal(1, grace.InProgress);
        Assert.Equal(1, grace.Completed);
        Assert.Equal(1, grace.Overdue);
        Assert.Equal(1, grace.Blocked);
    }

    [Fact]
    public void Warning_NamesTheLeaveThatCoversTheDeadline()
    {
        var message = LeaveCalendar.Warning("Grace", "Annual", new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 10));
        Assert.Contains("Grace", message);
        Assert.Contains("Annual", message);
        Assert.True(LeaveCalendar.Covers(new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 9)));
        Assert.False(LeaveCalendar.Covers(new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 11)));
    }
}

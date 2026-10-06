using Worknexa.Application.Projects;
using Worknexa.Domain.Enums;

namespace Worknexa.Api.Tests;

public sealed class ProjectMetricsTests
{
    [Fact]
    public void Dashboard_CountsOpenCompletedBlockedAndDeadlines()
    {
        var now = new DateTimeOffset(2026, 10, 6, 9, 0, 0, TimeSpan.Zero);
        var tasks = new[]
        {
            Row("Ship notes", WorkTaskStatus.Completed, "Ada", now.AddDays(-1)),
            Row("Fix login", WorkTaskStatus.Blocked, "Grace", now.AddDays(2)),
            Row("Write tests", WorkTaskStatus.Todo, null, now.AddDays(20)),
            Row("Drop scope", WorkTaskStatus.Cancelled, "Ada", now.AddDays(1))
        };

        var dashboard = ProjectMetrics.Build(tasks, now);

        Assert.Equal(33, dashboard.Progress);
        Assert.Equal(2, dashboard.OpenTasks);
        Assert.Equal(1, dashboard.CompletedTasks);
        Assert.Equal(1, dashboard.BlockedTasks);
        Assert.Single(dashboard.UpcomingDeadlines);
        Assert.Equal("Fix login", dashboard.UpcomingDeadlines[0].Title);
        Assert.Equal(2, dashboard.TeamWorkload.Count);
        Assert.Contains(dashboard.TeamWorkload, row => row.Name == "Grace" && row.BlockedTasks == 1);
        Assert.Contains(dashboard.TeamWorkload, row => row.Name == "Unassigned" && row.OpenTasks == 1);
    }

    private static ProjectTaskRow Row(string title, WorkTaskStatus status, string? assignee, DateTimeOffset due)
    {
        return new ProjectTaskRow(Guid.NewGuid(), title, status, assignee is null ? null : Guid.NewGuid(), assignee, due, due);
    }
}

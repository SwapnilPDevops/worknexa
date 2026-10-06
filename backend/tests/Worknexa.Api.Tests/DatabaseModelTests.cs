using Microsoft.EntityFrameworkCore;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Platform;
using Worknexa.Domain.Productivity;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Api.Tests;

public sealed class DatabaseModelTests
{
    private static readonly string[] ExpectedTables =
    [
        "Users",
        "Roles",
        "Permissions",
        "UserRoles",
        "RolePermissions",
        "Teams",
        "TeamMembers",
        "MemberCompetencies",
        "Tasks",
        "TaskComments",
        "TaskDependencies",
        "Tags",
        "TaskTags",
        "TodoItems",
        "TodoTags",
        "Reminders",
        "CalendarEvents",
        "EventParticipants",
        "Meetings",
        "MeetingParticipants",
        "MeetingNotes",
        "MeetingActionItems",
        "Standups",
        "StandupEntries",
        "Projects",
        "ProjectMembers",
        "LeaveTypes",
        "Leaves",
        "LearningTopics",
        "LearningResources",
        "LearningNotes",
        "Notes",
        "NoteTags",
        "Notifications",
        "NotificationPreferences",
        "ActivityLogs",
        "Attachments",
        "IntegrationConnections",
        "ExternalLinks",
        "SyncRuns",
        "SyncFailures"
    ];

    [Fact]
    public void Model_ContainsTheRelationalSchema()
    {
        var options = new DbContextOptionsBuilder<WorknexaDbContext>()
            .UseWorknexaPostgres("Host=localhost;Port=5432;Database=worknexa_db;Username=postgres;Password=model-only;Search Path=worknexa")
            .Options;

        using var context = new WorknexaDbContext(options);
        var tables = context.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Where(name => name is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(ExpectedTables.Length, tables.Count);
        foreach (var table in ExpectedTables)
        {
            Assert.Contains(table, tables);
        }

        var task = context.Model.FindEntityType(typeof(WorkTask));
        Assert.NotNull(task);
        var taskForeignKeys = task.GetForeignKeys().SelectMany(foreignKey => foreignKey.Properties).Select(property => property.Name).ToHashSet();
        Assert.Contains(nameof(WorkTask.ProjectId), taskForeignKeys);
        Assert.Contains(nameof(WorkTask.AssigneeId), taskForeignKeys);
        Assert.Contains(nameof(WorkTask.TeamId), taskForeignKeys);
        Assert.Contains(nameof(WorkTask.ParentTaskId), taskForeignKeys);
        Assert.NotEmpty(task.GetDeclaredQueryFilters());

        var activity = context.Model.FindEntityType(typeof(ActivityLog));
        Assert.NotNull(activity);
        Assert.Empty(activity.GetDeclaredQueryFilters());

        var user = context.Model.FindEntityType(typeof(User));
        Assert.NotNull(user);
        var emailIndex = user.GetIndexes().Single(index => index.Properties.Any(property => property.Name == nameof(User.Email)));
        Assert.True(emailIndex.IsUnique);

        var script = context.Database.GenerateCreateScript();
        Assert.Contains("CREATE TABLE worknexa.\"Tasks\"", script);
        Assert.Contains("CK_TaskDependencies_NotSelf", script);
        Assert.Contains("admin@worknexa.local", script);
        Assert.Contains("manager@worknexa.local", script);
        Assert.Contains("member@worknexa.local", script);
    }
}

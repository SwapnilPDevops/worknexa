namespace Worknexa.Domain.Identity;

public sealed record PermissionDefinition(string Key, string Description);

public static class RoleNames
{
    public const string Admin = "Admin";

    public const string Manager = "Manager";

    public const string TeamMember = "TeamMember";
}

public static class PermissionCatalog
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string TeamsRead = "teams.read";
    public const string TeamsManage = "teams.manage";
    public const string TasksRead = "tasks.read";
    public const string TasksManage = "tasks.manage";
    public const string ProjectsRead = "projects.read";
    public const string ProjectsManage = "projects.manage";
    public const string CalendarRead = "calendar.read";
    public const string CalendarManage = "calendar.manage";
    public const string MeetingsRead = "meetings.read";
    public const string MeetingsManage = "meetings.manage";
    public const string LeavesRead = "leaves.read";
    public const string LeavesManage = "leaves.manage";
    public const string LearningRead = "learning.read";
    public const string LearningManage = "learning.manage";
    public const string NotesRead = "notes.read";
    public const string NotesManage = "notes.manage";
    public const string NotificationsRead = "notifications.read";

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(UsersRead, "View users"),
        new(UsersManage, "Manage users"),
        new(TeamsRead, "View teams"),
        new(TeamsManage, "Manage teams"),
        new(TasksRead, "View tasks"),
        new(TasksManage, "Manage tasks"),
        new(ProjectsRead, "View projects"),
        new(ProjectsManage, "Manage projects"),
        new(CalendarRead, "View calendar events"),
        new(CalendarManage, "Manage calendar events"),
        new(MeetingsRead, "View meetings"),
        new(MeetingsManage, "Manage meetings"),
        new(LeavesRead, "View leave"),
        new(LeavesManage, "Manage leave"),
        new(LearningRead, "View learning"),
        new(LearningManage, "Manage learning"),
        new(NotesRead, "View notes"),
        new(NotesManage, "Manage notes"),
        new(NotificationsRead, "View notifications")
    ];

    public static readonly IReadOnlyList<string> TeamMemberKeys =
    [
        UsersRead,
        TeamsRead,
        TasksRead,
        TasksManage,
        ProjectsRead,
        CalendarRead,
        MeetingsRead,
        LeavesRead,
        LearningRead,
        LearningManage,
        NotesRead,
        NotesManage,
        NotificationsRead
    ];

    public static readonly IReadOnlyList<string> ManagerKeys =
    [
        ..TeamMemberKeys,
        TeamsManage,
        ProjectsManage,
        CalendarManage,
        MeetingsManage,
        LeavesManage
    ];

    public static IReadOnlyList<string> KeysForRole(string roleName)
    {
        return roleName switch
        {
            RoleNames.Admin => All.Select(permission => permission.Key).ToArray(),
            RoleNames.Manager => ManagerKeys,
            RoleNames.TeamMember => TeamMemberKeys,
            _ => []
        };
    }
}

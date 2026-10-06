namespace Worknexa.Domain.Enums;

public enum WorkTaskStatus
{
    Backlog,
    Todo,
    InProgress,
    Blocked,
    Review,
    Completed,
    Cancelled
}

public enum WorkPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum Recurrence
{
    None,
    Daily,
    Weekly,
    Monthly,
    Custom
}

public enum CalendarItemKind
{
    Meeting,
    Task,
    Personal,
    Learning,
    Deadline
}

public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Completed,
    Cancelled
}

public enum TeamMemberRole
{
    Owner,
    Member
}

public enum MemberStatus
{
    Active,
    OnLeave,
    Inactive
}

public enum MemberCompetencyKind
{
    Skill,
    Technology
}

public enum ProjectMemberRole
{
    Owner,
    Member,
    Viewer
}

public enum ParticipantResponse
{
    Pending,
    Accepted,
    Declined
}

public enum MeetingType
{
    OneOnOne,
    Team,
    Client,
    Project,
    Technical,
    Interview,
    Planning,
    Review,
    Standup
}

public enum StandupStatus
{
    Clear,
    Blocked,
    Away
}

public enum LeaveStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public enum LearningStatus
{
    NotStarted,
    Learning,
    Practicing,
    Completed
}

public enum LearningResourceType
{
    Article,
    Video,
    Course,
    Book,
    Other
}

public enum NotificationType
{
    TaskAssigned,
    TaskCompleted,
    TaskOverdue,
    MeetingApproaching,
    ReminderDue,
    ProjectDeadline,
    TeamMemberBlocked,
    LeaveUpdate,
    LearningReminder
}

public enum IntegrationCategory
{
    Calendar,
    Email,
    Messaging,
    SourceControl,
    ProjectManagement
}

public enum IntegrationStatus
{
    Connected,
    NeedsReconnect,
    Error
}

/// <summary>Which way changes flow. Each linked item is owned by the side it came from.</summary>
public enum SyncDirection
{
    ImportOnly,
    ExportOnly,
    TwoWay
}

public enum LinkOrigin
{
    Local,
    External
}

public enum SyncTrigger
{
    Manual,
    Scheduled
}

public enum SyncRunStatus
{
    Running,
    Succeeded,
    PartiallySucceeded,
    Failed
}

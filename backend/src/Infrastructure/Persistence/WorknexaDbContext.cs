using Microsoft.EntityFrameworkCore;
using Worknexa.Domain.Calendar;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Integrations;
using Worknexa.Domain.Knowledge;
using Worknexa.Domain.Learning;
using Worknexa.Domain.Leaves;
using Worknexa.Domain.Meetings;
using Worknexa.Domain.Organization;
using Worknexa.Domain.Platform;
using Worknexa.Domain.Productivity;
using Worknexa.Domain.Projects;
using Worknexa.Infrastructure.Persistence.Seed;

namespace Worknexa.Infrastructure.Persistence;

public sealed class WorknexaDbContext : DbContext
{
    public WorknexaDbContext(DbContextOptions<WorknexaDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    public DbSet<MemberCompetency> MemberCompetencies => Set<MemberCompetency>();

    public DbSet<WorkTask> Tasks => Set<WorkTask>();

    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<TaskTag> TaskTags => Set<TaskTag>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public DbSet<TodoTag> TodoTags => Set<TodoTag>();

    public DbSet<Reminder> Reminders => Set<Reminder>();

    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();

    public DbSet<Meeting> Meetings => Set<Meeting>();

    public DbSet<MeetingParticipant> MeetingParticipants => Set<MeetingParticipant>();

    public DbSet<MeetingNote> MeetingNotes => Set<MeetingNote>();

    public DbSet<MeetingActionItem> MeetingActionItems => Set<MeetingActionItem>();

    public DbSet<Standup> Standups => Set<Standup>();

    public DbSet<StandupEntry> StandupEntries => Set<StandupEntry>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();

    public DbSet<Leave> Leaves => Set<Leave>();

    public DbSet<LearningTopic> LearningTopics => Set<LearningTopic>();

    public DbSet<LearningResource> LearningResources => Set<LearningResource>();

    public DbSet<LearningNote> LearningNotes => Set<LearningNote>();

    public DbSet<Note> Notes => Set<Note>();

    public DbSet<NoteTag> NoteTags => Set<NoteTag>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<IntegrationConnection> IntegrationConnections => Set<IntegrationConnection>();

    public DbSet<ExternalLink> ExternalLinks => Set<ExternalLink>();

    public DbSet<SyncRun> SyncRuns => Set<SyncRun>();

    public DbSet<SyncFailure> SyncFailures => Set<SyncFailure>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("worknexa");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorknexaDbContext).Assembly);
        WorknexaSeed.Apply(modelBuilder);
    }
}

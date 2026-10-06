using Worknexa.Domain.Common;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Learning;
using Worknexa.Domain.Leaves;
using Worknexa.Domain.Meetings;
using Worknexa.Domain.Organization;
using Worknexa.Domain.Productivity;
using Worknexa.Domain.Projects;

namespace Worknexa.Domain.Identity;

public sealed class User : SoftDeletableEntity
{
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Phone { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Designation { get; set; }

    public string? Department { get; set; }

    public DateOnly? JoinedOn { get; set; }

    public Guid? ManagerId { get; set; }

    public User? Manager { get; set; }

    public MemberStatus Status { get; set; } = MemberStatus.Active;

    public string? Notes { get; set; }

    public ICollection<User> DirectReports { get; set; } = new List<User>();

    public ICollection<MemberCompetency> Competencies { get; set; } = new List<MemberCompetency>();

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();

    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();

    public ICollection<WorkTask> AssignedTasks { get; set; } = new List<WorkTask>();

    public ICollection<MeetingParticipant> MeetingParticipations { get; set; } = new List<MeetingParticipant>();

    public ICollection<Leave> Leaves { get; set; } = new List<Leave>();

    public ICollection<LearningTopic> LearningTopics { get; set; } = new List<LearningTopic>();
}

public sealed class Role : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public sealed class Permission : SoftDeletableEntity
{
    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public sealed class UserRole : AuditableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;
}

public sealed class RolePermission : AuditableEntity
{
    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public Guid PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}

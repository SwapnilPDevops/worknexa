using Worknexa.Domain.Common;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Meetings;
using Worknexa.Domain.Productivity;
using Worknexa.Domain.Projects;

namespace Worknexa.Domain.Organization;

public sealed class Team : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();

    public ICollection<Project> Projects { get; set; } = new List<Project>();

    public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();

    public ICollection<Standup> Standups { get; set; } = new List<Standup>();
}

public sealed class TeamMember : AuditableEntity
{
    public Guid TeamId { get; set; }

    public Team Team { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;
}

public sealed class MemberCompetency : AuditableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public MemberCompetencyKind Kind { get; set; }

    public string Name { get; set; } = string.Empty;
}

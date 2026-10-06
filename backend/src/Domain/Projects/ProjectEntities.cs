using Worknexa.Domain.Common;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Organization;
using Worknexa.Domain.Productivity;

namespace Worknexa.Domain.Projects;

public sealed class Project : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid OwnerId { get; set; }

    public User? Owner { get; set; }

    public Guid? TeamId { get; set; }

    public Team? Team { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    public WorkPriority Priority { get; set; } = WorkPriority.Medium;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Technology { get; set; }

    public string? RepositoryUrl { get; set; }

    public string? DocumentationUrl { get; set; }

    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();

    public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
}

public sealed class ProjectMember : AuditableEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Member;
}

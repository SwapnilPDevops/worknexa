using Worknexa.Domain.Common;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;

namespace Worknexa.Domain.Learning;

public sealed class LearningTopic : SoftDeletableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Category { get; set; }

    public LearningStatus Status { get; set; } = LearningStatus.NotStarted;

    public WorkPriority Priority { get; set; } = WorkPriority.Medium;

    public int Progress { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? TargetDate { get; set; }

    public string? Goal { get; set; }

    public Guid? ParentTopicId { get; set; }

    public LearningTopic? Parent { get; set; }

    public ICollection<LearningTopic> Children { get; set; } = new List<LearningTopic>();

    public ICollection<LearningResource> Resources { get; set; } = new List<LearningResource>();

    public ICollection<LearningNote> Notes { get; set; } = new List<LearningNote>();
}

public sealed class LearningResource : SoftDeletableEntity
{
    public Guid LearningTopicId { get; set; }

    public LearningTopic LearningTopic { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Url { get; set; }

    public LearningResourceType ResourceType { get; set; } = LearningResourceType.Other;
}

public sealed class LearningNote : SoftDeletableEntity
{
    public Guid LearningTopicId { get; set; }

    public LearningTopic LearningTopic { get; set; } = null!;

    public string Body { get; set; } = string.Empty;
}

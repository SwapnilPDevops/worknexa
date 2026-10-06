using Worknexa.Domain.Common;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Learning;
using Worknexa.Domain.Meetings;
using Worknexa.Domain.Productivity;
using Worknexa.Domain.Projects;

namespace Worknexa.Domain.Knowledge;

public sealed class Note : SoftDeletableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? Category { get; set; }

    public bool IsFavorite { get; set; }

    public bool IsPinned { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? TaskId { get; set; }

    public WorkTask? Task { get; set; }

    public Guid? MeetingId { get; set; }

    public Meeting? Meeting { get; set; }

    public Guid? LearningTopicId { get; set; }

    public LearningTopic? LearningTopic { get; set; }

    public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();
}

public sealed class NoteTag : AuditableEntity
{
    public Guid NoteId { get; set; }

    public Note Note { get; set; } = null!;

    public Guid TagId { get; set; }

    public Tag Tag { get; set; } = null!;
}

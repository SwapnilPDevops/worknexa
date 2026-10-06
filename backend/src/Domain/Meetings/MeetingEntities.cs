using Worknexa.Domain.Calendar;
using Worknexa.Domain.Common;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Organization;
using Worknexa.Domain.Productivity;
using Worknexa.Domain.Projects;

namespace Worknexa.Domain.Meetings;

public sealed class Meeting : SoftDeletableEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string? Location { get; set; }

    public string? MeetingLink { get; set; }

    public MeetingType MeetingType { get; set; } = MeetingType.Team;

    public string? Agenda { get; set; }

    public string? NotesText { get; set; }

    public string? Decisions { get; set; }

    public string? FollowUps { get; set; }

    public DateTimeOffset? FollowUpAt { get; set; }

    public Guid OrganizerId { get; set; }

    public User Organizer { get; set; } = null!;

    public Guid? TeamId { get; set; }

    public Team? Team { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? CalendarEventId { get; set; }

    public CalendarEvent? CalendarEvent { get; set; }

    public ICollection<MeetingParticipant> Participants { get; set; } = new List<MeetingParticipant>();

    public ICollection<MeetingNote> Notes { get; set; } = new List<MeetingNote>();

    public ICollection<MeetingActionItem> ActionItems { get; set; } = new List<MeetingActionItem>();
}

public sealed class MeetingParticipant : AuditableEntity
{
    public Guid MeetingId { get; set; }

    public Meeting Meeting { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public ParticipantResponse ResponseStatus { get; set; } = ParticipantResponse.Pending;
}

public sealed class MeetingNote : SoftDeletableEntity
{
    public Guid MeetingId { get; set; }

    public Meeting Meeting { get; set; } = null!;

    public string Body { get; set; } = string.Empty;
}

public sealed class MeetingActionItem : SoftDeletableEntity
{
    public Guid MeetingId { get; set; }

    public Meeting Meeting { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? AssigneeId { get; set; }

    public User? Assignee { get; set; }

    public Guid? TaskId { get; set; }

    public WorkTask? Task { get; set; }

    public bool IsCompleted { get; set; }

    public DateTimeOffset? DueAt { get; set; }
}

public sealed class Standup : SoftDeletableEntity
{
    public Guid? TeamId { get; set; }

    public Team? Team { get; set; }

    public Guid? OwnerId { get; set; }

    public User? Owner { get; set; }

    public Guid? MeetingId { get; set; }

    public Meeting? Meeting { get; set; }

    public DateOnly StandupDate { get; set; }

    public ICollection<StandupEntry> Entries { get; set; } = new List<StandupEntry>();
}

public sealed class StandupEntry : SoftDeletableEntity
{
    public Guid StandupId { get; set; }

    public Standup Standup { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string? Yesterday { get; set; }

    public string? Today { get; set; }

    public string? Blockers { get; set; }

    public string? Notes { get; set; }

    public StandupStatus Status { get; set; } = StandupStatus.Clear;
}

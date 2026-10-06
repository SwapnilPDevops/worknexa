using Worknexa.Domain.Common;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Organization;
using Worknexa.Domain.Projects;

namespace Worknexa.Domain.Calendar;

public sealed class CalendarEvent : SoftDeletableEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public bool IsAllDay { get; set; }

    public string? Location { get; set; }

    public string? MeetingLink { get; set; }

    public CalendarItemKind Kind { get; set; } = CalendarItemKind.Personal;

    public Recurrence Recurrence { get; set; } = Recurrence.None;

    public int? RecurrenceInterval { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid OrganizerId { get; set; }

    public User Organizer { get; set; } = null!;

    public Guid? TeamId { get; set; }

    public Team? Team { get; set; }

    public ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
}

public sealed class EventParticipant : AuditableEntity
{
    public Guid CalendarEventId { get; set; }

    public CalendarEvent CalendarEvent { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public ParticipantResponse ResponseStatus { get; set; } = ParticipantResponse.Pending;
}

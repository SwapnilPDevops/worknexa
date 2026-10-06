using Worknexa.Domain.Enums;

namespace Worknexa.Application.Integrations.Calendar;

/// <summary>The fields both sides share. Only these take part in change detection.</summary>
public sealed record CalendarContent(
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    bool IsAllDay,
    string? Location,
    string? MeetingLink);

public sealed record LocalCalendarItem(Guid Id, CalendarContent Content, bool Recurring, DateTimeOffset UpdatedAt);

public sealed record SyncLink(Guid Id, Guid LocalId, string ExternalId, string? ExternalVersion, string SyncedHash, LinkOrigin Origin);

public interface ICalendarSyncStore
{
    Task<IReadOnlyList<SyncLink>> LinksAsync(Guid connectionId, CancellationToken cancellationToken);

    /// <summary>Live (not deleted) events by id, regardless of date.</summary>
    Task<IReadOnlyDictionary<Guid, LocalCalendarItem>> FindAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Events this person organizes in the window that may be sent out: those linked to this connection,
    /// and those created in Worknexa. Events imported through other connections are excluded.
    /// </summary>
    Task<IReadOnlyList<LocalCalendarItem>> ExportCandidatesAsync(Guid userId, Guid connectionId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    /// <summary>Creates the local event and its link in one transaction.</summary>
    Task<SyncLink> ImportAsync(Guid connectionId, Guid userId, CalendarContent content, string externalId, string? version, string hash, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Updates the local event and its link in one transaction.</summary>
    Task ApplyAsync(Guid linkId, Guid localId, CalendarContent content, string? version, string hash, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Deletes the local event (soft) and its link.</summary>
    Task RemoveLocalAsync(Guid linkId, Guid localId, DateTimeOffset now, CancellationToken cancellationToken);

    Task SaveLinkAsync(Guid connectionId, Guid localId, string externalId, string? version, string hash, LinkOrigin origin, DateTimeOffset now, CancellationToken cancellationToken);

    Task UpdateLinkAsync(Guid linkId, string externalId, string? version, string hash, DateTimeOffset now, CancellationToken cancellationToken);

    Task DeleteLinkAsync(Guid linkId, CancellationToken cancellationToken);

    /// <summary>Removes events imported through the connection and forgets every link. Returns how many events were removed.</summary>
    Task<int> RemoveImportedAsync(Guid connectionId, DateTimeOffset now, CancellationToken cancellationToken);
}

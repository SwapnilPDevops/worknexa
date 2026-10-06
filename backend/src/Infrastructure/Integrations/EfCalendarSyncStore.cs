using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Integrations.Calendar;
using Worknexa.Domain.Calendar;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Integrations;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Integrations;

public sealed class EfCalendarSyncStore(WorknexaDbContext context) : ICalendarSyncStore
{
    public const string EntityType = "CalendarEvent";

    private const int CandidateLimit = 500;

    public async Task<IReadOnlyList<SyncLink>> LinksAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        return await context.ExternalLinks.AsNoTracking()
            .Where(link => link.ConnectionId == connectionId && link.EntityType == EntityType)
            .Select(link => new SyncLink(link.Id, link.LocalId, link.ExternalId, link.ExternalVersion, link.SyncedHash, link.Origin))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, LocalCalendarItem>> FindAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, LocalCalendarItem>();
        }

        var items = await Items(context.CalendarEvents.AsNoTracking().Where(item => ids.Contains(item.Id))).ToListAsync(cancellationToken);
        return items.ToDictionary(item => item.Id);
    }

    public async Task<IReadOnlyList<LocalCalendarItem>> ExportCandidatesAsync(Guid userId, Guid connectionId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var importedElsewhere = context.ExternalLinks
            .Where(link => link.EntityType == EntityType && link.Origin == LinkOrigin.External && link.ConnectionId != connectionId)
            .Select(link => link.LocalId);
        var query = context.CalendarEvents.AsNoTracking()
            .Where(item =>
                item.OrganizerId == userId
                && item.StartAt < to
                && (item.Recurrence != Recurrence.None || item.EndAt > from)
                && !importedElsewhere.Contains(item.Id))
            .OrderBy(item => item.StartAt)
            .Take(CandidateLimit);
        return await Items(query).ToListAsync(cancellationToken);
    }

    public async Task<SyncLink> ImportAsync(Guid connectionId, Guid userId, CalendarContent content, string externalId, string? version, string hash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        var calendarEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            OrganizerId = userId,
            Kind = content.MeetingLink is null ? CalendarItemKind.Personal : CalendarItemKind.Meeting,
            Recurrence = Recurrence.None
        };
        Apply(calendarEvent, content);
        var link = new ExternalLink
        {
            Id = Guid.NewGuid(),
            ConnectionId = connectionId,
            EntityType = EntityType,
            LocalId = calendarEvent.Id,
            ExternalId = externalId,
            ExternalVersion = version,
            SyncedHash = hash,
            Origin = LinkOrigin.External,
            LastSyncedAt = now
        };
        context.CalendarEvents.Add(calendarEvent);
        context.ExternalLinks.Add(link);
        await context.SaveChangesAsync(cancellationToken);
        return new SyncLink(link.Id, link.LocalId, link.ExternalId, link.ExternalVersion, link.SyncedHash, link.Origin);
    }

    public async Task ApplyAsync(Guid linkId, Guid localId, CalendarContent content, string? version, string hash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        var calendarEvent = await context.CalendarEvents.FirstAsync(item => item.Id == localId, cancellationToken);
        var link = await context.ExternalLinks.FirstAsync(item => item.Id == linkId, cancellationToken);
        Apply(calendarEvent, content);
        link.ExternalVersion = version;
        link.SyncedHash = hash;
        link.LastSyncedAt = now;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveLocalAsync(Guid linkId, Guid localId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        var calendarEvent = await context.CalendarEvents.FirstOrDefaultAsync(item => item.Id == localId, cancellationToken);
        if (calendarEvent is not null)
        {
            calendarEvent.DeletedAt = now;
            foreach (var reminder in await context.Reminders.Where(item => item.CalendarEventId == localId).ToListAsync(cancellationToken))
            {
                reminder.DeletedAt = now;
            }
        }

        var link = await context.ExternalLinks.FirstOrDefaultAsync(item => item.Id == linkId, cancellationToken);
        if (link is not null)
        {
            context.ExternalLinks.Remove(link);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveLinkAsync(Guid connectionId, Guid localId, string externalId, string? version, string hash, LinkOrigin origin, DateTimeOffset now, CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        var link = await context.ExternalLinks.FirstOrDefaultAsync(item => item.ConnectionId == connectionId && item.EntityType == EntityType && item.LocalId == localId, cancellationToken);
        if (link is null)
        {
            link = new ExternalLink { Id = Guid.NewGuid(), ConnectionId = connectionId, EntityType = EntityType, LocalId = localId, Origin = origin };
            context.ExternalLinks.Add(link);
        }

        link.ExternalId = externalId;
        link.ExternalVersion = version;
        link.SyncedHash = hash;
        link.LastSyncedAt = now;
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateLinkAsync(Guid linkId, string externalId, string? version, string hash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return context.ExternalLinks.Where(link => link.Id == linkId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(link => link.ExternalId, externalId)
                .SetProperty(link => link.ExternalVersion, version)
                .SetProperty(link => link.SyncedHash, hash)
                .SetProperty(link => link.LastSyncedAt, now), cancellationToken);
    }

    public Task DeleteLinkAsync(Guid linkId, CancellationToken cancellationToken)
    {
        return context.ExternalLinks.Where(link => link.Id == linkId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> RemoveImportedAsync(Guid connectionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var imported = context.ExternalLinks
            .Where(link => link.ConnectionId == connectionId && link.EntityType == EntityType && link.Origin == LinkOrigin.External)
            .Select(link => link.LocalId);
        var removed = await context.CalendarEvents
            .Where(item => imported.Contains(item.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.DeletedAt, now), cancellationToken);
        await context.ExternalLinks.Where(link => link.ConnectionId == connectionId).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return removed;
    }

    private static void Apply(CalendarEvent calendarEvent, CalendarContent content)
    {
        calendarEvent.Title = content.Title;
        calendarEvent.Description = content.Description;
        calendarEvent.StartAt = content.StartAt;
        calendarEvent.EndAt = content.EndAt;
        calendarEvent.IsAllDay = content.IsAllDay;
        calendarEvent.Location = content.Location;
        calendarEvent.MeetingLink = content.MeetingLink ?? calendarEvent.MeetingLink;
    }

    private static IQueryable<LocalCalendarItem> Items(IQueryable<CalendarEvent> events)
    {
        return events.Select(item => new LocalCalendarItem(
            item.Id,
            new CalendarContent(item.Title, item.Description, item.StartAt, item.EndAt, item.IsAllDay, item.Location, item.MeetingLink),
            item.Recurrence != Recurrence.None,
            item.UpdatedAt));
    }
}

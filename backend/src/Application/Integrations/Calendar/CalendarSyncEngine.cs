using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Worknexa.Domain.Enums;

namespace Worknexa.Application.Integrations.Calendar;

public sealed record CalendarSyncContext(
    ConnectionState Connection,
    string CalendarId,
    ICalendarProvider Provider,
    IntegrationSession Session,
    DateTimeOffset Now,
    Func<string?, DateTimeOffset?, Task> SaveCursor);

/// <summary>
/// Synchronizes one calendar connection. Every linked event is owned by the side it came from:
/// <list type="bullet">
/// <item>Import only: external events flow in and stay read-only here. Nothing goes out.</item>
/// <item>Export only: Worknexa events flow out. External edits are ignored.</item>
/// <item>Two-way: both flow. When both sides changed since the last sync, the newer edit wins and a conflict is counted.</item>
/// </list>
/// Links (unique per external id and per local id), a content hash, and idempotent creates prevent duplicates.
/// </summary>
public sealed class CalendarSyncEngine(ICalendarSyncStore store, IOptions<IntegrationSettings> options)
{
    private const int TitleLimit = 300;
    private const int DescriptionLimit = 8000;
    private const int LocationLimit = 300;
    private const int LinkLimit = 500;

    private readonly IntegrationSettings _settings = options.Value;

    public async Task RunAsync(CalendarSyncContext context, SyncTally tally, CancellationToken cancellationToken)
    {
        var zone = Zones.Resolve(context.Connection.TimeZone);
        var from = context.Now.AddDays(-_settings.PastDays);
        var to = context.Now.AddDays(_settings.FutureDays);

        if (context.Connection.Direction != SyncDirection.ExportOnly)
        {
            await ImportAsync(context, zone, from, to, tally, cancellationToken);
        }

        if (context.Connection.Direction != SyncDirection.ImportOnly)
        {
            await ExportAsync(context, zone, from, to, tally, cancellationToken);
        }
    }

    public static string Hash(CalendarContent content)
    {
        var text = string.Join('\u001f',
            content.Title,
            content.Description ?? "",
            content.StartAt.UtcTicks.ToString(CultureInfo.InvariantCulture),
            content.EndAt.UtcTicks.ToString(CultureInfo.InvariantCulture),
            content.IsAllDay ? "1" : "0",
            content.Location ?? "");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..32];
    }

    public static CalendarContent ToLocal(ExternalCalendarEvent item, TimeZoneInfo zone)
    {
        var start = item.Start.ToUniversalTime();
        var end = item.End.ToUniversalTime();
        if (item.IsAllDay)
        {
            var firstDay = DateOnly.FromDateTime(item.Start.UtcDateTime);
            var lastDay = DateOnly.FromDateTime(item.End.UtcDateTime);
            if (lastDay <= firstDay)
            {
                lastDay = firstDay.AddDays(1);
            }

            start = Zones.Midnight(firstDay, zone);
            end = Zones.Midnight(lastDay, zone);
        }

        if (end < start)
        {
            end = start;
        }

        var title = Clean(item.Title, TitleLimit) ?? "(No title)";
        return new CalendarContent(title, Clean(item.Description, DescriptionLimit), start, end, item.IsAllDay, Clean(item.Location, LocationLimit), Clean(item.MeetingLink, LinkLimit));
    }

    public static CalendarEventWriteModel ToExternal(LocalCalendarItem item, TimeZoneInfo zone)
    {
        var content = item.Content;
        if (!content.IsAllDay)
        {
            return new CalendarEventWriteModel(content.Title, content.Description, content.StartAt.ToUniversalTime(), content.EndAt.ToUniversalTime(), false, content.Location, null, item.Id.ToString("N"));
        }

        var firstDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(content.StartAt, zone).DateTime);
        var lastDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(content.EndAt, zone).DateTime);
        if (lastDay <= firstDay)
        {
            lastDay = firstDay.AddDays(1);
        }

        return new CalendarEventWriteModel(
            content.Title,
            content.Description,
            new DateTimeOffset(firstDay.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            new DateTimeOffset(lastDay.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            true,
            content.Location,
            zone.Id,
            item.Id.ToString("N"));
    }

    private async Task ImportAsync(CalendarSyncContext context, TimeZoneInfo zone, DateTimeOffset from, DateTimeOffset to, SyncTally tally, CancellationToken cancellationToken)
    {
        var connection = context.Connection;
        var links = (await store.LinksAsync(connection.Id, cancellationToken)).ToDictionary(link => link.ExternalId, StringComparer.Ordinal);
        var locals = await store.FindAsync(links.Values.Select(link => link.LocalId).ToArray(), cancellationToken);

        var maxAge = TimeSpan.FromDays(Math.Max(1, _settings.CursorMaxAgeDays));
        var cursor = connection.CursorIssuedAt is { } issued && context.Now - issued < maxAge ? connection.SyncCursor : null;
        var issuedAt = cursor is null ? context.Now : connection.CursorIssuedAt;
        var restarted = false;
        var failed = false;

        while (true)
        {
            CalendarChangePage page;
            try
            {
                page = await context.Provider.ReadChangesAsync(context.Session, context.CalendarId, cursor, from, to, cancellationToken);
            }
            catch (IntegrationException exception) when (exception.Failure == IntegrationFailure.CursorExpired && !restarted)
            {
                restarted = true;
                cursor = null;
                issuedAt = context.Now;
                continue;
            }

            foreach (var change in page.Changes)
            {
                try
                {
                    await ImportOneAsync(context, zone, change, links, locals, tally, cancellationToken);
                }
                catch (Exception exception) when (IsItemFailure(exception))
                {
                    failed = true;
                    tally.Fail("Import", null, change.Id, change.Title, Describe(exception));
                }
            }

            cursor = page.Cursor;
            if (!page.HasMore)
            {
                break;
            }
        }

        // Keep the old cursor after item failures so those changes are read again; links make that safe.
        if (!failed)
        {
            await context.SaveCursor(cursor, issuedAt);
        }
    }

    private async Task ImportOneAsync(
        CalendarSyncContext context,
        TimeZoneInfo zone,
        ExternalCalendarEvent change,
        Dictionary<string, SyncLink> links,
        IReadOnlyDictionary<Guid, LocalCalendarItem> locals,
        SyncTally tally,
        CancellationToken cancellationToken)
    {
        var connection = context.Connection;
        links.TryGetValue(change.Id, out var link);
        if (link is not null && link.Origin == LinkOrigin.Local && connection.Direction != SyncDirection.TwoWay)
        {
            return;
        }

        if (change.Removed)
        {
            if (link is null)
            {
                return;
            }

            await store.RemoveLocalAsync(link.Id, link.LocalId, context.Now, cancellationToken);
            links.Remove(change.Id);
            tally.Removed++;
            return;
        }

        var content = ToLocal(change, zone);
        var hash = Hash(content);
        if (link is null)
        {
            links[change.Id] = await store.ImportAsync(connection.Id, connection.UserId, content, change.Id, change.Version, hash, context.Now, cancellationToken);
            tally.Imported++;
            return;
        }

        if (hash == link.SyncedHash)
        {
            if (change.Version != link.ExternalVersion)
            {
                await store.UpdateLinkAsync(link.Id, link.ExternalId, change.Version, hash, context.Now, cancellationToken);
            }

            return;
        }

        if (!locals.TryGetValue(link.LocalId, out var local))
        {
            if (connection.Direction == SyncDirection.TwoWay)
            {
                // Deleted here, edited there: the deletion wins and the export step removes the external copy.
                tally.Conflicts++;
                return;
            }

            await store.DeleteLinkAsync(link.Id, cancellationToken);
            links[change.Id] = await store.ImportAsync(connection.Id, connection.UserId, content, change.Id, change.Version, hash, context.Now, cancellationToken);
            tally.Imported++;
            return;
        }

        if (connection.Direction == SyncDirection.TwoWay && Hash(local.Content) != link.SyncedHash)
        {
            tally.Conflicts++;
            if (local.UpdatedAt > (change.UpdatedAt ?? DateTimeOffset.MinValue))
            {
                return;
            }
        }

        await store.ApplyAsync(link.Id, link.LocalId, content, change.Version, hash, context.Now, cancellationToken);
        links[change.Id] = link with { ExternalVersion = change.Version, SyncedHash = hash };
        tally.Updated++;
    }

    private async Task ExportAsync(CalendarSyncContext context, TimeZoneInfo zone, DateTimeOffset from, DateTimeOffset to, SyncTally tally, CancellationToken cancellationToken)
    {
        var connection = context.Connection;
        var links = await store.LinksAsync(connection.Id, cancellationToken);
        var byLocal = links.ToDictionary(link => link.LocalId);

        foreach (var local in await store.ExportCandidatesAsync(connection.UserId, connection.Id, from, to, cancellationToken))
        {
            byLocal.TryGetValue(local.Id, out var link);
            if (link is not null && link.Origin == LinkOrigin.External && connection.Direction != SyncDirection.TwoWay)
            {
                continue;
            }

            if (local.Recurring)
            {
                tally.Skipped++;
                continue;
            }

            var hash = Hash(local.Content);
            if (link is not null && hash == link.SyncedHash)
            {
                continue;
            }

            try
            {
                if (link is null)
                {
                    var created = await context.Provider.CreateEventAsync(context.Session, context.CalendarId, ToExternal(local, zone), cancellationToken);
                    await store.SaveLinkAsync(connection.Id, local.Id, created.Id, created.Version, hash, LinkOrigin.Local, context.Now, cancellationToken);
                    tally.Exported++;
                    continue;
                }

                try
                {
                    var updated = await context.Provider.UpdateEventAsync(context.Session, context.CalendarId, link.ExternalId, ToExternal(local, zone), cancellationToken);
                    await store.UpdateLinkAsync(link.Id, updated.Id, updated.Version, hash, context.Now, cancellationToken);
                    tally.Updated++;
                }
                catch (IntegrationException exception) when (exception.Failure == IntegrationFailure.NotFound)
                {
                    if (link.Origin == LinkOrigin.External)
                    {
                        await store.RemoveLocalAsync(link.Id, link.LocalId, context.Now, cancellationToken);
                        tally.Removed++;
                        continue;
                    }

                    var created = await context.Provider.CreateEventAsync(context.Session, context.CalendarId, ToExternal(local, zone), cancellationToken);
                    await store.UpdateLinkAsync(link.Id, created.Id, created.Version, hash, context.Now, cancellationToken);
                    tally.Exported++;
                }
            }
            catch (Exception exception) when (IsItemFailure(exception))
            {
                tally.Fail("Export", local.Id, link?.ExternalId, local.Content.Title, Describe(exception));
            }
        }

        var live = await store.FindAsync(links.Select(link => link.LocalId).ToArray(), cancellationToken);
        foreach (var link in links.Where(link => !live.ContainsKey(link.LocalId)))
        {
            try
            {
                if (link.Origin == LinkOrigin.Local || connection.Direction == SyncDirection.TwoWay)
                {
                    await context.Provider.DeleteEventAsync(context.Session, context.CalendarId, link.ExternalId, cancellationToken);
                    tally.Removed++;
                }

                await store.DeleteLinkAsync(link.Id, cancellationToken);
            }
            catch (Exception exception) when (IsItemFailure(exception))
            {
                tally.Fail("Delete", link.LocalId, link.ExternalId, null, Describe(exception));
            }
        }
    }

    /// <summary>Problems with one item are logged and skipped. Auth and transient errors stop the whole run.</summary>
    private static bool IsItemFailure(Exception exception)
    {
        return exception is not OperationCanceledException
            && exception is not IntegrationException { Failure: IntegrationFailure.Auth or IntegrationFailure.Transient };
    }

    private static string Describe(Exception exception)
    {
        var message = exception is IntegrationException or Common.ConflictException or FluentValidation.ValidationException
            ? exception.Message
            : "Unexpected error while syncing this item.";
        return message.Length <= 1000 ? message : message[..1000];
    }

    private static string? Clean(string? value, int limit)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        return text.Length <= limit ? text : text[..limit];
    }
}

public static class Zones
{
    public static TimeZoneInfo Resolve(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
        {
            return zone;
        }

        return TimeZoneInfo.Utc;
    }

    public static DateTimeOffset Midnight(DateOnly day, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
    }
}

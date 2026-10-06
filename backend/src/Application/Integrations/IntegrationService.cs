using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Common;
using Worknexa.Application.Integrations.Calendar;
using Worknexa.Domain.Enums;

namespace Worknexa.Application.Integrations;

public sealed record IntegrationProviderItem(
    string Key,
    string Name,
    string Vendor,
    IReadOnlyList<string> Categories,
    bool Available,
    bool Configured,
    bool CanManage);

public sealed record IntegrationConnectionItem(
    Guid Id,
    string Provider,
    string Category,
    string Status,
    string Direction,
    bool SyncEnabled,
    string? AccountName,
    string? CalendarId,
    string? CalendarName,
    string? TimeZone,
    DateTimeOffset? LastSyncedAt,
    DateTimeOffset? NextSyncAt,
    int ConsecutiveFailures,
    string? LastError,
    SyncRunItem? LastRun);

public sealed record IntegrationOverview(IReadOnlyList<IntegrationProviderItem> Providers, IReadOnlyList<IntegrationConnectionItem> Connections);

public sealed record ConnectRequest(string? TimeZone);

public sealed record ConnectStart(string AuthorizationUrl);

public sealed record ConnectionSettingsWrite(string Direction, bool SyncEnabled, string? CalendarId);

public sealed record DisconnectResult(int RemovedEvents);

/// <summary>Raised while finishing an OAuth sign-in; the reason is shown to the person after the redirect.</summary>
public sealed class IntegrationConnectException(string reason) : Exception(reason)
{
    public string Reason { get; } = reason;
}

public interface IIntegrationService
{
    Task<IntegrationOverview> OverviewAsync(Guid userId, IReadOnlyList<string> permissions, CancellationToken cancellationToken);

    Task<ConnectStart> StartConnectAsync(Guid userId, IReadOnlyList<string> permissions, string provider, ConnectRequest request, DateTimeOffset now, CancellationToken cancellationToken);

    Task<string> CompleteConnectAsync(Guid? userId, string? code, string? state, string? error, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<ExternalCalendar>> CalendarsAsync(Guid userId, Guid connectionId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IntegrationConnectionItem> UpdateAsync(Guid userId, IReadOnlyList<string> permissions, Guid connectionId, ConnectionSettingsWrite request, DateTimeOffset now, CancellationToken cancellationToken);

    Task<SyncRunItem> SyncAsync(Guid userId, IReadOnlyList<string> permissions, Guid connectionId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<SyncRunItem>> RunsAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken);

    Task<DisconnectResult> DisconnectAsync(Guid userId, IReadOnlyList<string> permissions, Guid connectionId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> DueAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken);

    Task RunScheduledAsync(Guid connectionId, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class IntegrationService(
    IntegrationProviders providers,
    IIntegrationStore store,
    ICalendarSyncStore calendarStore,
    CalendarSyncEngine calendarEngine,
    ISecretProtector protector,
    IOptions<IntegrationSettings> options,
    ILogger<IntegrationService> logger) : IIntegrationService
{
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LockLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(2);
    private const int ErrorAfterFailures = 5;
    private const string ReconnectMessage = "Access was revoked or expired. Reconnect to resume syncing.";

    private readonly IntegrationSettings _settings = options.Value;

    public async Task<IntegrationOverview> OverviewAsync(Guid userId, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        var items = IntegrationCatalog.All.Select(descriptor =>
        {
            var provider = providers.Find(descriptor.Key);
            return new IntegrationProviderItem(
                descriptor.Key,
                descriptor.Name,
                descriptor.Vendor,
                descriptor.Categories.Select(category => category.ToString()).ToArray(),
                provider is not null,
                provider?.IsConfigured ?? false,
                descriptor.Categories.All(category => permissions.Contains(IntegrationCategories.ManagePermission(category))));
        }).ToArray();

        var connections = new List<IntegrationConnectionItem>();
        foreach (var connection in await store.ListAsync(userId, cancellationToken))
        {
            connections.Add(await ItemAsync(connection, cancellationToken));
        }

        return new IntegrationOverview(items, connections);
    }

    public Task<ConnectStart> StartConnectAsync(Guid userId, IReadOnlyList<string> permissions, string provider, ConnectRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var oauth = providers.Find<IOAuthIntegration>(provider) ?? throw new NotFoundException("That integration is not available yet.");
        Require(permissions, oauth.Category);
        if (!oauth.IsConfigured)
        {
            throw new ConflictException("This integration needs app credentials on the server before anyone can connect.");
        }

        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var state = new OAuthState(userId, provider, verifier, ValidZone(request.TimeZone), now.Add(StateLifetime).ToUnixTimeSeconds());
        var token = protector.Protect(JsonSerializer.Serialize(state));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return Task.FromResult(new ConnectStart(oauth.AuthorizationUrl(token, challenge)));
    }

    public async Task<string> CompleteConnectAsync(Guid? userId, string? code, string? state, string? error, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(error))
        {
            throw new IntegrationConnectException("denied");
        }

        if (userId is null)
        {
            throw new IntegrationConnectException("signin");
        }

        var payload = ReadState(state);
        if (payload is null || payload.UserId != userId || payload.ExpiresAt < now.ToUnixTimeSeconds() || string.IsNullOrEmpty(code))
        {
            throw new IntegrationConnectException("expired");
        }

        var oauth = providers.Find<IOAuthIntegration>(payload.Provider);
        if (oauth is not { IsConfigured: true })
        {
            throw new IntegrationConnectException("unavailable");
        }

        OAuthGrant grant;
        try
        {
            grant = await oauth.ExchangeCodeAsync(code, payload.Verifier, cancellationToken);
        }
        catch (IntegrationException exception)
        {
            logger.LogWarning(exception, "Connecting {Provider} failed", payload.Provider);
            throw new IntegrationConnectException("failed");
        }

        var existing = await store.FindByProviderAsync(userId.Value, payload.Provider, cancellationToken);
        if (existing is not null && existing.AccountId != grant.AccountId)
        {
            await calendarStore.RemoveImportedAsync(existing.Id, now, cancellationToken);
            await store.UpdateSettingsAsync(existing.Id, existing.Direction, existing.SyncEnabled, null, null, resetCursor: true, cancellationToken);
        }

        await store.UpsertAsync(userId.Value, new ConnectionDraft(
            payload.Provider,
            oauth.Category,
            grant.AccountId,
            grant.AccountName,
            payload.TimeZone,
            protector.Protect(grant.AccessToken),
            grant.RefreshToken is null ? null : protector.Protect(grant.RefreshToken),
            grant.ExpiresAt,
            now), cancellationToken);
        logger.LogInformation("User {UserId} connected {Provider}", userId, payload.Provider);
        return payload.Provider;
    }

    public async Task<IReadOnlyList<ExternalCalendar>> CalendarsAsync(Guid userId, Guid connectionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connection = await FindAsync(userId, connectionId, cancellationToken);
        var provider = Calendar(connection);
        try
        {
            var session = await SessionAsync(connection, provider, now, false, cancellationToken);
            return await provider.ListCalendarsAsync(session, cancellationToken);
        }
        catch (IntegrationException exception) when (exception.Failure == IntegrationFailure.Auth)
        {
            await store.MarkReconnectAsync(connection.Id, ReconnectMessage, CancellationToken.None);
            throw;
        }
    }

    public async Task<IntegrationConnectionItem> UpdateAsync(Guid userId, IReadOnlyList<string> permissions, Guid connectionId, ConnectionSettingsWrite request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connection = await FindAsync(userId, connectionId, cancellationToken);
        Require(permissions, connection.Category);
        if (!Enum.TryParse<SyncDirection>(request.Direction, ignoreCase: false, out var direction) || !Enum.IsDefined(direction))
        {
            throw new ValidationException([new ValidationFailure("direction", "Choose ImportOnly, ExportOnly, or TwoWay.")]);
        }

        var containerId = connection.ContainerId;
        var containerName = connection.ContainerName;
        var reset = false;
        if (!string.IsNullOrWhiteSpace(request.CalendarId) && request.CalendarId != connection.ContainerId)
        {
            var calendars = await CalendarsAsync(userId, connectionId, now, cancellationToken);
            var chosen = calendars.FirstOrDefault(calendar => calendar.Id == request.CalendarId)
                ?? throw new ValidationException([new ValidationFailure("calendarId", "Choose one of your calendars.")]);
            if (direction != SyncDirection.ImportOnly && !chosen.CanEdit)
            {
                throw new ValidationException([new ValidationFailure("calendarId", "That calendar is read-only. Choose import only, or another calendar.")]);
            }

            // Events from the previous calendar are not in the new one, so their copies and links go.
            await calendarStore.RemoveImportedAsync(connection.Id, now, cancellationToken);
            containerId = chosen.Id;
            containerName = chosen.Name;
            reset = true;
        }

        await store.UpdateSettingsAsync(connection.Id, direction, request.SyncEnabled, containerId, containerName, reset, cancellationToken);
        return await ItemAsync((await store.FindByIdAsync(connection.Id, cancellationToken))!, cancellationToken);
    }

    public async Task<SyncRunItem> SyncAsync(Guid userId, IReadOnlyList<string> permissions, Guid connectionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connection = await FindAsync(userId, connectionId, cancellationToken);
        Require(permissions, connection.Category);
        var runId = await RunAsync(connection, SyncTrigger.Manual, now, cancellationToken);
        return (await store.RunsAsync(connection.Id, 1, cancellationToken)).First(run => run.Id == runId);
    }

    public async Task<IReadOnlyList<SyncRunItem>> RunsAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken)
    {
        var connection = await FindAsync(userId, connectionId, cancellationToken);
        return await store.RunsAsync(connection.Id, 20, cancellationToken);
    }

    public async Task<DisconnectResult> DisconnectAsync(Guid userId, IReadOnlyList<string> permissions, Guid connectionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connection = await FindAsync(userId, connectionId, cancellationToken);
        Require(permissions, connection.Category);
        if (!await store.TryLockAsync(connection.Id, now, now.Add(LockLifetime), cancellationToken))
        {
            throw new ConflictException("A sync is running. Try again in a moment.");
        }

        var removed = await calendarStore.RemoveImportedAsync(connection.Id, now, cancellationToken);
        await store.DeleteAsync(connection.Id, cancellationToken);
        logger.LogInformation("User {UserId} disconnected {Provider}; {Removed} imported events removed", userId, connection.Provider, removed);
        return new DisconnectResult(removed);
    }

    public Task<IReadOnlyList<Guid>> DueAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken)
    {
        var calendarProviders = providers.Keys.Where(key => providers.Find<ICalendarProvider>(key) is { IsConfigured: true }).ToArray();
        return calendarProviders.Length == 0
            ? Task.FromResult<IReadOnlyList<Guid>>([])
            : store.DueAsync(calendarProviders, now, limit, cancellationToken);
    }

    public async Task RunScheduledAsync(Guid connectionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connection = await store.FindByIdAsync(connectionId, cancellationToken);
        if (connection is { SyncEnabled: true, Status: not IntegrationStatus.NeedsReconnect })
        {
            await RunAsync(connection, SyncTrigger.Scheduled, now, cancellationToken);
        }
    }

    private async Task<Guid> RunAsync(ConnectionState connection, SyncTrigger trigger, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var provider = Calendar(connection);
        if (!await store.TryLockAsync(connection.Id, now, now.Add(LockLifetime), cancellationToken))
        {
            throw new ConflictException("A sync is already running for this connection.");
        }

        var runId = await store.StartRunAsync(connection.Id, trigger, connection.Direction, now, CancellationToken.None);
        var tally = new SyncTally();
        var status = SyncRunStatus.Failed;
        string? error = null;
        var failures = connection.ConsecutiveFailures + 1;
        var outcome = new ConnectionOutcome(connection.Status, failures, null, connection.LastSyncedAt, now.Add(Backoff(failures, null)));
        try
        {
            await SyncCalendarAsync(connection, provider, now, tally, cancellationToken);
            status = tally.Failed > 0 ? SyncRunStatus.PartiallySucceeded : SyncRunStatus.Succeeded;
            var note = tally.Failed > 0 ? $"{tally.Failed} item(s) could not be synced. See the sync log." : null;
            outcome = new ConnectionOutcome(IntegrationStatus.Connected, 0, note, now, now.AddMinutes(Math.Max(1, _settings.IntervalMinutes)));
        }
        catch (IntegrationException exception) when (exception.Failure == IntegrationFailure.Auth)
        {
            error = ReconnectMessage;
            outcome = new ConnectionOutcome(IntegrationStatus.NeedsReconnect, failures, error, connection.LastSyncedAt, null);
        }
        catch (IntegrationException exception) when (exception.Failure == IntegrationFailure.Transient)
        {
            error = $"{exception.Message} Worknexa will retry automatically.";
            outcome = new ConnectionOutcome(connection.Status == IntegrationStatus.Error ? IntegrationStatus.Error : IntegrationStatus.Connected, failures, error, connection.LastSyncedAt, now.Add(Backoff(failures, exception.RetryAfter)));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Sync failed for connection {ConnectionId}", connection.Id);
            error = exception is IntegrationException or ConflictException ? exception.Message : "The sync stopped because of an unexpected error.";
            var state = failures >= ErrorAfterFailures ? IntegrationStatus.Error : IntegrationStatus.Connected;
            outcome = new ConnectionOutcome(state, failures, error, connection.LastSyncedAt, now.Add(Backoff(failures, null)));
        }
        finally
        {
            await store.FinishRunAsync(runId, status, tally, error, DateTimeOffset.UtcNow, CancellationToken.None);
            await store.FinishAsync(connection.Id, outcome, CancellationToken.None);
        }

        return runId;
    }

    private async Task SyncCalendarAsync(ConnectionState connection, ICalendarProvider provider, DateTimeOffset now, SyncTally tally, CancellationToken cancellationToken)
    {
        var session = await SessionAsync(connection, provider, now, false, cancellationToken);
        var calendarId = connection.ContainerId;
        if (string.IsNullOrEmpty(calendarId))
        {
            var calendars = await provider.ListCalendarsAsync(session, cancellationToken);
            var chosen = calendars.FirstOrDefault(calendar => calendar.IsDefault) ?? calendars.FirstOrDefault()
                ?? throw new IntegrationException(IntegrationFailure.Rejected, "The account has no calendars.");
            calendarId = chosen.Id;
            await store.UpdateSettingsAsync(connection.Id, connection.Direction, connection.SyncEnabled, chosen.Id, chosen.Name, resetCursor: true, cancellationToken);
            connection = connection with { ContainerId = chosen.Id, ContainerName = chosen.Name, SyncCursor = null, CursorIssuedAt = null };
        }

        CalendarSyncContext Context(IntegrationSession current) => new(
            connection,
            calendarId,
            provider,
            current,
            now,
            (cursor, issuedAt) => store.SaveCursorAsync(connection.Id, cursor, issuedAt, cancellationToken));

        try
        {
            await calendarEngine.RunAsync(Context(session), tally, cancellationToken);
        }
        catch (IntegrationException exception) when (exception.Failure == IntegrationFailure.Auth)
        {
            // The token may have been revoked early; refresh once, then run again. Links make the rerun safe.
            var refreshed = await SessionAsync(connection, provider, now, true, cancellationToken);
            await calendarEngine.RunAsync(Context(refreshed), tally, cancellationToken);
        }
    }

    private async Task<IntegrationSession> SessionAsync(ConnectionState connection, IIntegrationProvider provider, DateTimeOffset now, bool force, CancellationToken cancellationToken)
    {
        if (provider is not IOAuthIntegration oauth)
        {
            throw new IntegrationException(IntegrationFailure.Rejected, "This provider does not support sign-in.");
        }

        var access = connection.AccessTokenProtected is null ? null : protector.Unprotect(connection.AccessTokenProtected);
        if (!force && access is not null && connection.TokenExpiresAt > now.Add(RefreshMargin))
        {
            return new IntegrationSession(connection.Id, access);
        }

        var refresh = connection.RefreshTokenProtected is null ? null : protector.Unprotect(connection.RefreshTokenProtected);
        if (refresh is null)
        {
            throw new IntegrationException(IntegrationFailure.Auth, "Sign in again to continue.");
        }

        var grant = await oauth.RefreshAsync(refresh, cancellationToken);
        await store.SaveTokensAsync(
            connection.Id,
            protector.Protect(grant.AccessToken),
            grant.RefreshToken is null ? connection.RefreshTokenProtected : protector.Protect(grant.RefreshToken),
            grant.ExpiresAt,
            cancellationToken);
        return new IntegrationSession(connection.Id, grant.AccessToken);
    }

    private async Task<IntegrationConnectionItem> ItemAsync(ConnectionState connection, CancellationToken cancellationToken)
    {
        var lastRun = (await store.RunsAsync(connection.Id, 1, cancellationToken)).FirstOrDefault();
        return new IntegrationConnectionItem(
            connection.Id,
            connection.Provider,
            connection.Category.ToString(),
            connection.Status.ToString(),
            connection.Direction.ToString(),
            connection.SyncEnabled,
            connection.AccountName,
            connection.ContainerId,
            connection.ContainerName,
            connection.TimeZone,
            connection.LastSyncedAt,
            connection.SyncEnabled && connection.Status != IntegrationStatus.NeedsReconnect ? connection.NextSyncAt : null,
            connection.ConsecutiveFailures,
            connection.LastError,
            lastRun);
    }

    private async Task<ConnectionState> FindAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken)
    {
        return await store.FindAsync(userId, connectionId, cancellationToken) ?? throw new NotFoundException("Integration was not found.");
    }

    private ICalendarProvider Calendar(ConnectionState connection)
    {
        return providers.Find<ICalendarProvider>(connection.Provider)
            ?? throw new ConflictException("This integration is not available on the server any more.");
    }

    private static void Require(IReadOnlyList<string> permissions, IntegrationCategory category)
    {
        if (!permissions.Contains(IntegrationCategories.ManagePermission(category)))
        {
            throw new ForbiddenException("You do not have permission to manage this integration.");
        }
    }

    /// <summary>Exponential backoff from 5 minutes up to 6 hours, honouring the provider's Retry-After.</summary>
    private static TimeSpan Backoff(int failures, TimeSpan? retryAfter)
    {
        var minutes = Math.Min(360, 5 * Math.Pow(2, Math.Clamp(failures - 1, 0, 10)));
        var delay = TimeSpan.FromMinutes(minutes);
        return retryAfter is { } wait && wait > delay ? wait : delay;
    }

    private static string? ValidZone(string? zone)
    {
        return !string.IsNullOrWhiteSpace(zone) && zone.Length <= 64 && TimeZoneInfo.TryFindSystemTimeZoneById(zone, out _) ? zone : null;
    }

    private OAuthState? ReadState(string? state)
    {
        if (string.IsNullOrEmpty(state))
        {
            return null;
        }

        var json = protector.Unprotect(state);
        try
        {
            return json is null ? null : JsonSerializer.Deserialize<OAuthState>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record OAuthState(Guid UserId, string Provider, string Verifier, string? TimeZone, long ExpiresAt);
}

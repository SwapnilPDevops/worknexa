using Worknexa.Domain.Enums;

namespace Worknexa.Application.Integrations;

public sealed record ConnectionState(
    Guid Id,
    Guid UserId,
    string Provider,
    IntegrationCategory Category,
    IntegrationStatus Status,
    SyncDirection Direction,
    bool SyncEnabled,
    string? AccountId,
    string? AccountName,
    string? ContainerId,
    string? ContainerName,
    string? TimeZone,
    string? AccessTokenProtected,
    string? RefreshTokenProtected,
    DateTimeOffset? TokenExpiresAt,
    string? SyncCursor,
    DateTimeOffset? CursorIssuedAt,
    DateTimeOffset? LastSyncedAt,
    DateTimeOffset? NextSyncAt,
    int ConsecutiveFailures,
    string? LastError);

public sealed record ConnectionDraft(
    string Provider,
    IntegrationCategory Category,
    string? AccountId,
    string? AccountName,
    string? TimeZone,
    string AccessTokenProtected,
    string? RefreshTokenProtected,
    DateTimeOffset TokenExpiresAt,
    DateTimeOffset NextSyncAt);

public sealed record ConnectionOutcome(
    IntegrationStatus Status,
    int ConsecutiveFailures,
    string? LastError,
    DateTimeOffset? LastSyncedAt,
    DateTimeOffset? NextSyncAt);

public sealed record SyncFailureDraft(string Operation, Guid? LocalId, string? ExternalId, string? Title, string Message);

public sealed record SyncFailureItem(string Operation, string? Title, string Message);

public sealed record SyncRunItem(
    Guid Id,
    string Trigger,
    string Direction,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int Imported,
    int Updated,
    int Exported,
    int Removed,
    int Skipped,
    int Conflicts,
    int Failed,
    string? Error,
    IReadOnlyList<SyncFailureItem> Failures);

/// <summary>Counts and item failures collected while a sync runs.</summary>
public sealed class SyncTally
{
    private const int MaxFailures = 50;

    public int Imported { get; set; }

    public int Updated { get; set; }

    public int Exported { get; set; }

    public int Removed { get; set; }

    public int Skipped { get; set; }

    public int Conflicts { get; set; }

    public int Failed { get; private set; }

    public List<SyncFailureDraft> Failures { get; } = [];

    public void Fail(string operation, Guid? localId, string? externalId, string? title, string message)
    {
        Failed++;
        if (Failures.Count < MaxFailures)
        {
            Failures.Add(new SyncFailureDraft(operation, localId, externalId, title, message));
        }
    }
}

public interface IIntegrationStore
{
    Task<IReadOnlyList<ConnectionState>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<ConnectionState?> FindAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken);

    Task<ConnectionState?> FindByProviderAsync(Guid userId, string provider, CancellationToken cancellationToken);

    Task<ConnectionState?> FindByIdAsync(Guid connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> DueAsync(IReadOnlyCollection<string> providers, DateTimeOffset now, int limit, CancellationToken cancellationToken);

    /// <summary>Creates the connection, or refreshes credentials on the existing one for this provider.</summary>
    Task<ConnectionState> UpsertAsync(Guid userId, ConnectionDraft draft, CancellationToken cancellationToken);

    Task SaveTokensAsync(Guid connectionId, string accessTokenProtected, string? refreshTokenProtected, DateTimeOffset expiresAt, CancellationToken cancellationToken);

    Task UpdateSettingsAsync(Guid connectionId, SyncDirection direction, bool syncEnabled, string? containerId, string? containerName, bool resetCursor, CancellationToken cancellationToken);

    Task SaveCursorAsync(Guid connectionId, string? cursor, DateTimeOffset? issuedAt, CancellationToken cancellationToken);

    /// <summary>Atomically claims the connection so two syncs never run at once.</summary>
    Task<bool> TryLockAsync(Guid connectionId, DateTimeOffset now, DateTimeOffset until, CancellationToken cancellationToken);

    /// <summary>Records the result on the connection and releases the lock.</summary>
    Task FinishAsync(Guid connectionId, ConnectionOutcome outcome, CancellationToken cancellationToken);

    /// <summary>Stops scheduling until the person signs in again. Leaves any running sync's lock alone.</summary>
    Task MarkReconnectAsync(Guid connectionId, string error, CancellationToken cancellationToken);

    Task DeleteAsync(Guid connectionId, CancellationToken cancellationToken);

    Task<Guid> StartRunAsync(Guid connectionId, SyncTrigger trigger, SyncDirection direction, DateTimeOffset startedAt, CancellationToken cancellationToken);

    Task FinishRunAsync(Guid runId, SyncRunStatus status, SyncTally tally, string? error, DateTimeOffset finishedAt, CancellationToken cancellationToken);

    Task<IReadOnlyList<SyncRunItem>> RunsAsync(Guid connectionId, int limit, CancellationToken cancellationToken);
}

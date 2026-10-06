using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Worknexa.Application.Integrations;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Integrations;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Integrations;

public sealed class EfIntegrationStore(WorknexaDbContext context, IOptions<IntegrationSettings> options) : IIntegrationStore
{
    public async Task<IReadOnlyList<ConnectionState>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await Project(context.IntegrationConnections.AsNoTracking().Where(connection => connection.UserId == userId).OrderBy(connection => connection.Provider))
            .ToListAsync(cancellationToken);
    }

    public Task<ConnectionState?> FindAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken)
    {
        return Project(context.IntegrationConnections.AsNoTracking().Where(connection => connection.Id == connectionId && connection.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ConnectionState?> FindByProviderAsync(Guid userId, string provider, CancellationToken cancellationToken)
    {
        return Project(context.IntegrationConnections.AsNoTracking().Where(connection => connection.UserId == userId && connection.Provider == provider))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ConnectionState?> FindByIdAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        return Project(context.IntegrationConnections.AsNoTracking().Where(connection => connection.Id == connectionId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> DueAsync(IReadOnlyCollection<string> providers, DateTimeOffset now, int limit, CancellationToken cancellationToken)
    {
        return await context.IntegrationConnections.AsNoTracking()
            .Where(connection =>
                connection.SyncEnabled
                && connection.Status != IntegrationStatus.NeedsReconnect
                && providers.Contains(connection.Provider)
                && (connection.NextSyncAt == null || connection.NextSyncAt <= now)
                && (connection.LockedUntil == null || connection.LockedUntil < now))
            .OrderBy(connection => connection.NextSyncAt)
            .Take(limit)
            .Select(connection => connection.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConnectionState> UpsertAsync(Guid userId, ConnectionDraft draft, CancellationToken cancellationToken)
    {
        var connection = await context.IntegrationConnections.FirstOrDefaultAsync(item => item.UserId == userId && item.Provider == draft.Provider, cancellationToken);
        if (connection is null)
        {
            connection = new IntegrationConnection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = draft.Provider,
                Category = draft.Category,
                Direction = SyncDirection.ImportOnly,
                SyncEnabled = true
            };
            context.IntegrationConnections.Add(connection);
        }

        connection.Status = IntegrationStatus.Connected;
        connection.ExternalAccountId = draft.AccountId;
        connection.ExternalAccountName = Truncate(draft.AccountName, 200);
        connection.TimeZone = draft.TimeZone ?? connection.TimeZone;
        connection.AccessTokenProtected = draft.AccessTokenProtected;
        connection.RefreshTokenProtected = draft.RefreshTokenProtected ?? connection.RefreshTokenProtected;
        connection.TokenExpiresAt = draft.TokenExpiresAt;
        connection.NextSyncAt = draft.NextSyncAt;
        connection.ConsecutiveFailures = 0;
        connection.LastError = null;
        await context.SaveChangesAsync(cancellationToken);
        return (await FindByIdAsync(connection.Id, cancellationToken))!;
    }

    public Task SaveTokensAsync(Guid connectionId, string accessTokenProtected, string? refreshTokenProtected, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        return context.IntegrationConnections.Where(connection => connection.Id == connectionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(connection => connection.AccessTokenProtected, accessTokenProtected)
                .SetProperty(connection => connection.RefreshTokenProtected, refreshTokenProtected)
                .SetProperty(connection => connection.TokenExpiresAt, expiresAt), cancellationToken);
    }

    public async Task UpdateSettingsAsync(Guid connectionId, SyncDirection direction, bool syncEnabled, string? containerId, string? containerName, bool resetCursor, CancellationToken cancellationToken)
    {
        var connection = await context.IntegrationConnections.FirstAsync(item => item.Id == connectionId, cancellationToken);
        connection.Direction = direction;
        connection.SyncEnabled = syncEnabled;
        connection.ExternalContainerId = containerId;
        connection.ExternalContainerName = Truncate(containerName, 200);
        if (resetCursor)
        {
            connection.SyncCursor = null;
            connection.CursorIssuedAt = null;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveCursorAsync(Guid connectionId, string? cursor, DateTimeOffset? issuedAt, CancellationToken cancellationToken)
    {
        return context.IntegrationConnections.Where(connection => connection.Id == connectionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(connection => connection.SyncCursor, cursor)
                .SetProperty(connection => connection.CursorIssuedAt, issuedAt), cancellationToken);
    }

    public async Task<bool> TryLockAsync(Guid connectionId, DateTimeOffset now, DateTimeOffset until, CancellationToken cancellationToken)
    {
        var claimed = await context.IntegrationConnections
            .Where(connection => connection.Id == connectionId && (connection.LockedUntil == null || connection.LockedUntil < now))
            .ExecuteUpdateAsync(setters => setters.SetProperty(connection => connection.LockedUntil, until), cancellationToken);
        return claimed == 1;
    }

    public Task FinishAsync(Guid connectionId, ConnectionOutcome outcome, CancellationToken cancellationToken)
    {
        var lastError = Truncate(outcome.LastError, 1000);
        return context.IntegrationConnections.Where(connection => connection.Id == connectionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(connection => connection.Status, outcome.Status)
                .SetProperty(connection => connection.ConsecutiveFailures, outcome.ConsecutiveFailures)
                .SetProperty(connection => connection.LastError, lastError)
                .SetProperty(connection => connection.LastSyncedAt, outcome.LastSyncedAt)
                .SetProperty(connection => connection.NextSyncAt, outcome.NextSyncAt)
                .SetProperty(connection => connection.LockedUntil, (DateTimeOffset?)null), cancellationToken);
    }

    public Task MarkReconnectAsync(Guid connectionId, string error, CancellationToken cancellationToken)
    {
        var lastError = Truncate(error, 1000);
        return context.IntegrationConnections.Where(connection => connection.Id == connectionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(connection => connection.Status, IntegrationStatus.NeedsReconnect)
                .SetProperty(connection => connection.LastError, lastError)
                .SetProperty(connection => connection.NextSyncAt, (DateTimeOffset?)null), cancellationToken);
    }

    public Task DeleteAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        return context.IntegrationConnections.Where(connection => connection.Id == connectionId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<Guid> StartRunAsync(Guid connectionId, SyncTrigger trigger, SyncDirection direction, DateTimeOffset startedAt, CancellationToken cancellationToken)
    {
        var run = new SyncRun
        {
            Id = Guid.NewGuid(),
            ConnectionId = connectionId,
            Trigger = trigger,
            Direction = direction,
            Status = SyncRunStatus.Running,
            StartedAt = startedAt
        };
        context.SyncRuns.Add(run);
        await context.SaveChangesAsync(cancellationToken);
        return run.Id;
    }

    public async Task FinishRunAsync(Guid runId, SyncRunStatus status, SyncTally tally, string? error, DateTimeOffset finishedAt, CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        var run = await context.SyncRuns.FirstAsync(item => item.Id == runId, cancellationToken);
        run.Status = status;
        run.FinishedAt = finishedAt;
        run.Imported = tally.Imported;
        run.Updated = tally.Updated;
        run.Exported = tally.Exported;
        run.Removed = tally.Removed;
        run.Skipped = tally.Skipped;
        run.Conflicts = tally.Conflicts;
        run.Failed = tally.Failed;
        run.Error = Truncate(error, 1000);
        foreach (var failure in tally.Failures)
        {
            context.SyncFailures.Add(new SyncFailure
            {
                Id = Guid.NewGuid(),
                RunId = runId,
                Operation = failure.Operation,
                LocalId = failure.LocalId,
                ExternalId = Truncate(failure.ExternalId, 500),
                Title = Truncate(failure.Title, 300),
                Message = Truncate(failure.Message, 1000) ?? "Failed."
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var keep = Math.Max(5, options.Value.RunsKept);
        var stale = await context.SyncRuns.AsNoTracking()
            .Where(item => item.ConnectionId == run.ConnectionId)
            .OrderByDescending(item => item.StartedAt)
            .Skip(keep)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (stale.Count > 0)
        {
            await context.SyncRuns.Where(item => stale.Contains(item.Id)).ExecuteDeleteAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<SyncRunItem>> RunsAsync(Guid connectionId, int limit, CancellationToken cancellationToken)
    {
        var runs = await context.SyncRuns.AsNoTracking()
            .Where(run => run.ConnectionId == connectionId)
            .OrderByDescending(run => run.StartedAt)
            .Take(limit)
            .Select(run => new
            {
                run.Id,
                run.Trigger,
                run.Direction,
                run.Status,
                run.StartedAt,
                run.FinishedAt,
                run.Imported,
                run.Updated,
                run.Exported,
                run.Removed,
                run.Skipped,
                run.Conflicts,
                run.Failed,
                run.Error,
                Failures = run.Failures.OrderBy(failure => failure.CreatedAt).Take(20).Select(failure => new SyncFailureItem(failure.Operation, failure.Title, failure.Message)).ToList()
            })
            .ToListAsync(cancellationToken);

        return runs.Select(run => new SyncRunItem(
            run.Id,
            run.Trigger.ToString(),
            run.Direction.ToString(),
            run.Status.ToString(),
            run.StartedAt,
            run.FinishedAt,
            run.Imported,
            run.Updated,
            run.Exported,
            run.Removed,
            run.Skipped,
            run.Conflicts,
            run.Failed,
            run.Error,
            run.Failures)).ToArray();
    }

    private static IQueryable<ConnectionState> Project(IQueryable<IntegrationConnection> connections)
    {
        return connections.Select(connection => new ConnectionState(
            connection.Id,
            connection.UserId,
            connection.Provider,
            connection.Category,
            connection.Status,
            connection.Direction,
            connection.SyncEnabled,
            connection.ExternalAccountId,
            connection.ExternalAccountName,
            connection.ExternalContainerId,
            connection.ExternalContainerName,
            connection.TimeZone,
            connection.AccessTokenProtected,
            connection.RefreshTokenProtected,
            connection.TokenExpiresAt,
            connection.SyncCursor,
            connection.CursorIssuedAt,
            connection.LastSyncedAt,
            connection.NextSyncAt,
            connection.ConsecutiveFailures,
            connection.LastError));
    }

    private static string? Truncate(string? value, int limit) => value is null || value.Length <= limit ? value : value[..limit];
}

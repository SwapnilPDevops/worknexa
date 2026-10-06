using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Health;

/// <summary>
/// Readiness fails while the database is behind this build, so a rollout that skipped the migration step
/// never receives traffic. The API never migrates on its own; the migration job does.
/// </summary>
public sealed class MigrationsHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MigrationsHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WorknexaDbContext>();
            var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            return pending.Length == 0
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"{pending.Length} database migration(s) have not been applied.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Migration state could not be read.", exception);
        }
    }
}

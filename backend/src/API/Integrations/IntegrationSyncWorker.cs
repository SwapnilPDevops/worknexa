using Microsoft.Extensions.Options;
using Worknexa.Application.Common;
using Worknexa.Application.Integrations;

namespace Worknexa.Api.Integrations;

/// <summary>Runs scheduled syncs. Each connection gets its own scope so one failure cannot affect another.</summary>
public sealed class IntegrationSyncWorker(IServiceScopeFactory scopes, IOptions<IntegrationSettings> options, ILogger<IntegrationSyncWorker> logger) : BackgroundService
{
    private const int BatchSize = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.BackgroundSync)
        {
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            do
            {
                await TickAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task TickAsync(CancellationToken stoppingToken)
    {
        IReadOnlyList<Guid> due;
        try
        {
            using var scope = scopes.CreateScope();
            due = await scope.ServiceProvider.GetRequiredService<IIntegrationService>().DueAsync(DateTimeOffset.UtcNow, BatchSize, stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Finding integrations to sync failed");
            return;
        }

        foreach (var connectionId in due)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IIntegrationService>().RunScheduledAsync(connectionId, DateTimeOffset.UtcNow, stoppingToken);
            }
            catch (ConflictException)
            {
                // Another run holds the lock; the next tick picks the connection up again.
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Scheduled sync for connection {ConnectionId} failed", connectionId);
            }
        }
    }
}

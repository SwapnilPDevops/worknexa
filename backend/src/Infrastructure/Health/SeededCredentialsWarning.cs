using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Worknexa.Infrastructure.Persistence;
using Worknexa.Infrastructure.Persistence.Seed;

namespace Worknexa.Infrastructure.Health;

/// <summary>
/// The migrations seed admin, manager and member accounts with a published development password.
/// Outside Development, log a critical alert at startup while any of them can still sign in with it.
/// </summary>
internal sealed class SeededCredentialsWarning : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SeededCredentialsWarning> _logger;

    public SeededCredentialsWarning(IServiceScopeFactory scopeFactory, IHostEnvironment environment, ILogger<SeededCredentialsWarning> logger)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_environment.IsDevelopment())
        {
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WorknexaDbContext>();
            var exposed = await dbContext.Users
                .AsNoTracking()
                .CountAsync(user => user.IsActive && user.PasswordHash == WorknexaSeed.DevelopmentPasswordHash, cancellationToken);
            if (exposed > 0)
            {
                _logger.LogCritical(
                    "{Count} active account(s) still use the seeded development password. Sign in as each seeded account and change its password (docs/deployment/production.md).",
                    exposed);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning("Could not check for seeded development credentials ({ExceptionType}).", exception.GetType().Name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

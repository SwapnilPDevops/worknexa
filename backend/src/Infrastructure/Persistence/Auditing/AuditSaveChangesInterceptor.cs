using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Worknexa.Application.Abstractions;
using Worknexa.Domain.Common;
using Worknexa.Infrastructure.Platform;

namespace Worknexa.Infrastructure.Persistence.Auditing;

public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser? _currentUser;
    private bool _capturing;

    public AuditSaveChangesInterceptor(ICurrentUser? currentUser = null)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ApplyStamps(eventData.Context);
            if (!_capturing)
            {
                _capturing = true;
                try
                {
                    await ActivityCapture.ApplyAsync(eventData.Context, _currentUser?.UserId, cancellationToken);
                    ApplyStamps(eventData.Context);
                }
                finally
                {
                    _capturing = false;
                }
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        ApplyStamps(context);
        if (_capturing)
        {
            return;
        }

        _capturing = true;
        try
        {
            ActivityCapture.Apply(context, _currentUser?.UserId);
            ApplyStamps(context);
        }
        finally
        {
            _capturing = false;
        }
    }

    private void ApplyStamps(DbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = _currentUser?.UserId;
        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                if (entry.Entity.UpdatedAt == default)
                {
                    entry.Entity.UpdatedAt = now;
                }

                if (entry.Entity.CreatedBy is null)
                {
                    entry.Entity.CreatedBy = userId;
                }

                if (entry.Entity.UpdatedBy is null)
                {
                    entry.Entity.UpdatedBy = userId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                if (userId is not null)
                {
                    entry.Entity.UpdatedBy = userId;
                }
            }
        }
    }
}

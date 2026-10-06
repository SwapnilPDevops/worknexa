namespace Worknexa.Application.Platform;

public sealed record ActivityItem(
    Guid Id,
    Guid? ActorId,
    string ActorName,
    string Action,
    string Entity,
    Guid EntityId,
    DateTimeOffset Timestamp,
    string? Metadata,
    string Summary);

public interface IActivityStore
{
    Task<IReadOnlyList<ActivityItem>> ListAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IActivityService
{
    Task<IReadOnlyList<ActivityItem>> ListAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class ActivityService(IActivityStore store) : IActivityService
{
    public Task<IReadOnlyList<ActivityItem>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return store.ListAsync(userId, cancellationToken);
    }
}

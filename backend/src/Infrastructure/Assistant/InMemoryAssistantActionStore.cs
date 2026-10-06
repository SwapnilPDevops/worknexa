using System.Collections.Concurrent;
using Worknexa.Application.Assistant;

namespace Worknexa.Infrastructure.Assistant;

/// <summary>
/// Proposals live for a few minutes in this process. Running several API instances needs a shared store
/// (for example Redis or a database table) behind the same interface.
/// </summary>
public sealed class InMemoryAssistantActionStore : IAssistantActionStore
{
    private readonly ConcurrentDictionary<Guid, PendingAction> _actions = new();

    public void Save(PendingAction action)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var expired in _actions.Where(entry => entry.Value.ExpiresAt <= now))
        {
            _actions.TryRemove(expired);
        }

        _actions[action.Id] = action;
    }

    public PendingAction? Take(Guid userId, Guid actionId, DateTimeOffset now)
    {
        if (!_actions.TryGetValue(actionId, out var action) || action.UserId != userId)
        {
            return null;
        }

        if (!_actions.TryRemove(new KeyValuePair<Guid, PendingAction>(actionId, action)))
        {
            return null;
        }

        return action.ExpiresAt > now ? action : null;
    }

    public bool Remove(Guid userId, Guid actionId)
    {
        return _actions.TryGetValue(actionId, out var action)
            && action.UserId == userId
            && _actions.TryRemove(new KeyValuePair<Guid, PendingAction>(actionId, action));
    }
}

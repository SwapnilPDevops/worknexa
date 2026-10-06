using Worknexa.Application.Abstractions;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Tests;

public sealed class InMemoryAccountStore : IAccountStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, AccountRecord> _accounts;

    public InMemoryAccountStore(IPasswordHasher passwords)
    {
        var hash = passwords.Hash("Worknexa_Dev123!");
        _accounts = new Dictionary<Guid, AccountRecord>
        {
            [Id("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2d")] = Account(Id("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2d"), "admin@worknexa.local", "Worknexa Admin", hash, RoleNames.Admin),
            [Id("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2e")] = Account(Id("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2e"), "manager@worknexa.local", "Worknexa Manager", hash, RoleNames.Manager),
            [Id("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2f")] = Account(Id("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2f"), "member@worknexa.local", "Worknexa Member", hash, RoleNames.TeamMember)
        };
    }

    public Task<AccountRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        lock (_gate)
        {
            return Task.FromResult(_accounts.Values.FirstOrDefault(account => account.Email == normalized));
        }
    }

    public Task<AccountRecord?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_accounts.TryGetValue(userId, out var account) ? account : null);
        }
    }

    public Task UpdateProfileAsync(Guid userId, string displayName, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var account = _accounts[userId];
            _accounts[userId] = account with { DisplayName = displayName };
        }

        return Task.CompletedTask;
    }

    public Task UpdatePasswordAsync(Guid userId, string passwordHash, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var account = _accounts[userId];
            _accounts[userId] = account with { PasswordHash = passwordHash };
        }

        return Task.CompletedTask;
    }

    private static AccountRecord Account(Guid id, string email, string displayName, string passwordHash, string role)
    {
        return new AccountRecord(id, email, displayName, passwordHash, true, [role], PermissionCatalog.KeysForRole(role));
    }

    private static Guid Id(string value) => Guid.Parse(value);
}

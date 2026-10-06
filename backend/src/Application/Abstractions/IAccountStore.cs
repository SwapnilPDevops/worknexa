namespace Worknexa.Application.Abstractions;

public sealed record AccountRecord(
    Guid Id,
    string Email,
    string DisplayName,
    string? PasswordHash,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions)
{
    public UserSession ToSession()
    {
        return new UserSession(Id, Email, DisplayName, Roles, Permissions);
    }
}

public sealed record UserSession(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public interface IAccountStore
{
    Task<AccountRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<AccountRecord?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task UpdateProfileAsync(Guid userId, string displayName, CancellationToken cancellationToken);

    Task UpdatePasswordAsync(Guid userId, string passwordHash, CancellationToken cancellationToken);
}

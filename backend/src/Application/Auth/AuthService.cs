using System.Runtime.CompilerServices;
using Worknexa.Application.Abstractions;

namespace Worknexa.Application.Auth;

public interface IAuthService
{
    Task<AccountRecord> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<UserSession> GetCurrentAsync(CancellationToken cancellationToken);

    Task<UserSession> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken);

    Task<AccountRecord> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);
}

public sealed class AuthService : IAuthService
{
    private readonly IAccountStore _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwords;

    public AuthService(IAccountStore accounts, ICurrentUser currentUser, IPasswordHasher passwords)
    {
        _accounts = accounts;
        _currentUser = currentUser;
        _passwords = passwords;
    }

    public async Task<AccountRecord> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByEmailAsync(request.Email, cancellationToken);
        if (account is not { IsActive: true, PasswordHash: { } passwordHash })
        {
            // Hash anyway so response time does not reveal which emails have active accounts.
            _passwords.Verify(DecoyHash(), request.Password);
            throw new AuthenticationFailedException();
        }

        if (!_passwords.Verify(passwordHash, request.Password))
        {
            throw new AuthenticationFailedException();
        }

        return account;
    }

    private static readonly ConditionalWeakTable<IPasswordHasher, string> DecoyHashes = new();

    private string DecoyHash() => DecoyHashes.GetValue(_passwords, hasher => hasher.Hash(Guid.NewGuid().ToString("N")));

    public async Task<UserSession> GetCurrentAsync(CancellationToken cancellationToken)
    {
        return (await RequireAccountAsync(cancellationToken)).ToSession();
    }

    public async Task<UserSession> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var account = await RequireAccountAsync(cancellationToken);
        var displayName = request.DisplayName.Trim();
        await _accounts.UpdateProfileAsync(account.Id, displayName, cancellationToken);
        return account.ToSession() with { DisplayName = displayName };
    }

    public async Task<AccountRecord> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var account = await RequireAccountAsync(cancellationToken);
        if (account.PasswordHash is null || !_passwords.Verify(account.PasswordHash, request.CurrentPassword))
        {
            throw new AuthenticationFailedException();
        }

        var passwordHash = _passwords.Hash(request.NewPassword);
        await _accounts.UpdatePasswordAsync(account.Id, passwordHash, cancellationToken);
        return account with { PasswordHash = passwordHash };
    }

    private async Task<AccountRecord> RequireAccountAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            throw new UnauthorizedAccessException();
        }

        var account = await _accounts.FindByIdAsync(userId, cancellationToken);
        if (account is not { IsActive: true, PasswordHash: not null })
        {
            throw new UnauthorizedAccessException();
        }

        return account;
    }
}

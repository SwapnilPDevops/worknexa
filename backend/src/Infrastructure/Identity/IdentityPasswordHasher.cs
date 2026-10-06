using Microsoft.AspNetCore.Identity;
using Worknexa.Application.Abstractions;
using Worknexa.Domain.Identity;

namespace Worknexa.Infrastructure.Identity;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword(new User(), password);
    }

    public bool Verify(string passwordHash, string password)
    {
        return _hasher.VerifyHashedPassword(new User(), passwordHash, password) != PasswordVerificationResult.Failed;
    }
}

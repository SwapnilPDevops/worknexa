using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Abstractions;
using Worknexa.Domain.Identity;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Identity;

public sealed class EfAccountStore : IAccountStore
{
    private readonly WorknexaDbContext _context;

    public EfAccountStore(WorknexaDbContext context)
    {
        _context = context;
    }

    public async Task<AccountRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await Query()
            .FirstOrDefaultAsync(candidate => candidate.Email.ToLower() == normalized, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<AccountRecord?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await Query().FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task UpdateProfileAsync(Guid userId, string displayName, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("The signed-in user no longer exists.");
        user.DisplayName = displayName;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePasswordAsync(Guid userId, string passwordHash, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("The signed-in user no longer exists.");
        user.PasswordHash = passwordHash;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<User> Query()
    {
        return _context.Users
            .AsNoTracking()
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .ThenInclude(role => role.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission);
    }

    private static AccountRecord Map(User user)
    {
        var roles = user.UserRoles
            .Select(userRole => userRole.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var permissions = user.UserRoles
            .SelectMany(userRole => userRole.Role.RolePermissions)
            .Select(rolePermission => rolePermission.Permission.Key)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return new AccountRecord(user.Id, user.Email, user.DisplayName, user.PasswordHash, user.IsActive, roles, permissions);
    }
}

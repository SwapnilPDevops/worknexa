using Microsoft.AspNetCore.Authorization;
using Worknexa.Application.Abstractions;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Auth;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IAccountStore _accounts;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(IAccountStore accounts, IHttpContextAccessor httpContextAccessor)
    {
        _accounts = accounts;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var value = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(value, out var userId))
        {
            return;
        }

        var account = await LoadAccountAsync(userId);
        if (account is { IsActive: true } && account.Permissions.Contains(requirement.Permission, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
        }
    }

    private async Task<AccountRecord?> LoadAccountAsync(Guid userId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items["worknexa.account"] is AccountRecord cached && cached.Id == userId)
        {
            return cached;
        }

        var account = await _accounts.FindByIdAsync(userId, httpContext?.RequestAborted ?? CancellationToken.None);
        if (httpContext is not null && account is not null)
        {
            httpContext.Items["worknexa.account"] = account;
        }

        return account;
    }
}

public static class PermissionPolicyExtensions
{
    public static void AddPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var permission in PermissionCatalog.All)
        {
            options.AddPolicy(permission.Key, policy => policy.Requirements.Add(new PermissionRequirement(permission.Key)));
        }
    }
}

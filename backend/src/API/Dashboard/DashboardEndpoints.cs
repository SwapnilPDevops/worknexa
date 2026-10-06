using Worknexa.Api.Contracts;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Analytics;
using Worknexa.Application.Dashboard;

namespace Worknexa.Api.Dashboard;

public static class DashboardEndpoints
{
    public const string AccountItemKey = "worknexa.account";

    public static RouteGroupBuilder MapWorknexaDashboard(this RouteGroupBuilder api)
    {
        var dashboard = api.MapGroup("/dashboard").WithTags("Dashboard").RequireAuthorization();

        dashboard.MapGet("/my-day", async (
            DateOnly? date,
            int? offsetMinutes,
            IMyDayService service,
            ICurrentUser currentUser,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await PermissionsAsync(httpContext, userId, cancellationToken);
            var response = await service.GetAsync(
                userId,
                permissions,
                date ?? DateOnly.FromDateTime(DateTime.UtcNow),
                offsetMinutes ?? 0,
                cancellationToken);
            return ApiResults.Success(response);
        });

        dashboard.MapGet("/analytics", async (
            int? weeks,
            int? offsetMinutes,
            Guid? teamId,
            IAnalyticsService service,
            ICurrentUser currentUser,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await PermissionsAsync(httpContext, userId, cancellationToken);
            var report = await service.GetAsync(
                userId,
                permissions,
                new AnalyticsQuery(weeks ?? 8, offsetMinutes ?? 0, teamId),
                DateTimeOffset.UtcNow,
                cancellationToken);
            return ApiResults.Success(report);
        });

        dashboard.MapPost("/quick-capture", async (
            QuickCaptureRequest request,
            IMyDayService service,
            ICurrentUser currentUser,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await PermissionsAsync(httpContext, userId, cancellationToken);
            await service.CaptureAsync(userId, permissions, request, cancellationToken);
            return Results.NoContent();
        });

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }

    internal static async Task<IReadOnlyList<string>> PermissionsAsync(HttpContext httpContext, Guid userId, CancellationToken cancellationToken)
    {
        if (httpContext.Items[AccountItemKey] is AccountRecord cached)
        {
            return cached.Permissions;
        }

        var accounts = httpContext.RequestServices.GetRequiredService<IAccountStore>();
        var account = await accounts.FindByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedAccessException();
        return account.Permissions;
    }
}

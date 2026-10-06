using Worknexa.Api.Contracts;
using Worknexa.Api.Dashboard;
using Worknexa.Api.Security;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Assistant;

namespace Worknexa.Api.Assistant;

public static class AssistantEndpoints
{
    public static RouteGroupBuilder MapWorknexaAssistant(this RouteGroupBuilder api)
    {
        var assistant = api.MapGroup("/assistant").WithTags("Assistant").RequireAuthorization();

        assistant.MapGet("/status", async (IAssistantService service, ICurrentUser currentUser, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await DashboardEndpoints.PermissionsAsync(httpContext, userId, cancellationToken);
            return ApiResults.Success(service.Status(permissions));
        });

        assistant.MapPost("/chat", async (AssistantChatRequest request, IAssistantService service, ICurrentUser currentUser, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await DashboardEndpoints.PermissionsAsync(httpContext, userId, cancellationToken);
            var reply = await service.ChatAsync(userId, permissions, request, DateTimeOffset.UtcNow, cancellationToken);
            return ApiResults.Success(reply);
        }).RequireRateLimiting(RateLimitPolicies.AssistantChat);

        assistant.MapPost("/actions/{actionId:guid}/confirm", async (Guid actionId, IAssistantService service, ICurrentUser currentUser, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await DashboardEndpoints.PermissionsAsync(httpContext, userId, cancellationToken);
            var result = await service.ConfirmAsync(userId, permissions, actionId, DateTimeOffset.UtcNow, cancellationToken);
            return ApiResults.Success(result);
        });

        assistant.MapDelete("/actions/{actionId:guid}", (Guid actionId, IAssistantService service, ICurrentUser currentUser) =>
        {
            service.Cancel(RequireUser(currentUser), actionId);
            return Results.NoContent();
        });

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }
}

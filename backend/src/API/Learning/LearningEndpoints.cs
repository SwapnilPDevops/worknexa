using Worknexa.Api.Contracts;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Learning;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Learning;

public static class LearningEndpoints
{
    public static RouteGroupBuilder MapWorknexaLearning(this RouteGroupBuilder api)
    {
        var learning = api.MapGroup("/learning").WithTags("Learning");
        learning.MapGet("/dashboard", async (DateOnly? date, ILearningService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.DashboardAsync(RequireUser(currentUser), date ?? DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LearningRead);

        learning.MapGet("/roadmap", async (ILearningService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.RoadmapAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LearningRead);

        learning.MapPost("/roadmap/example", async (ILearningService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CreateExampleAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LearningManage);

        learning.MapGet("/topics", async (string? q, ILearningService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ListAsync(RequireUser(currentUser), q, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LearningRead);

        learning.MapGet("/topics/{topicId:guid}", async (Guid topicId, ILearningService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.GetAsync(RequireUser(currentUser), topicId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LearningRead);

        learning.MapPost("/topics", async (LearningTopicWrite request, ILearningService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CreateAsync(RequireUser(currentUser), request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LearningManage);

        learning.MapPut("/topics/{topicId:guid}", async (Guid topicId, LearningTopicWrite request, ILearningService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.UpdateAsync(RequireUser(currentUser), topicId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LearningManage);

        learning.MapDelete("/topics/{topicId:guid}", async (Guid topicId, ILearningService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(RequireUser(currentUser), topicId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.LearningManage);

        learning.MapPost("/topics/{topicId:guid}/resources", async (Guid topicId, LearningResourceWrite request, ILearningService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.AddResourceAsync(RequireUser(currentUser), topicId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LearningManage);

        learning.MapDelete("/topics/{topicId:guid}/resources/{resourceId:guid}", async (Guid topicId, Guid resourceId, ILearningService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.DeleteResourceAsync(RequireUser(currentUser), topicId, resourceId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.LearningManage);

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }
}

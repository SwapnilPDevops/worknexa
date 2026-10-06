using Worknexa.Api.Contracts;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Projects;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Projects;

public static class ProjectEndpoints
{
    public static RouteGroupBuilder MapWorknexaProjects(this RouteGroupBuilder api)
    {
        var projects = api.MapGroup("/projects").WithTags("Projects");
        projects.MapGet("/", async (string? q, IProjectService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ListAsync(RequireUser(currentUser), q, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.ProjectsRead);

        projects.MapGet("/options", async (IProjectService service, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.OptionsAsync(cancellationToken)))
            .RequireAuthorization(PermissionCatalog.ProjectsRead);

        projects.MapGet("/{projectId:guid}", async (Guid projectId, IProjectService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.GetAsync(RequireUser(currentUser), projectId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.ProjectsRead);

        projects.MapGet("/{projectId:guid}/activity", async (Guid projectId, IProjectService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ActivityAsync(RequireUser(currentUser), projectId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.ProjectsRead);

        projects.MapGet("/{projectId:guid}/meetings", async (Guid projectId, IProjectService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.MeetingsAsync(RequireUser(currentUser), projectId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.ProjectsRead);

        projects.MapGet("/{projectId:guid}/notes", async (Guid projectId, IProjectService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.NotesAsync(RequireUser(currentUser), projectId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.ProjectsRead);

        projects.MapPost("/", async (ProjectWrite request, IProjectService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CreateAsync(RequireUser(currentUser), request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.ProjectsManage);

        projects.MapPut("/{projectId:guid}", async (Guid projectId, ProjectWrite request, IProjectService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.UpdateAsync(RequireUser(currentUser), projectId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.ProjectsManage);

        projects.MapDelete("/{projectId:guid}", async (Guid projectId, IProjectService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(RequireUser(currentUser), projectId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.ProjectsManage);

        projects.MapPost("/{projectId:guid}/members", async (Guid projectId, ProjectMemberWrite request, IProjectService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.AddMemberAsync(RequireUser(currentUser), projectId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.ProjectsManage);

        projects.MapDelete("/{projectId:guid}/members/{memberId:guid}", async (Guid projectId, Guid memberId, IProjectService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.RemoveMemberAsync(RequireUser(currentUser), projectId, memberId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.ProjectsManage);

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }
}

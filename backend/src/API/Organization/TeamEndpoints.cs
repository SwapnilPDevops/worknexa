using Worknexa.Api.Contracts;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Organization;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Organization;

public static class TeamEndpoints
{
    public static RouteGroupBuilder MapWorknexaTeams(this RouteGroupBuilder api)
    {
        var teams = api.MapGroup("/teams").WithTags("Teams");
        teams.MapGet("/", async (ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ListAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsRead);

        teams.MapGet("/options", async (ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.OptionsAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsRead);

        teams.MapGet("/workload", async (Guid teamId, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.WorkloadAsync(RequireUser(currentUser), teamId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsRead);

        teams.MapGet("/{teamId:guid}", async (Guid teamId, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.GetAsync(RequireUser(currentUser), teamId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsRead);

        teams.MapPost("/", async (TeamWrite request, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CreateAsync(RequireUser(currentUser), request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsManage);

        teams.MapPut("/{teamId:guid}", async (Guid teamId, TeamWrite request, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.UpdateAsync(RequireUser(currentUser), teamId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsManage);

        teams.MapDelete("/{teamId:guid}", async (Guid teamId, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(RequireUser(currentUser), teamId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.TeamsManage);

        teams.MapPost("/{teamId:guid}/members", async (Guid teamId, TeamMembershipWrite request, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.AddMemberAsync(RequireUser(currentUser), teamId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsManage);

        teams.MapDelete("/{teamId:guid}/members/{memberUserId:guid}", async (Guid teamId, Guid memberUserId, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.RemoveMemberAsync(RequireUser(currentUser), teamId, memberUserId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsManage);

        var members = api.MapGroup("/members").WithTags("Team members");
        members.MapGet("/", async (string? q, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.MembersAsync(RequireUser(currentUser), q, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsRead);

        members.MapGet("/{memberId:guid}", async (Guid memberId, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.GetMemberAsync(RequireUser(currentUser), memberId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsRead);

        members.MapPut("/{memberId:guid}", async (Guid memberId, MemberWrite request, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.UpdateMemberAsync(RequireUser(currentUser), memberId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsManage);

        members.MapGet("/{memberId:guid}/projects", async (Guid memberId, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ProjectsAsync(RequireUser(currentUser), memberId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsRead);

        members.MapGet("/{memberId:guid}/standups", async (Guid memberId, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.StandupsAsync(RequireUser(currentUser), memberId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsRead);

        members.MapGet("/{memberId:guid}/activity", async (Guid memberId, ITeamService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ActivityAsync(RequireUser(currentUser), memberId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TeamsRead);

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }
}

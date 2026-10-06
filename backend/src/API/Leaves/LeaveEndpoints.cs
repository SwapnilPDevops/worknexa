using Worknexa.Api.Contracts;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Leaves;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Leaves;

public static class LeaveEndpoints
{
    public static RouteGroupBuilder MapWorknexaLeaves(this RouteGroupBuilder api)
    {
        var leaves = api.MapGroup("/leaves").WithTags("Leaves");
        leaves.MapGet("/", async (Guid? employeeId, DateOnly? from, DateOnly? to, ILeaveService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ListAsync(RequireUser(currentUser), employeeId, from, to, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LeavesRead);

        leaves.MapGet("/options", async (ILeaveService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.OptionsAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LeavesRead);

        leaves.MapGet("/availability", async (Guid userId, DateOnly on, ILeaveService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.WarningAsync(RequireUser(currentUser), userId, on, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LeavesRead);

        leaves.MapGet("/availability/team", async (Guid? teamId, DateOnly from, DateOnly to, ILeaveService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.TeamAvailabilityAsync(RequireUser(currentUser), teamId, from, to, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LeavesRead);

        leaves.MapPost("/", async (LeaveWrite request, ILeaveService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CreateAsync(RequireUser(currentUser), request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LeavesRead);

        leaves.MapPut("/{leaveId:guid}", async (Guid leaveId, LeaveWrite request, ILeaveService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.UpdateAsync(RequireUser(currentUser), leaveId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LeavesRead);

        leaves.MapPost("/{leaveId:guid}/decision", async (Guid leaveId, LeaveDecision request, ILeaveService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.DecideAsync(RequireUser(currentUser), leaveId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.LeavesRead);

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }
}

using Worknexa.Api.Contracts;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Meetings;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Meetings;

public static class MeetingEndpoints
{
    public static RouteGroupBuilder MapWorknexaMeetings(this RouteGroupBuilder api)
    {
        var meetings = api.MapGroup("/meetings").WithTags("Meetings");
        meetings.MapGet("/", async (string? view, DateOnly date, int offsetMinutes, IMeetingService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ListAsync(RequireUser(currentUser), view, date, offsetMinutes, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.MeetingsRead);

        meetings.MapGet("/{meetingId:guid}", async (Guid meetingId, IMeetingService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.GetAsync(RequireUser(currentUser), meetingId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.MeetingsRead);

        meetings.MapPost("/", async (MeetingWrite request, IMeetingService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CreateAsync(RequireUser(currentUser), request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.MeetingsManage);

        meetings.MapPut("/{meetingId:guid}", async (Guid meetingId, MeetingWrite request, IMeetingService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.UpdateAsync(RequireUser(currentUser), meetingId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.MeetingsManage);

        meetings.MapDelete("/{meetingId:guid}", async (Guid meetingId, IMeetingService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(RequireUser(currentUser), meetingId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.MeetingsManage);

        var standups = api.MapGroup("/standups").WithTags("Standups");
        standups.MapGet("/", async (DateOnly date, Guid? teamId, IStandupService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.BoardAsync(RequireUser(currentUser), date, teamId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.MeetingsRead);

        standups.MapGet("/search", async (string? q, IStandupService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.SearchAsync(RequireUser(currentUser), q, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.MeetingsRead);

        standups.MapPost("/", async (StandupWrite request, IStandupService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.SaveAsync(RequireUser(currentUser), request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.MeetingsRead);

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }
}

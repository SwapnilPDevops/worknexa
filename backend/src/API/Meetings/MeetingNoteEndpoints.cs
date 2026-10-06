using Worknexa.Api.Contracts;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Meetings;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Meetings;

public static class MeetingNoteEndpoints
{
    public static RouteGroupBuilder MapWorknexaMeetingNotes(this RouteGroupBuilder api)
    {
        var notes = api.MapGroup("/meeting-notes").WithTags("Meeting notes");
        notes.MapGet("/", async (string? q, IMeetingNoteService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ListAsync(RequireUser(currentUser), q, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.MeetingsRead);

        notes.MapGet("/{meetingId:guid}", async (Guid meetingId, IMeetingNoteService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.GetAsync(RequireUser(currentUser), meetingId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.MeetingsRead);

        notes.MapPut("/{meetingId:guid}", async (Guid meetingId, MeetingNoteWrite request, IMeetingNoteService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.SaveAsync(RequireUser(currentUser), meetingId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.MeetingsRead);

        notes.MapPost("/{meetingId:guid}/actions/{actionId:guid}/task", async (Guid meetingId, Guid actionId, IMeetingNoteService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ConvertAsync(RequireUser(currentUser), meetingId, actionId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }
}

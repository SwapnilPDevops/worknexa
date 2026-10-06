using Worknexa.Api.Contracts;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Knowledge;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Knowledge;

public static class NoteEndpoints
{
    public static RouteGroupBuilder MapWorknexaNotes(this RouteGroupBuilder api)
    {
        var notes = api.MapGroup("/notes").WithTags("Notes");
        notes.MapGet("/options", async (INoteService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.OptionsAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.NotesRead);

        notes.MapGet("/", async (string? q, Guid? learningTopicId, bool? learningOnly, bool? favorites, INoteService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ListAsync(RequireUser(currentUser), q, learningTopicId, learningOnly == true, favorites == true, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.NotesRead);

        notes.MapGet("/{noteId:guid}", async (Guid noteId, INoteService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.GetAsync(RequireUser(currentUser), noteId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.NotesRead);

        notes.MapPost("/", async (NoteWrite request, INoteService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CreateAsync(RequireUser(currentUser), request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.NotesManage);

        notes.MapPut("/{noteId:guid}", async (Guid noteId, NoteWrite request, INoteService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.UpdateAsync(RequireUser(currentUser), noteId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.NotesManage);

        notes.MapDelete("/{noteId:guid}", async (Guid noteId, INoteService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(RequireUser(currentUser), noteId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.NotesManage);

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }
}

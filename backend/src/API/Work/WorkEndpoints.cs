using Worknexa.Api.Contracts;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Work;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Work;

public static class WorkEndpoints
{
    public static RouteGroupBuilder MapWorknexaWork(this RouteGroupBuilder api)
    {
        var tasks = api.MapGroup("/tasks").WithTags("Tasks");
        tasks.MapGet("/", async (
            string? view,
            string? status,
            string? priority,
            Guid? projectId,
            Guid? assigneeId,
            DateTimeOffset? dueFrom,
            DateTimeOffset? dueTo,
            string? tag,
            Guid? teamId,
            DateOnly? date,
            int? offsetMinutes,
            ITaskService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            var items = await service.ListAsync(
                RequireUser(currentUser),
                view,
                status,
                priority,
                projectId,
                assigneeId,
                dueFrom,
                dueTo,
                tag,
                teamId,
                date ?? DateOnly.FromDateTime(DateTime.UtcNow),
                offsetMinutes ?? 0,
                cancellationToken);
            return ApiResults.Success(items);
        }).RequireAuthorization(PermissionCatalog.TasksRead);

        tasks.MapGet("/{taskId:guid}", async (Guid taskId, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.GetAsync(RequireUser(currentUser), taskId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksRead);

        tasks.MapPost("/", async (TaskWriteRequest request, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CreateAsync(RequireUser(currentUser), request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        tasks.MapPut("/{taskId:guid}", async (Guid taskId, TaskWriteRequest request, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.UpdateAsync(RequireUser(currentUser), taskId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        tasks.MapPost("/{taskId:guid}/status", async (Guid taskId, StatusRequest request, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.SetStatusAsync(RequireUser(currentUser), taskId, request.Status, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        tasks.MapPost("/{taskId:guid}/complete", async (Guid taskId, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CompleteAsync(RequireUser(currentUser), taskId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        tasks.MapPost("/{taskId:guid}/reopen", async (Guid taskId, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ReopenAsync(RequireUser(currentUser), taskId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        tasks.MapDelete("/{taskId:guid}", async (Guid taskId, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(RequireUser(currentUser), taskId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.TasksManage);

        tasks.MapPut("/{taskId:guid}/assignee", async (Guid taskId, AssignRequest request, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.AssignAsync(RequireUser(currentUser), taskId, request.AssigneeId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        tasks.MapPost("/{taskId:guid}/comments", async (Guid taskId, CommentRequest request, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CommentAsync(RequireUser(currentUser), taskId, request.Body, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        tasks.MapPost("/{taskId:guid}/dependencies", async (Guid taskId, DependencyRequest request, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.AddDependencyAsync(RequireUser(currentUser), taskId, request.DependsOnTaskId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        tasks.MapDelete("/{taskId:guid}/dependencies/{dependencyId:guid}", async (Guid taskId, Guid dependencyId, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.RemoveDependencyAsync(RequireUser(currentUser), taskId, dependencyId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.TasksManage);

        tasks.MapPost("/{taskId:guid}/attachments", async (Guid taskId, IFormFile file, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            if (file.Length > 10 * 1024 * 1024)
            {
                throw new FluentValidation.ValidationException([new FluentValidation.Results.ValidationFailure("file", "Attachments must be 10 MB or smaller.")]);
            }

            await using var stream = file.OpenReadStream();
            var saved = await service.AddAttachmentAsync(RequireUser(currentUser), taskId, file.FileName, file.ContentType, stream, cancellationToken);
            return ApiResults.Success(saved);
        }).RequireAuthorization(PermissionCatalog.TasksManage).DisableAntiforgery();

        tasks.MapGet("/{taskId:guid}/attachments/{attachmentId:guid}", async (Guid taskId, Guid attachmentId, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            var stored = await service.OpenAttachmentAsync(RequireUser(currentUser), taskId, attachmentId, cancellationToken);
            return Results.File(stored.Content, stored.File.ContentType, stored.File.FileName);
        }).RequireAuthorization(PermissionCatalog.TasksRead);

        tasks.MapDelete("/{taskId:guid}/attachments/{attachmentId:guid}", async (Guid taskId, Guid attachmentId, ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.RemoveAttachmentAsync(RequireUser(currentUser), taskId, attachmentId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.TasksManage);

        var todos = api.MapGroup("/todos").WithTags("Todos");
        todos.MapGet("/", async (DateOnly? date, int? offsetMinutes, ITodoService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ListAsync(RequireUser(currentUser), date ?? DateOnly.FromDateTime(DateTime.UtcNow), offsetMinutes ?? 0, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksRead);

        todos.MapPost("/", async (TodoWriteRequest request, ITodoService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CreateAsync(RequireUser(currentUser), request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        todos.MapPut("/{todoId:guid}", async (Guid todoId, TodoWriteRequest request, ITodoService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.UpdateAsync(RequireUser(currentUser), todoId, request, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        todos.MapPost("/{todoId:guid}/complete", async (Guid todoId, ITodoService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CompleteAsync(RequireUser(currentUser), todoId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        todos.MapPost("/{todoId:guid}/reopen", async (Guid todoId, ITodoService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.ReopenAsync(RequireUser(currentUser), todoId, cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksManage);

        todos.MapDelete("/{todoId:guid}", async (Guid todoId, ITodoService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(RequireUser(currentUser), todoId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization(PermissionCatalog.TasksManage);

        api.MapGet("/work/options", async (ITaskService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.OptionsAsync(RequireUser(currentUser), cancellationToken)))
            .RequireAuthorization(PermissionCatalog.TasksRead);

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }
}

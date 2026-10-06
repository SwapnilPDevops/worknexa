using FluentValidation;
using Worknexa.Application.Common;
using Worknexa.Application.Dashboard;
using Worknexa.Domain.Enums;

namespace Worknexa.Application.Work;

public interface ITaskStore
{
    Task<IReadOnlyList<TaskListItem>> ListAsync(Guid userId, TaskFilter filter, CancellationToken cancellationToken);

    Task<TaskDetail?> FindAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);

    Task<WorkOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<TaskDetail> CreateAsync(Guid userId, TaskWriteRequest request, WorkTaskStatus status, WorkPriority priority, CancellationToken cancellationToken);

    Task<TaskDetail?> UpdateAsync(Guid userId, Guid taskId, TaskWriteRequest request, WorkTaskStatus status, WorkPriority priority, CancellationToken cancellationToken);

    Task<TaskDetail?> SetStatusAsync(Guid userId, Guid taskId, WorkTaskStatus status, DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);

    Task<TaskDetail?> AssignAsync(Guid userId, Guid taskId, Guid? assigneeId, CancellationToken cancellationToken);

    Task<TaskCommentItem?> CommentAsync(Guid userId, Guid taskId, string body, CancellationToken cancellationToken);

    Task<TaskDetail?> AddDependencyAsync(Guid userId, Guid taskId, Guid dependsOnTaskId, CancellationToken cancellationToken);

    Task<bool> RemoveDependencyAsync(Guid userId, Guid taskId, Guid dependencyId, CancellationToken cancellationToken);

    Task<TaskFile?> AddAttachmentAsync(Guid userId, Guid taskId, string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task<StoredAttachment?> OpenAttachmentAsync(Guid userId, Guid taskId, Guid attachmentId, CancellationToken cancellationToken);

    Task<bool> RemoveAttachmentAsync(Guid userId, Guid taskId, Guid attachmentId, CancellationToken cancellationToken);
}

public sealed record StoredAttachment(TaskFile File, Stream Content);

public interface ITodoStore
{
    Task<IReadOnlyList<TodoItemDto>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<TodoItemDto> CreateAsync(Guid userId, TodoDraft draft, CancellationToken cancellationToken);

    Task<TodoItemDto?> UpdateAsync(Guid userId, Guid todoId, TodoDraft draft, CancellationToken cancellationToken);

    Task<TodoItemDto?> CompleteAsync(Guid userId, Guid todoId, TodoDraft? next, CancellationToken cancellationToken);

    Task<TodoItemDto?> ReopenAsync(Guid userId, Guid todoId, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid todoId, CancellationToken cancellationToken);
}

public interface ITaskService
{
    Task<IReadOnlyList<TaskListItem>> ListAsync(Guid userId, string? view, string? status, string? priority, Guid? projectId, Guid? assigneeId, DateTimeOffset? dueFrom, DateTimeOffset? dueTo, string? tag, Guid? teamId, DateOnly date, int offsetMinutes, CancellationToken cancellationToken);

    Task<TaskDetail> GetAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);

    Task<WorkOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<TaskDetail> CreateAsync(Guid userId, TaskWriteRequest request, CancellationToken cancellationToken);

    Task<TaskDetail> UpdateAsync(Guid userId, Guid taskId, TaskWriteRequest request, CancellationToken cancellationToken);

    Task<TaskDetail> SetStatusAsync(Guid userId, Guid taskId, string status, CancellationToken cancellationToken);

    Task<TaskDetail> CompleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);

    Task<TaskDetail> ReopenAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);

    Task<TaskDetail> AssignAsync(Guid userId, Guid taskId, Guid? assigneeId, CancellationToken cancellationToken);

    Task<TaskCommentItem> CommentAsync(Guid userId, Guid taskId, string body, CancellationToken cancellationToken);

    Task<TaskDetail> AddDependencyAsync(Guid userId, Guid taskId, Guid dependsOnTaskId, CancellationToken cancellationToken);

    Task RemoveDependencyAsync(Guid userId, Guid taskId, Guid dependencyId, CancellationToken cancellationToken);

    Task<TaskFile> AddAttachmentAsync(Guid userId, Guid taskId, string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task<StoredAttachment> OpenAttachmentAsync(Guid userId, Guid taskId, Guid attachmentId, CancellationToken cancellationToken);

    Task RemoveAttachmentAsync(Guid userId, Guid taskId, Guid attachmentId, CancellationToken cancellationToken);
}

public sealed class TaskService(ITaskStore store) : ITaskService
{
    public Task<IReadOnlyList<TaskListItem>> ListAsync(
        Guid userId,
        string? view,
        string? status,
        string? priority,
        Guid? projectId,
        Guid? assigneeId,
        DateTimeOffset? dueFrom,
        DateTimeOffset? dueTo,
        string? tag,
        Guid? teamId,
        DateOnly date,
        int offsetMinutes,
        CancellationToken cancellationToken)
    {
        var window = Window(date, offsetMinutes);
        var selected = (view ?? "list").Trim().ToLowerInvariant();
        var filter = new TaskFilter(
            ParseOptional<WorkTaskStatus>(status, "status"),
            ParseOptional<WorkPriority>(priority, "priority"),
            projectId,
            selected == "mine" ? userId : assigneeId,
            selected == "calendar" ? dueFrom ?? window.DayStart : dueFrom,
            selected == "calendar" ? dueTo ?? window.DayStart.AddDays(DateTime.DaysInMonth(date.Year, date.Month)) : dueTo,
            string.IsNullOrWhiteSpace(tag) ? null : tag.Trim(),
            selected == "mine",
            selected == "team",
            selected == "overdue" ? window.DayStart : null,
            teamId);
        return store.ListAsync(userId, filter, cancellationToken);
    }

    public async Task<TaskDetail> GetAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        return await store.FindAsync(userId, taskId, cancellationToken) ?? throw new NotFoundException("Task was not found.");
    }

    public Task<WorkOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return store.OptionsAsync(userId, cancellationToken);
    }

    public Task<TaskDetail> CreateAsync(Guid userId, TaskWriteRequest request, CancellationToken cancellationToken)
    {
        var (status, priority) = ParseWrite(request);
        return store.CreateAsync(userId, request, status, priority, cancellationToken);
    }

    public async Task<TaskDetail> UpdateAsync(Guid userId, Guid taskId, TaskWriteRequest request, CancellationToken cancellationToken)
    {
        var (status, priority) = ParseWrite(request);
        return await store.UpdateAsync(userId, taskId, request, status, priority, cancellationToken) ?? throw new NotFoundException("Task was not found.");
    }

    public async Task<TaskDetail> SetStatusAsync(Guid userId, Guid taskId, string status, CancellationToken cancellationToken)
    {
        return await store.SetStatusAsync(userId, taskId, ParseRequired<WorkTaskStatus>(status, "status"), DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new NotFoundException("Task was not found.");
    }

    public Task<TaskDetail> CompleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        return SetStatusAsync(userId, taskId, nameof(WorkTaskStatus.Completed), cancellationToken);
    }

    public Task<TaskDetail> ReopenAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        return SetStatusAsync(userId, taskId, nameof(WorkTaskStatus.Todo), cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        if (!await store.DeleteAsync(userId, taskId, cancellationToken))
        {
            throw new NotFoundException("Task was not found.");
        }
    }

    public async Task<TaskDetail> AssignAsync(Guid userId, Guid taskId, Guid? assigneeId, CancellationToken cancellationToken)
    {
        return await store.AssignAsync(userId, taskId, assigneeId, cancellationToken) ?? throw new NotFoundException("Task was not found.");
    }

    public async Task<TaskCommentItem> CommentAsync(Guid userId, Guid taskId, string body, CancellationToken cancellationToken)
    {
        return await store.CommentAsync(userId, taskId, body.Trim(), cancellationToken) ?? throw new NotFoundException("Task was not found.");
    }

    public async Task<TaskDetail> AddDependencyAsync(Guid userId, Guid taskId, Guid dependsOnTaskId, CancellationToken cancellationToken)
    {
        return await store.AddDependencyAsync(userId, taskId, dependsOnTaskId, cancellationToken) ?? throw new NotFoundException("Task was not found.");
    }

    public async Task RemoveDependencyAsync(Guid userId, Guid taskId, Guid dependencyId, CancellationToken cancellationToken)
    {
        if (!await store.RemoveDependencyAsync(userId, taskId, dependencyId, cancellationToken))
        {
            throw new NotFoundException("Dependency was not found.");
        }
    }

    public async Task<TaskFile> AddAttachmentAsync(Guid userId, Guid taskId, string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        return await store.AddAttachmentAsync(userId, taskId, Path.GetFileName(fileName), string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType, content, cancellationToken)
            ?? throw new NotFoundException("Task was not found.");
    }

    public async Task<StoredAttachment> OpenAttachmentAsync(Guid userId, Guid taskId, Guid attachmentId, CancellationToken cancellationToken)
    {
        return await store.OpenAttachmentAsync(userId, taskId, attachmentId, cancellationToken) ?? throw new NotFoundException("Attachment was not found.");
    }

    public async Task RemoveAttachmentAsync(Guid userId, Guid taskId, Guid attachmentId, CancellationToken cancellationToken)
    {
        if (!await store.RemoveAttachmentAsync(userId, taskId, attachmentId, cancellationToken))
        {
            throw new NotFoundException("Attachment was not found.");
        }
    }

    private static (WorkTaskStatus Status, WorkPriority Priority) ParseWrite(TaskWriteRequest request)
    {
        if (request.StartAt is not null && request.DueAt is not null && request.StartAt > request.DueAt)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("startAt", "Start must be on or before the due date.")]);
        }

        return (ParseRequired<WorkTaskStatus>(request.Status, "status"), ParseRequired<WorkPriority>(request.Priority, "priority"));
    }

    internal static TEnum ParseRequired<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new ValidationException([new FluentValidation.Results.ValidationFailure(field, $"Choose a valid {field}.")]);
    }

    private static TEnum? ParseOptional<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseRequired<TEnum>(value, field);
    }

    private static MyDayWindow Window(DateOnly date, int offsetMinutes)
    {
        if (offsetMinutes is < -840 or > 840)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("offsetMinutes", "Offset is out of range.")]);
        }

        return MyDayWindow.Create(date, offsetMinutes);
    }
}

public sealed class TaskWriteRequestValidator : AbstractValidator<TaskWriteRequest>
{
    public TaskWriteRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Description).MaximumLength(8000);
        RuleFor(request => request.Status).NotEmpty();
        RuleFor(request => request.Priority).NotEmpty();
        RuleFor(request => request.EstimatedHours).InclusiveBetween(0, 10000);
        RuleFor(request => request.ActualHours).InclusiveBetween(0, 10000);
    }
}

public sealed class StatusRequestValidator : AbstractValidator<StatusRequest>
{
    public StatusRequestValidator()
    {
        RuleFor(request => request.Status).NotEmpty();
    }
}

public sealed class CommentRequestValidator : AbstractValidator<CommentRequest>
{
    public CommentRequestValidator()
    {
        RuleFor(request => request.Body).NotEmpty().MaximumLength(4000);
    }
}

public interface ITodoService
{
    Task<IReadOnlyList<TodoItemDto>> ListAsync(Guid userId, DateOnly date, int offsetMinutes, CancellationToken cancellationToken);

    Task<TodoItemDto> CreateAsync(Guid userId, TodoWriteRequest request, CancellationToken cancellationToken);

    Task<TodoItemDto> UpdateAsync(Guid userId, Guid todoId, TodoWriteRequest request, CancellationToken cancellationToken);

    Task<TodoItemDto> CompleteAsync(Guid userId, Guid todoId, CancellationToken cancellationToken);

    Task<TodoItemDto> ReopenAsync(Guid userId, Guid todoId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid todoId, CancellationToken cancellationToken);
}

public sealed class TodoService(ITodoStore store) : ITodoService
{
    public async Task<IReadOnlyList<TodoItemDto>> ListAsync(Guid userId, DateOnly date, int offsetMinutes, CancellationToken cancellationToken)
    {
        var window = Window(date, offsetMinutes);
        var items = await store.ListAsync(userId, cancellationToken);
        return items
            .Select(item => item with { Bucket = DueSchedule.Bucket(item.DueAt, item.IsCompleted, window).ToString() })
            .OrderBy(item => item.DueAt)
            .ToArray();
    }

    public Task<TodoItemDto> CreateAsync(Guid userId, TodoWriteRequest request, CancellationToken cancellationToken)
    {
        return store.CreateAsync(userId, Draft(request), cancellationToken);
    }

    public async Task<TodoItemDto> UpdateAsync(Guid userId, Guid todoId, TodoWriteRequest request, CancellationToken cancellationToken)
    {
        return await store.UpdateAsync(userId, todoId, Draft(request), cancellationToken) ?? throw new NotFoundException("Todo was not found.");
    }

    public async Task<TodoItemDto> CompleteAsync(Guid userId, Guid todoId, CancellationToken cancellationToken)
    {
        var current = (await store.ListAsync(userId, cancellationToken)).FirstOrDefault(item => item.Id == todoId)
            ?? throw new NotFoundException("Todo was not found.");
        TodoDraft? next = null;
        if (Enum.TryParse<Recurrence>(current.Recurrence, out var recurrence) && DueSchedule.CanAdvance(recurrence) && current.DueAt is not null)
        {
            var nextDue = DueSchedule.Next(current.DueAt.Value, recurrence);
            DateTimeOffset? remindAt = current.RemindAt is null ? null : nextDue - (current.DueAt.Value - current.RemindAt.Value);
            next = new TodoDraft(current.Title, Enum.Parse<WorkPriority>(current.Priority), nextDue, remindAt, recurrence, current.Tags);
        }

        return await store.CompleteAsync(userId, todoId, next, cancellationToken) ?? throw new NotFoundException("Todo was not found.");
    }

    public async Task<TodoItemDto> ReopenAsync(Guid userId, Guid todoId, CancellationToken cancellationToken)
    {
        return await store.ReopenAsync(userId, todoId, cancellationToken) ?? throw new NotFoundException("Todo was not found.");
    }

    public async Task DeleteAsync(Guid userId, Guid todoId, CancellationToken cancellationToken)
    {
        if (!await store.DeleteAsync(userId, todoId, cancellationToken))
        {
            throw new NotFoundException("Todo was not found.");
        }
    }

    private static TodoDraft Draft(TodoWriteRequest request)
    {
        return new TodoDraft(
            request.Title.Trim(),
            TaskService.ParseRequired<WorkPriority>(request.Priority, "priority"),
            request.DueAt,
            request.RemindAt,
            TaskService.ParseRequired<Recurrence>(request.Recurrence, "recurrence"),
            TagNames.Normalize(request.Tags));
    }

    private static MyDayWindow Window(DateOnly date, int offsetMinutes)
    {
        if (offsetMinutes is < -840 or > 840)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("offsetMinutes", "Offset is out of range.")]);
        }

        return MyDayWindow.Create(date, offsetMinutes);
    }
}

public sealed class TodoWriteRequestValidator : AbstractValidator<TodoWriteRequest>
{
    public TodoWriteRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Priority).NotEmpty();
        RuleFor(request => request.Recurrence).NotEmpty();
    }
}

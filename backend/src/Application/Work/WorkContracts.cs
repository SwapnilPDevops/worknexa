using Worknexa.Domain.Enums;

namespace Worknexa.Application.Work;

public sealed record NamedRef(Guid Id, string Name);

public sealed record WorkOptions(IReadOnlyList<NamedRef> Projects, IReadOnlyList<NamedRef> People, IReadOnlyList<NamedRef> Teams, IReadOnlyList<string> Tags);

public sealed record TaskListItem(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid? ProjectId,
    string? ProjectName,
    Guid? ParentTaskId,
    Guid? TeamId,
    decimal? EstimatedHours,
    decimal? ActualHours,
    IReadOnlyList<string> Tags);

public sealed record TaskChild(Guid Id, string Title, string Status);

public sealed record TaskLink(Guid Id, Guid DependsOnTaskId, string Title, string Status);

public sealed record TaskCommentItem(Guid Id, string Body, string AuthorName, DateTimeOffset CreatedAt);

public sealed record TaskFile(Guid Id, string FileName, string ContentType, long SizeBytes);

public sealed record TaskDetail(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid? ProjectId,
    string? ProjectName,
    Guid? ParentTaskId,
    string? ParentTitle,
    Guid? TeamId,
    decimal? EstimatedHours,
    decimal? ActualHours,
    IReadOnlyList<string> Tags,
    IReadOnlyList<TaskChild> Subtasks,
    IReadOnlyList<TaskLink> Dependencies,
    IReadOnlyList<TaskCommentItem> Comments,
    IReadOnlyList<TaskFile> Attachments,
    Guid? MeetingId,
    string? MeetingTitle);

public sealed record TaskWriteRequest(
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt,
    Guid? AssigneeId,
    Guid? ProjectId,
    Guid? ParentTaskId,
    Guid? TeamId,
    decimal? EstimatedHours,
    decimal? ActualHours,
    IReadOnlyList<string>? Tags);

public sealed record StatusRequest(string Status);

public sealed record AssignRequest(Guid? AssigneeId);

public sealed record CommentRequest(string Body);

public sealed record DependencyRequest(Guid DependsOnTaskId);

public sealed record TaskFilter(
    WorkTaskStatus? Status,
    WorkPriority? Priority,
    Guid? ProjectId,
    Guid? AssigneeId,
    DateTimeOffset? DueFrom,
    DateTimeOffset? DueTo,
    string? Tag,
    bool Mine,
    bool Team,
    DateTimeOffset? OverdueBefore,
    Guid? TeamId = null);

public sealed record TodoItemDto(
    Guid Id,
    string Title,
    bool IsCompleted,
    string Priority,
    string Recurrence,
    DateTimeOffset? DueAt,
    DateTimeOffset? RemindAt,
    string Bucket,
    IReadOnlyList<string> Tags);

public sealed record TodoWriteRequest(
    string Title,
    string Priority,
    DateTimeOffset? DueAt,
    DateTimeOffset? RemindAt,
    string Recurrence,
    IReadOnlyList<string>? Tags);

public sealed record TodoDraft(
    string Title,
    WorkPriority Priority,
    DateTimeOffset? DueAt,
    DateTimeOffset? RemindAt,
    Recurrence Recurrence,
    IReadOnlyList<string> Tags);

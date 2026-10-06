using FluentValidation;
using Worknexa.Application.Common;
using Worknexa.Application.Validation;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;

namespace Worknexa.Application.Projects;

public sealed record ProjectListItem(
    Guid Id,
    string Name,
    string Code,
    string Status,
    string Priority,
    Guid OwnerId,
    string OwnerName,
    Guid? TeamId,
    string? TeamName,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    int Progress,
    int OpenTasks);

public sealed record ProjectMemberItem(Guid UserId, string Name, string Role);

public sealed record ProjectDeadline(Guid TaskId, string Title, DateTimeOffset DueAt, string? AssigneeName);

public sealed record ProjectWorkload(Guid? AssigneeId, string Name, int OpenTasks, int BlockedTasks);

public sealed record ProjectDashboard(
    int Progress,
    int OpenTasks,
    int CompletedTasks,
    int BlockedTasks,
    IReadOnlyList<ProjectDeadline> UpcomingDeadlines,
    IReadOnlyList<ProjectWorkload> TeamWorkload);

public sealed record ProjectDetail(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    Guid OwnerId,
    string OwnerName,
    Guid? TeamId,
    string? TeamName,
    string Status,
    string Priority,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    string? Technology,
    string? RepositoryUrl,
    string? DocumentationUrl,
    IReadOnlyList<ProjectMemberItem> Members,
    ProjectDashboard Dashboard);

public sealed record ProjectWrite(
    string Name,
    string Code,
    string? Description,
    Guid? OwnerId,
    Guid? TeamId,
    string Status,
    string Priority,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    string? Technology,
    string? RepositoryUrl,
    string? DocumentationUrl);

public sealed record ProjectMemberWrite(Guid UserId, string Role);

public sealed record ProjectActivityItem(DateTimeOffset At, string Action, string Summary, string ActorName);

public sealed record ProjectMeetingItem(Guid Id, string Title, string MeetingType, DateTimeOffset StartAt);

public sealed record ProjectNoteItem(Guid Id, string Title, DateTimeOffset UpdatedAt, bool IsPinned);

public sealed record ProjectOptions(IReadOnlyList<NamedRef> People, IReadOnlyList<NamedRef> Teams);

public sealed record ProjectTaskRow(
    Guid Id,
    string Title,
    WorkTaskStatus Status,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTimeOffset? DueAt,
    DateTimeOffset UpdatedAt);

public interface IProjectStore
{
    Task<IReadOnlyList<ProjectListItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken);

    Task<ProjectOptions> OptionsAsync(CancellationToken cancellationToken);

    Task<ProjectDetail?> FindAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectTaskRow>> TasksAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<ProjectDetail> CreateAsync(Guid userId, ProjectDraft draft, CancellationToken cancellationToken);

    Task<ProjectDetail?> UpdateAsync(Guid userId, Guid projectId, ProjectDraft draft, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<ProjectDetail?> AddMemberAsync(Guid userId, Guid projectId, Guid memberId, ProjectMemberRole role, CancellationToken cancellationToken);

    Task<ProjectDetail?> RemoveMemberAsync(Guid userId, Guid projectId, Guid memberId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectActivityItem>?> ActivityAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectMeetingItem>?> MeetingsAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectNoteItem>?> NotesAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
}

public sealed record ProjectDraft(
    string Name,
    string Code,
    string? Description,
    Guid OwnerId,
    Guid? TeamId,
    ProjectStatus Status,
    WorkPriority Priority,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    string? Technology,
    string? RepositoryUrl,
    string? DocumentationUrl);

public interface IProjectService
{
    Task<IReadOnlyList<ProjectListItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken);

    Task<ProjectOptions> OptionsAsync(CancellationToken cancellationToken);

    Task<ProjectDetail> GetAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<ProjectDetail> CreateAsync(Guid userId, ProjectWrite request, CancellationToken cancellationToken);

    Task<ProjectDetail> UpdateAsync(Guid userId, Guid projectId, ProjectWrite request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<ProjectDetail> AddMemberAsync(Guid userId, Guid projectId, ProjectMemberWrite request, CancellationToken cancellationToken);

    Task<ProjectDetail> RemoveMemberAsync(Guid userId, Guid projectId, Guid memberId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectActivityItem>> ActivityAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectMeetingItem>> MeetingsAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectNoteItem>> NotesAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);
}

public sealed class ProjectService(IProjectStore store) : IProjectService
{
    public Task<IReadOnlyList<ProjectListItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        return store.ListAsync(userId, string.IsNullOrWhiteSpace(query) ? null : query.Trim(), cancellationToken);
    }

    public Task<ProjectOptions> OptionsAsync(CancellationToken cancellationToken)
    {
        return store.OptionsAsync(cancellationToken);
    }

    public async Task<ProjectDetail> GetAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        return await store.FindAsync(userId, projectId, cancellationToken) ?? throw new NotFoundException("Project was not found.");
    }

    public Task<ProjectDetail> CreateAsync(Guid userId, ProjectWrite request, CancellationToken cancellationToken)
    {
        return store.CreateAsync(userId, Draft(userId, request), cancellationToken);
    }

    public async Task<ProjectDetail> UpdateAsync(Guid userId, Guid projectId, ProjectWrite request, CancellationToken cancellationToken)
    {
        return await store.UpdateAsync(userId, projectId, Draft(userId, request), cancellationToken) ?? throw new NotFoundException("Project was not found.");
    }

    public async Task DeleteAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        if (!await store.DeleteAsync(userId, projectId, cancellationToken))
        {
            throw new NotFoundException("Project was not found.");
        }
    }

    public async Task<ProjectDetail> AddMemberAsync(Guid userId, Guid projectId, ProjectMemberWrite request, CancellationToken cancellationToken)
    {
        var role = TaskService.ParseRequired<ProjectMemberRole>(request.Role, "role");
        return await store.AddMemberAsync(userId, projectId, request.UserId, role, cancellationToken) ?? throw new NotFoundException("Project was not found.");
    }

    public async Task<ProjectDetail> RemoveMemberAsync(Guid userId, Guid projectId, Guid memberId, CancellationToken cancellationToken)
    {
        return await store.RemoveMemberAsync(userId, projectId, memberId, cancellationToken) ?? throw new NotFoundException("Project was not found.");
    }

    public async Task<IReadOnlyList<ProjectActivityItem>> ActivityAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        return await store.ActivityAsync(userId, projectId, cancellationToken) ?? throw new NotFoundException("Project was not found.");
    }

    public async Task<IReadOnlyList<ProjectMeetingItem>> MeetingsAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        return await store.MeetingsAsync(userId, projectId, cancellationToken) ?? throw new NotFoundException("Project was not found.");
    }

    public async Task<IReadOnlyList<ProjectNoteItem>> NotesAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        return await store.NotesAsync(userId, projectId, cancellationToken) ?? throw new NotFoundException("Project was not found.");
    }

    private static ProjectDraft Draft(Guid userId, ProjectWrite request)
    {
        if (request.StartDate is not null && request.TargetDate is not null && request.StartDate > request.TargetDate)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("targetDate", "Target date must be on or after the start date.")]);
        }

        return new ProjectDraft(
            request.Name.Trim(),
            request.Code.Trim().ToUpperInvariant(),
            Blank(request.Description),
            request.OwnerId ?? userId,
            request.TeamId,
            TaskService.ParseRequired<ProjectStatus>(request.Status, "status"),
            TaskService.ParseRequired<WorkPriority>(request.Priority, "priority"),
            request.StartDate,
            request.TargetDate,
            Blank(request.Technology),
            Url(request.RepositoryUrl, "repositoryUrl"),
            Url(request.DocumentationUrl, "documentationUrl"));
    }

    private static string? Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? Url(string? value, string field)
    {
        var trimmed = Blank(value);
        if (trimmed is null)
        {
            return null;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(field, "Use an http or https URL.")]);
        }

        return trimmed;
    }
}

public static class ProjectMetrics
{
    public static int Percent(int completed, int counted)
    {
        return counted == 0 ? 0 : (int)Math.Round(completed * 100d / counted);
    }

    public static ProjectDashboard Build(IReadOnlyList<ProjectTaskRow> tasks, DateTimeOffset now)
    {
        var counted = tasks.Count(task => task.Status != WorkTaskStatus.Cancelled);
        var completed = tasks.Count(task => task.Status == WorkTaskStatus.Completed);
        var blocked = tasks.Count(task => task.Status == WorkTaskStatus.Blocked);
        var open = tasks.Count(task => task.Status != WorkTaskStatus.Completed && task.Status != WorkTaskStatus.Cancelled);
        var horizon = now.AddDays(14);
        var deadlines = tasks
            .Where(task => task.DueAt is not null && task.DueAt >= now && task.DueAt <= horizon && task.Status != WorkTaskStatus.Completed && task.Status != WorkTaskStatus.Cancelled)
            .OrderBy(task => task.DueAt)
            .Take(8)
            .Select(task => new ProjectDeadline(task.Id, task.Title, task.DueAt!.Value, task.AssigneeName))
            .ToArray();
        var workload = tasks
            .Where(task => task.Status != WorkTaskStatus.Completed && task.Status != WorkTaskStatus.Cancelled)
            .GroupBy(task => task.AssigneeId)
            .Select(group => new ProjectWorkload(
                group.Key,
                group.First().AssigneeName ?? "Unassigned",
                group.Count(),
                group.Count(task => task.Status == WorkTaskStatus.Blocked)))
            .OrderByDescending(row => row.OpenTasks)
            .ThenBy(row => row.Name)
            .ToArray();
        return new ProjectDashboard(Percent(completed, counted), open, completed, blocked, deadlines, workload);
    }
}

public sealed class ProjectWriteValidator : AbstractValidator<ProjectWrite>
{
    public ProjectWriteValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Code).NotEmpty().MaximumLength(32);
        RuleFor(request => request.Description).MaximumLength(8000);
        RuleFor(request => request.Status).NotEmpty();
        RuleFor(request => request.Priority).NotEmpty();
        RuleFor(request => request.Technology).MaximumLength(120);
        RuleFor(request => request.RepositoryUrl).MaximumLength(500).WebUrl();
        RuleFor(request => request.DocumentationUrl).MaximumLength(500).WebUrl();
    }
}

public sealed class ProjectMemberWriteValidator : AbstractValidator<ProjectMemberWrite>
{
    public ProjectMemberWriteValidator()
    {
        RuleFor(request => request.UserId).NotEmpty();
        RuleFor(request => request.Role).NotEmpty();
    }
}

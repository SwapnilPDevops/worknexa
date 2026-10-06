using FluentValidation;
using Worknexa.Application.Common;
using Worknexa.Application.Validation;
using Worknexa.Domain.Enums;

namespace Worknexa.Application.Learning;

public sealed record LearningTopicItem(
    Guid Id,
    string Name,
    string? Category,
    string? Description,
    string Status,
    int Progress,
    string Priority,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    Guid? ParentId,
    string? ParentName,
    string? Goal,
    int ChildCount);

public sealed record LearningResourceItem(Guid Id, string Title, string? Url, string Type);

public sealed record LearningTopicDetail(
    Guid Id,
    string Name,
    string? Category,
    string? Description,
    string Status,
    int Progress,
    string Priority,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    Guid? ParentId,
    string? ParentName,
    string? Goal,
    int ChildCount,
    IReadOnlyList<LearningResourceItem> Resources);

public sealed record LearningGoalItem(Guid TopicId, string Name, string Goal);

public sealed record LearningTargetItem(Guid TopicId, string Name, DateOnly TargetDate, int Progress);

public sealed record LearningDashboard(
    int Progress,
    IReadOnlyList<LearningTopicItem> CurrentTopics,
    IReadOnlyList<LearningGoalItem> Goals,
    IReadOnlyList<LearningTopicItem> RecentlyStudied,
    IReadOnlyList<LearningTargetItem> UpcomingTargets);

public sealed record RoadmapNode(
    Guid Id,
    string Name,
    string? Category,
    string Status,
    int Progress,
    string Priority,
    IReadOnlyList<RoadmapNode> Children);

public sealed record LearningTopicWrite(
    string Name,
    string? Category,
    string? Description,
    string Status,
    int? Progress,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    string Priority,
    Guid? ParentTopicId,
    string? Goal);

public sealed record LearningResourceWrite(string Title, string? Url, string Type);

public sealed record LearningDraft(
    string Name,
    string? Category,
    string? Description,
    LearningStatus Status,
    int Progress,
    DateOnly? StartDate,
    DateOnly? TargetDate,
    WorkPriority Priority,
    Guid? ParentTopicId,
    string? Goal);

public sealed record LearningResourceDraft(string Title, string? Url, LearningResourceType Type);

public interface ILearningStore
{
    Task<LearningDashboard> DashboardAsync(Guid userId, DateOnly today, CancellationToken cancellationToken);

    Task<IReadOnlyList<LearningTopicItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken);

    Task<LearningTopicDetail?> FindAsync(Guid userId, Guid topicId, CancellationToken cancellationToken);

    Task<LearningTopicDetail> CreateAsync(Guid userId, LearningDraft draft, CancellationToken cancellationToken);

    Task<LearningTopicDetail?> UpdateAsync(Guid userId, Guid topicId, LearningDraft draft, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid topicId, CancellationToken cancellationToken);

    Task<LearningTopicDetail?> AddResourceAsync(Guid userId, Guid topicId, LearningResourceDraft draft, CancellationToken cancellationToken);

    Task<bool> DeleteResourceAsync(Guid userId, Guid topicId, Guid resourceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoadmapNode>> RoadmapAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoadmapNode>> CreateExampleAsync(Guid userId, CancellationToken cancellationToken);
}

public interface ILearningService
{
    Task<LearningDashboard> DashboardAsync(Guid userId, DateOnly today, CancellationToken cancellationToken);

    Task<IReadOnlyList<LearningTopicItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken);

    Task<LearningTopicDetail> GetAsync(Guid userId, Guid topicId, CancellationToken cancellationToken);

    Task<LearningTopicDetail> CreateAsync(Guid userId, LearningTopicWrite request, CancellationToken cancellationToken);

    Task<LearningTopicDetail> UpdateAsync(Guid userId, Guid topicId, LearningTopicWrite request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid topicId, CancellationToken cancellationToken);

    Task<LearningTopicDetail> AddResourceAsync(Guid userId, Guid topicId, LearningResourceWrite request, CancellationToken cancellationToken);

    Task DeleteResourceAsync(Guid userId, Guid topicId, Guid resourceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoadmapNode>> RoadmapAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoadmapNode>> CreateExampleAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class LearningService(ILearningStore store) : ILearningService
{
    public Task<LearningDashboard> DashboardAsync(Guid userId, DateOnly today, CancellationToken cancellationToken)
    {
        return store.DashboardAsync(userId, today, cancellationToken);
    }

    public Task<IReadOnlyList<LearningTopicItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        var term = query?.Trim();
        return store.ListAsync(userId, string.IsNullOrEmpty(term) ? null : term, cancellationToken);
    }

    public async Task<LearningTopicDetail> GetAsync(Guid userId, Guid topicId, CancellationToken cancellationToken)
    {
        return await store.FindAsync(userId, topicId, cancellationToken) ?? throw new NotFoundException("Learning topic was not found.");
    }

    public Task<LearningTopicDetail> CreateAsync(Guid userId, LearningTopicWrite request, CancellationToken cancellationToken)
    {
        return store.CreateAsync(userId, Normalize(request), cancellationToken);
    }

    public async Task<LearningTopicDetail> UpdateAsync(Guid userId, Guid topicId, LearningTopicWrite request, CancellationToken cancellationToken)
    {
        return await store.UpdateAsync(userId, topicId, Normalize(request), cancellationToken)
            ?? throw new NotFoundException("Learning topic was not found.");
    }

    public async Task DeleteAsync(Guid userId, Guid topicId, CancellationToken cancellationToken)
    {
        if (!await store.DeleteAsync(userId, topicId, cancellationToken))
        {
            throw new NotFoundException("Learning topic was not found.");
        }
    }

    public async Task<LearningTopicDetail> AddResourceAsync(Guid userId, Guid topicId, LearningResourceWrite request, CancellationToken cancellationToken)
    {
        return await store.AddResourceAsync(userId, topicId, NormalizeResource(request), cancellationToken)
            ?? throw new NotFoundException("Learning topic was not found.");
    }

    public async Task DeleteResourceAsync(Guid userId, Guid topicId, Guid resourceId, CancellationToken cancellationToken)
    {
        if (!await store.DeleteResourceAsync(userId, topicId, resourceId, cancellationToken))
        {
            throw new NotFoundException("Learning resource was not found.");
        }
    }

    public Task<IReadOnlyList<RoadmapNode>> RoadmapAsync(Guid userId, CancellationToken cancellationToken)
    {
        return store.RoadmapAsync(userId, cancellationToken);
    }

    public Task<IReadOnlyList<RoadmapNode>> CreateExampleAsync(Guid userId, CancellationToken cancellationToken)
    {
        return store.CreateExampleAsync(userId, cancellationToken);
    }

    private static LearningDraft Normalize(LearningTopicWrite request)
    {
        if (request.StartDate is DateOnly start && request.TargetDate is DateOnly target && start > target)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("startDate", "The start date must be on or before the target date.")]);
        }

        var status = Parse<LearningStatus>(request.Status, "status");
        var progress = request.Progress is int value ? Math.Clamp(value, 0, 100) : LearningProgress.Suggested(status);
        return new LearningDraft(
            request.Name.Trim(),
            Blank(request.Category),
            Blank(request.Description),
            status,
            progress,
            request.StartDate,
            request.TargetDate,
            Parse<WorkPriority>(request.Priority, "priority"),
            request.ParentTopicId,
            Blank(request.Goal));
    }

    private static LearningResourceDraft NormalizeResource(LearningResourceWrite request)
    {
        return new LearningResourceDraft(request.Title.Trim(), Url(request.Url), Parse<LearningResourceType>(request.Type, "type"));
    }

    private static TEnum Parse<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new ValidationException([new FluentValidation.Results.ValidationFailure(field, $"Choose a valid {field}.")]);
    }

    private static string? Blank(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string? Url(string? value)
    {
        var trimmed = Blank(value);
        if (trimmed is null)
        {
            return null;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("url", "Use an http or https URL.")]);
        }

        return trimmed;
    }
}

public sealed class LearningTopicWriteValidator : AbstractValidator<LearningTopicWrite>
{
    public LearningTopicWriteValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Category).MaximumLength(120);
        RuleFor(request => request.Description).MaximumLength(20_000);
        RuleFor(request => request.Goal).MaximumLength(500);
        RuleFor(request => request.Status).NotEmpty().MaximumLength(32);
        RuleFor(request => request.Priority).NotEmpty().MaximumLength(32);
        RuleFor(request => request.Progress).InclusiveBetween(0, 100).When(request => request.Progress is not null);
        RuleFor(request => request.TargetDate)
            .Must((request, target) => request.StartDate is null || target is null || request.StartDate <= target)
            .WithMessage("The start date must be on or before the target date.");
    }
}

public sealed class LearningResourceWriteValidator : AbstractValidator<LearningResourceWrite>
{
    public LearningResourceWriteValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Url).MaximumLength(2048).WebUrl();
        RuleFor(request => request.Type).NotEmpty().MaximumLength(32);
    }
}

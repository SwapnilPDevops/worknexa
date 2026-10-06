using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Common;
using Worknexa.Application.Learning;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Learning;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Learning;

public sealed class EfLearningStore(WorknexaDbContext context) : ILearningStore
{
    private static readonly (string Name, string[] Children)[] Example =
    [
        ("Frontend", ["JavaScript", "TypeScript", "React", "Next.js"]),
        ("Backend", ["C#", "ASP.NET Core", "Microservices"]),
        ("Cloud", ["Azure", "Docker", "Kubernetes", "Terraform"]),
        ("Architecture", ["System Design", "Distributed Systems"])
    ];

    public async Task<LearningDashboard> DashboardAsync(Guid userId, DateOnly today, CancellationToken cancellationToken)
    {
        var rows = await LoadAsync(userId, cancellationToken);
        var rolled = Roll(rows);
        var items = MapAll(rows, rolled);
        var byId = items.ToDictionary(item => item.Id);
        var studied = await RecentlyStudiedAsync(userId, rows, byId, cancellationToken);
        var current = rows
            .Where(row => row.Status is LearningStatus.Learning or LearningStatus.Practicing)
            .OrderByDescending(row => row.UpdatedAt)
            .Take(8)
            .Select(row => byId[row.Id])
            .ToList();
        var goals = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.Goal) && row.Status != LearningStatus.Completed)
            .OrderByDescending(row => row.UpdatedAt)
            .Take(8)
            .Select(row => new LearningGoalItem(row.Id, row.Title, row.Goal!.Trim()))
            .ToList();
        var targets = rows
            .Where(row => row.TargetDate is DateOnly date && date >= today && row.Status != LearningStatus.Completed)
            .OrderBy(row => row.TargetDate)
            .Take(8)
            .Select(row => new LearningTargetItem(row.Id, row.Title, row.TargetDate!.Value, rolled[row.Id]))
            .ToList();
        var inputs = rows.Select(row => new LearningProgressInput(row.Id, row.ParentTopicId, row.Progress)).ToList();
        return new LearningDashboard(LearningProgress.Overall(inputs, rolled), current, goals, studied, targets);
    }

    public async Task<IReadOnlyList<LearningTopicItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        var rows = await LoadAsync(userId, cancellationToken);
        var rolled = Roll(rows);
        var items = MapAll(rows, rolled);
        if (string.IsNullOrEmpty(query))
        {
            return items.OrderBy(item => item.Name).ToList();
        }

        return items
            .Where(item =>
                item.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (item.Category?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.Goal?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(item => item.Name)
            .ToList();
    }

    public async Task<LearningTopicDetail?> FindAsync(Guid userId, Guid topicId, CancellationToken cancellationToken)
    {
        var rows = await LoadAsync(userId, cancellationToken);
        var row = rows.FirstOrDefault(item => item.Id == topicId);
        if (row is null)
        {
            return null;
        }

        var rolled = Roll(rows);
        var item = MapAll(rows, rolled).First(topic => topic.Id == topicId);
        var resources = await context.LearningResources.AsNoTracking()
            .Where(resource => resource.LearningTopicId == topicId)
            .OrderBy(resource => resource.Title)
            .Select(resource => new LearningResourceItem(resource.Id, resource.Title, resource.Url, resource.ResourceType.ToString()))
            .ToListAsync(cancellationToken);
        return ToDetail(item, resources);
    }

    public async Task<LearningTopicDetail> CreateAsync(Guid userId, LearningDraft draft, CancellationToken cancellationToken)
    {
        var topic = new LearningTopic
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
        await EnsureParentAsync(userId, topic.Id, draft.ParentTopicId, cancellationToken);
        Apply(topic, draft);
        context.LearningTopics.Add(topic);
        await context.SaveChangesAsync(cancellationToken);
        return (await FindAsync(userId, topic.Id, cancellationToken))!;
    }

    public async Task<LearningTopicDetail?> UpdateAsync(Guid userId, Guid topicId, LearningDraft draft, CancellationToken cancellationToken)
    {
        var topic = await Owned(userId).FirstOrDefaultAsync(item => item.Id == topicId, cancellationToken);
        if (topic is null)
        {
            return null;
        }

        await EnsureParentAsync(userId, topic.Id, draft.ParentTopicId, cancellationToken);
        Apply(topic, draft);
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, topicId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid topicId, CancellationToken cancellationToken)
    {
        var topic = await Owned(userId).FirstOrDefaultAsync(item => item.Id == topicId, cancellationToken);
        if (topic is null)
        {
            return false;
        }

        if (await context.LearningTopics.AnyAsync(item => item.ParentTopicId == topicId, cancellationToken))
        {
            throw new ConflictException("Move or remove the child topics first.");
        }

        topic.DeletedAt = DateTimeOffset.UtcNow;
        topic.DeletedBy = userId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<LearningTopicDetail?> AddResourceAsync(Guid userId, Guid topicId, LearningResourceDraft draft, CancellationToken cancellationToken)
    {
        var topic = await Owned(userId).FirstOrDefaultAsync(item => item.Id == topicId, cancellationToken);
        if (topic is null)
        {
            return null;
        }

        var resource = new LearningResource
        {
            Id = Guid.NewGuid(),
            LearningTopicId = topic.Id,
            Title = draft.Title,
            Url = draft.Url,
            ResourceType = draft.Type
        };
        context.LearningResources.Add(resource);
        context.Entry(resource).State = EntityState.Added;
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, topicId, cancellationToken);
    }

    public async Task<bool> DeleteResourceAsync(Guid userId, Guid topicId, Guid resourceId, CancellationToken cancellationToken)
    {
        var resource = await context.LearningResources.FirstOrDefaultAsync(
            item => item.Id == resourceId && item.LearningTopicId == topicId && item.LearningTopic.UserId == userId,
            cancellationToken);
        if (resource is null)
        {
            return false;
        }

        resource.DeletedAt = DateTimeOffset.UtcNow;
        resource.DeletedBy = userId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<RoadmapNode>> RoadmapAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await LoadAsync(userId, cancellationToken);
        return Tree(rows, Roll(rows), null);
    }

    public async Task<IReadOnlyList<RoadmapNode>> CreateExampleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var existing = await context.LearningTopics
            .Where(topic => topic.UserId == userId)
            .ToListAsync(cancellationToken);
        foreach (var (name, children) in Example)
        {
            var parent = existing.FirstOrDefault(topic => topic.Title == name) ?? AddExample(userId, name, name, null, existing);
            foreach (var child in children)
            {
                if (existing.All(topic => topic.Title != child))
                {
                    AddExample(userId, child, name, parent.Id, existing);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return await RoadmapAsync(userId, cancellationToken);
    }

    private LearningTopic AddExample(Guid userId, string name, string category, Guid? parentId, List<LearningTopic> existing)
    {
        var topic = new LearningTopic
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = name,
            Category = category,
            Status = LearningStatus.NotStarted,
            Priority = WorkPriority.Medium,
            Progress = 0,
            ParentTopicId = parentId
        };
        context.LearningTopics.Add(topic);
        if (parentId is not null)
        {
            context.Entry(topic).State = EntityState.Added;
        }

        existing.Add(topic);
        return topic;
    }

    private async Task<List<LearningTopicItem>> RecentlyStudiedAsync(
        Guid userId,
        IReadOnlyList<TopicRow> rows,
        IReadOnlyDictionary<Guid, LearningTopicItem> byId,
        CancellationToken cancellationToken)
    {
        var stamps = await context.Notes.AsNoTracking()
            .Where(note => note.UserId == userId && note.LearningTopicId != null)
            .Select(note => new { note.LearningTopicId, note.UpdatedAt })
            .ToListAsync(cancellationToken);
        var latest = stamps
            .GroupBy(note => note.LearningTopicId!.Value)
            .ToDictionary(group => group.Key, group => group.Max(note => note.UpdatedAt));
        return rows
            .Select(row => (Item: byId[row.Id], When: latest.TryGetValue(row.Id, out var noteAt) && noteAt > row.UpdatedAt ? noteAt : row.UpdatedAt))
            .OrderByDescending(row => row.When)
            .Take(6)
            .Select(row => row.Item)
            .ToList();
    }

    private async Task EnsureParentAsync(Guid userId, Guid topicId, Guid? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return;
        }

        if (parentId == topicId)
        {
            throw new ConflictException("A topic cannot be placed inside itself.");
        }

        var cursor = parentId;
        var guard = 0;
        while (cursor is Guid current && guard++ < 32)
        {
            if (current == topicId)
            {
                throw new ConflictException("A topic cannot be placed inside itself.");
            }

            var parent = await context.LearningTopics.AsNoTracking()
                .Where(topic => topic.Id == current && topic.UserId == userId)
                .Select(topic => new { topic.ParentTopicId })
                .FirstOrDefaultAsync(cancellationToken);
            if (parent is null)
            {
                throw new ConflictException("Choose a parent topic you own.");
            }

            cursor = parent.ParentTopicId;
        }
    }

    private IQueryable<LearningTopic> Owned(Guid userId)
    {
        return context.LearningTopics.Where(topic => topic.UserId == userId);
    }

    private async Task<List<TopicRow>> LoadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await context.LearningTopics.AsNoTracking()
            .Where(topic => topic.UserId == userId)
            .Select(topic => new
            {
                topic.Id,
                topic.Title,
                topic.Category,
                topic.Description,
                topic.Status,
                topic.Progress,
                topic.Priority,
                topic.StartDate,
                topic.TargetDate,
                topic.ParentTopicId,
                topic.Goal,
                topic.UpdatedAt
            })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new TopicRow(
            row.Id,
            row.Title,
            row.Category,
            row.Description,
            row.Status,
            row.Progress,
            row.Priority,
            row.StartDate,
            row.TargetDate,
            row.ParentTopicId,
            row.Goal,
            row.UpdatedAt)).ToList();
    }

    private static IReadOnlyDictionary<Guid, int> Roll(IReadOnlyList<TopicRow> rows)
    {
        return LearningProgress.Rollup(rows.Select(row => new LearningProgressInput(row.Id, row.ParentTopicId, row.Progress)).ToList());
    }

    private static List<LearningTopicItem> MapAll(IReadOnlyList<TopicRow> rows, IReadOnlyDictionary<Guid, int> rolled)
    {
        var byId = rows.ToDictionary(row => row.Id);
        var childCounts = rows.Where(row => row.ParentTopicId is Guid parentId && byId.ContainsKey(parentId))
            .GroupBy(row => row.ParentTopicId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());
        return rows.Select(row => new LearningTopicItem(
            row.Id,
            row.Title,
            row.Category,
            row.Description,
            row.Status.ToString(),
            rolled[row.Id],
            row.Priority.ToString(),
            row.StartDate,
            row.TargetDate,
            row.ParentTopicId,
            row.ParentTopicId is Guid parentId && byId.TryGetValue(parentId, out var parent) ? parent.Title : null,
            row.Goal,
            childCounts.GetValueOrDefault(row.Id))).ToList();
    }

    private static List<RoadmapNode> Tree(IReadOnlyList<TopicRow> rows, IReadOnlyDictionary<Guid, int> rolled, Guid? parentId)
    {
        return rows
            .Where(row => row.ParentTopicId == parentId)
            .OrderBy(row => row.Title)
            .Select(row => new RoadmapNode(
                row.Id,
                row.Title,
                row.Category,
                row.Status.ToString(),
                rolled[row.Id],
                row.Priority.ToString(),
                Tree(rows, rolled, row.Id)))
            .ToList();
    }

    private static LearningTopicDetail ToDetail(LearningTopicItem item, IReadOnlyList<LearningResourceItem> resources)
    {
        return new LearningTopicDetail(
            item.Id,
            item.Name,
            item.Category,
            item.Description,
            item.Status,
            item.Progress,
            item.Priority,
            item.StartDate,
            item.TargetDate,
            item.ParentId,
            item.ParentName,
            item.Goal,
            item.ChildCount,
            resources);
    }

    private static void Apply(LearningTopic topic, LearningDraft draft)
    {
        topic.Title = draft.Name;
        topic.Category = draft.Category;
        topic.Description = draft.Description;
        topic.Status = draft.Status;
        topic.Progress = draft.Progress;
        topic.Priority = draft.Priority;
        topic.StartDate = draft.StartDate;
        topic.TargetDate = draft.TargetDate;
        topic.ParentTopicId = draft.ParentTopicId;
        topic.Goal = draft.Goal;
    }

    private sealed record TopicRow(
        Guid Id,
        string Title,
        string? Category,
        string? Description,
        LearningStatus Status,
        int Progress,
        WorkPriority Priority,
        DateOnly? StartDate,
        DateOnly? TargetDate,
        Guid? ParentTopicId,
        string? Goal,
        DateTimeOffset UpdatedAt);
}

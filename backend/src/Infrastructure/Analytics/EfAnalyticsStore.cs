using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Analytics;
using Worknexa.Application.Projects;
using Worknexa.Domain.Enums;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Analytics;

public sealed class EfAnalyticsStore(WorknexaDbContext context) : IAnalyticsStore
{
    public async Task<IReadOnlyList<AnalyticsTaskRow>> AssignedTasksAsync(Guid userId, DateTimeOffset from, CancellationToken cancellationToken)
    {
        var start = from.ToUniversalTime();
        return await context.Tasks.AsNoTracking()
            .Where(task => task.AssigneeId == userId && task.Status != WorkTaskStatus.Cancelled)
            .Where(task =>
                task.Status != WorkTaskStatus.Completed
                || (task.CompletedAt != null && task.CompletedAt >= start)
                || (task.DueAt != null && task.DueAt >= start))
            .Select(task => new AnalyticsTaskRow(task.Status, task.DueAt, task.CompletedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AnalyticsProjectTask>> ProjectTasksAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken)
    {
        if (projectIds.Count == 0)
        {
            return [];
        }

        return await context.Tasks.AsNoTracking()
            .Where(task => task.ProjectId != null && projectIds.Contains(task.ProjectId.Value))
            .Select(task => new AnalyticsProjectTask(
                task.ProjectId!.Value,
                new ProjectTaskRow(
                    task.Id,
                    task.Title,
                    task.Status,
                    task.AssigneeId,
                    task.Assignee != null ? task.Assignee.DisplayName : null,
                    task.DueAt,
                    task.UpdatedAt)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DateTimeOffset>> LearningActivityAsync(Guid userId, DateTimeOffset from, CancellationToken cancellationToken)
    {
        var start = from.ToUniversalTime();
        var notes = await context.Notes.AsNoTracking()
            .Where(note => note.UserId == userId && note.LearningTopicId != null && (note.CreatedAt >= start || note.UpdatedAt >= start))
            .Select(note => new { note.CreatedAt, note.UpdatedAt })
            .ToListAsync(cancellationToken);
        var topics = await context.LearningTopics.AsNoTracking()
            .Where(topic => topic.UserId == userId && (topic.CreatedAt >= start || topic.UpdatedAt >= start))
            .Select(topic => new { topic.CreatedAt, topic.UpdatedAt })
            .ToListAsync(cancellationToken);
        return notes.SelectMany(note => new[] { note.CreatedAt, note.UpdatedAt })
            .Concat(topics.SelectMany(topic => new[] { topic.CreatedAt, topic.UpdatedAt }))
            .ToArray();
    }
}

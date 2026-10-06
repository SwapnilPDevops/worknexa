using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Platform;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Platform;

public sealed class EfActivityStore(WorknexaDbContext context) : IActivityStore
{
    public async Task<IReadOnlyList<ActivityItem>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var logs = await context.ActivityLogs.AsNoTracking()
            .Where(log =>
                log.CreatedBy == userId
                || (log.EntityName == "Member" && log.EntityId == userId)
                || (log.EntityName == "Task" && context.Tasks.Any(task => task.Id == log.EntityId && (task.AssigneeId == userId || task.CreatedBy == userId)))
                || (log.EntityName == "Project" && context.Projects.Any(project => project.Id == log.EntityId && (project.OwnerId == userId || project.Members.Any(member => member.UserId == userId))))
                || (log.EntityName == "Team" && context.TeamMembers.Any(member => member.TeamId == log.EntityId && member.UserId == userId))
                || (log.EntityName == "Meeting" && context.Meetings.Any(meeting => meeting.Id == log.EntityId && (meeting.OrganizerId == userId || meeting.Participants.Any(person => person.UserId == userId))))
                || (log.EntityName == "LearningTopic" && context.LearningTopics.Any(topic => topic.Id == log.EntityId && topic.UserId == userId)))
            .OrderByDescending(log => log.CreatedAt)
            .Take(80)
            .Select(log => new
            {
                log.Id,
                log.CreatedBy,
                log.Action,
                log.EntityName,
                log.EntityId,
                log.CreatedAt,
                log.Metadata,
                log.Summary
            })
            .ToListAsync(cancellationToken);
        var actorIds = logs.Where(log => log.CreatedBy != null).Select(log => log.CreatedBy!.Value).Distinct().ToArray();
        var names = await context.Users.AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);
        return logs.Select(log => new ActivityItem(
            log.Id,
            log.CreatedBy,
            log.CreatedBy is Guid actorId && names.TryGetValue(actorId, out var name) ? name : "Someone",
            log.Action,
            log.EntityName,
            log.EntityId,
            log.CreatedAt,
            log.Metadata,
            log.Summary)).ToArray();
    }
}

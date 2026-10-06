using Microsoft.EntityFrameworkCore;
using Worknexa.Domain.Meetings;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Meetings;

internal sealed record ActionChange(
    Guid? Id,
    string Title,
    string? Description,
    Guid? AssigneeId,
    DateTimeOffset? DueAt,
    bool IsCompleted,
    bool SetDescription);

internal static class MeetingActionSync
{
    public static async Task ApplyAsync(WorknexaDbContext context, Meeting meeting, Guid userId, IReadOnlyList<ActionChange> incoming, CancellationToken cancellationToken)
    {
        var kept = new HashSet<Guid>();
        foreach (var item in incoming)
        {
            var match = item.Id is Guid id ? meeting.ActionItems.FirstOrDefault(action => action.Id == id) : null;
            if (match is null)
            {
                var action = new MeetingActionItem
                {
                    Id = Guid.NewGuid(),
                    MeetingId = meeting.Id,
                    Title = item.Title,
                    Description = Clean(item.Description),
                    AssigneeId = item.AssigneeId,
                    DueAt = item.DueAt,
                    IsCompleted = item.IsCompleted
                };
                context.MeetingActionItems.Add(action);
                context.Entry(action).State = EntityState.Added;
                continue;
            }

            kept.Add(match.Id);
            match.Title = item.Title;
            match.AssigneeId = item.AssigneeId;
            match.DueAt = item.DueAt;
            match.IsCompleted = item.IsCompleted;
            if (item.SetDescription)
            {
                match.Description = Clean(item.Description);
            }
        }

        var removed = meeting.ActionItems.Where(action => !kept.Contains(action.Id)).Select(action => action.Id).ToArray();
        foreach (var action in meeting.ActionItems.Where(action => removed.Contains(action.Id)).ToList())
        {
            context.Entry(action).State = EntityState.Detached;
        }

        if (removed.Length == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        await context.MeetingActionItems
            .Where(action => removed.Contains(action.Id))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(action => action.DeletedAt, now).SetProperty(action => action.DeletedBy, userId),
                cancellationToken);
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}

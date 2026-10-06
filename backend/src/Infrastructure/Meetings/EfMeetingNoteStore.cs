using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Common;
using Worknexa.Application.Meetings;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Meetings;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Meetings;

public sealed class EfMeetingNoteStore(WorknexaDbContext context, ITaskService tasks) : IMeetingNoteStore
{
    public async Task<IReadOnlyList<MeetingNoteListItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        var meetings = Visible(userId);
        if (!string.IsNullOrEmpty(query))
        {
            meetings = meetings.Where(meeting =>
                meeting.Title.Contains(query)
                || (meeting.Agenda != null && meeting.Agenda.Contains(query))
                || (meeting.NotesText != null && meeting.NotesText.Contains(query))
                || (meeting.Decisions != null && meeting.Decisions.Contains(query))
                || (meeting.FollowUps != null && meeting.FollowUps.Contains(query))
                || meeting.ActionItems.Any(action => action.Title.Contains(query) || (action.Description != null && action.Description.Contains(query))));
        }

        return await meetings
            .OrderByDescending(meeting => meeting.StartAt)
            .Take(100)
            .Select(meeting => new MeetingNoteListItem(meeting.Id, meeting.Title, meeting.StartAt, meeting.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<MeetingNoteDocument?> FindAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await Visible(userId)
            .AsNoTracking()
            .Include(item => item.Project)
            .Include(item => item.ActionItems).ThenInclude(action => action.Assignee)
            .Include(item => item.ActionItems).ThenInclude(action => action.Task)
            .FirstOrDefaultAsync(item => item.Id == meetingId, cancellationToken);
        return meeting is null ? null : Map(meeting);
    }

    public async Task<MeetingNoteDocument?> SaveAsync(Guid userId, Guid meetingId, MeetingNoteWrite request, CancellationToken cancellationToken)
    {
        var meeting = await Visible(userId)
            .Include(item => item.ActionItems)
            .FirstOrDefaultAsync(item => item.Id == meetingId, cancellationToken);
        if (meeting is null)
        {
            return null;
        }

        meeting.Agenda = request.Agenda;
        meeting.Description = request.Agenda;
        meeting.NotesText = request.Discussion;
        meeting.Decisions = request.Decisions;
        meeting.FollowUps = request.FollowUps;
        meeting.FollowUpAt = request.FollowUpAt;
        await MeetingActionSync.ApplyAsync(
            context,
            meeting,
            userId,
            (request.Actions ?? []).Select(action => new ActionChange(action.Id, action.Title, action.Description, action.AssigneeId, action.DueAt, action.IsCompleted, true)).ToArray(),
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, meetingId, cancellationToken);
    }

    public async Task<MeetingNoteDocument?> ConvertAsync(Guid userId, Guid meetingId, Guid actionId, CancellationToken cancellationToken)
    {
        var meeting = await Visible(userId)
            .Include(item => item.ActionItems)
            .FirstOrDefaultAsync(item => item.Id == meetingId, cancellationToken);
        if (meeting is null)
        {
            return null;
        }

        var action = meeting.ActionItems.FirstOrDefault(item => item.Id == actionId);
        if (action is null)
        {
            return null;
        }

        if (action.TaskId is null)
        {
            var created = await tasks.CreateAsync(userId, new TaskWriteRequest(
                action.Title,
                string.IsNullOrWhiteSpace(action.Description) ? null : action.Description.Trim(),
                nameof(WorkTaskStatus.Todo),
                nameof(WorkPriority.Medium),
                null,
                action.DueAt,
                action.AssigneeId,
                meeting.ProjectId,
                null,
                meeting.TeamId,
                null,
                null,
                []), cancellationToken);
            var task = await context.Tasks.FirstAsync(item => item.Id == created.Id, cancellationToken);
            task.MeetingId = meeting.Id;
            action.TaskId = created.Id;
            await context.SaveChangesAsync(cancellationToken);
        }

        return await FindAsync(userId, meetingId, cancellationToken);
    }

    private IQueryable<Meeting> Visible(Guid userId)
    {
        return context.Meetings.Where(meeting =>
            meeting.OrganizerId == userId
            || meeting.Participants.Any(participant => participant.UserId == userId)
            || (meeting.TeamId != null && context.TeamMembers.Any(member => member.UserId == userId && member.TeamId == meeting.TeamId)));
    }

    private static MeetingNoteDocument Map(Meeting meeting)
    {
        return new MeetingNoteDocument(
            meeting.Id,
            meeting.Title,
            meeting.StartAt,
            meeting.ProjectId,
            meeting.Project?.Name,
            meeting.Agenda,
            meeting.NotesText,
            meeting.Decisions,
            meeting.FollowUps,
            meeting.FollowUpAt,
            meeting.ActionItems
                .OrderBy(action => action.IsCompleted)
                .ThenBy(action => action.Title)
                .Select(action => new MeetingNoteAction(
                    action.Id,
                    action.Title,
                    action.Description,
                    action.AssigneeId,
                    action.Assignee?.DisplayName,
                    action.DueAt,
                    action.IsCompleted,
                    action.TaskId,
                    action.Task?.Title))
                .ToArray(),
            meeting.UpdatedAt);
    }
}

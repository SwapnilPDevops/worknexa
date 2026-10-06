using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Common;
using Worknexa.Application.Meetings;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Meetings;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Meetings;

public sealed class EfMeetingStore(WorknexaDbContext context) : IMeetingStore
{
    public async Task<IReadOnlyList<MeetingListItem>> ListAsync(Guid userId, string view, DateOnly date, int offsetMinutes, CancellationToken cancellationToken)
    {
        var offset = TimeSpan.FromMinutes(offsetMinutes);
        var dayStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, offset);
        var dayEnd = dayStart.AddDays(1);
        var monthStart = new DateTimeOffset(date.Year, date.Month, 1, 0, 0, 0, offset);
        var query = Visible(userId);
        query = view switch
        {
            "today" => query.Where(meeting => meeting.StartAt < dayEnd && meeting.EndAt > dayStart),
            "past" => query.Where(meeting => meeting.EndAt <= dayStart),
            "calendar" => query.Where(meeting => meeting.StartAt < monthStart.AddMonths(1) && meeting.EndAt > monthStart),
            _ => query.Where(meeting => meeting.StartAt >= dayEnd)
        };

        var ordered = view == "past" ? query.OrderByDescending(meeting => meeting.EndAt) : query.OrderBy(meeting => meeting.StartAt);
        var rows = await ordered.Take(200)
            .Select(meeting => new MeetingRow(
                meeting.Id,
                meeting.Title,
                meeting.MeetingType.ToString(),
                meeting.StartAt,
                meeting.EndAt,
                meeting.Location,
                meeting.MeetingLink,
                meeting.ProjectId,
                meeting.Project != null ? meeting.Project.Name : null,
                meeting.TeamId,
                meeting.Team != null ? meeting.Team.Name : null,
                meeting.Participants.Select(participant => new NamedRef(participant.UserId, participant.User.DisplayName)).ToList()))
            .ToListAsync(cancellationToken);

        return rows.Select(row => new MeetingListItem(
            row.Id,
            row.Title,
            row.MeetingType,
            row.StartAt,
            row.EndAt,
            MeetingTime.Minutes(row.StartAt, row.EndAt),
            row.Location,
            row.MeetingLink,
            row.ProjectId,
            row.ProjectName,
            row.TeamId,
            row.TeamName,
            row.Participants)).ToArray();
    }

    public async Task<MeetingDetail?> FindAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await Visible(userId)
            .AsNoTracking()
            .Include(item => item.Project)
            .Include(item => item.Team)
            .Include(item => item.Participants).ThenInclude(participant => participant.User)
            .Include(item => item.ActionItems).ThenInclude(item => item.Assignee)
            .FirstOrDefaultAsync(item => item.Id == meetingId, cancellationToken);
        return meeting is null ? null : Map(meeting, meeting.OrganizerId == userId);
    }

    public async Task<MeetingDetail> CreateAsync(Guid userId, MeetingWrite request, MeetingType meetingType, DateTimeOffset end, CancellationToken cancellationToken)
    {
        await EnsureLinksAsync(request, cancellationToken);
        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Agenda,
            StartAt = request.StartAt,
            EndAt = end,
            Location = request.Location,
            MeetingLink = request.MeetingLink,
            MeetingType = meetingType,
            Agenda = request.Agenda,
            NotesText = request.Notes,
            Decisions = request.Decisions,
            FollowUpAt = request.FollowUpAt,
            OrganizerId = userId,
            ProjectId = request.ProjectId,
            TeamId = request.TeamId
        };
        AddParticipants(meeting, userId, request.ParticipantIds);
        AddActions(meeting, request.ActionItems);
        context.Meetings.Add(meeting);
        await context.SaveChangesAsync(cancellationToken);
        return (await FindAsync(userId, meeting.Id, cancellationToken))!;
    }

    public async Task<MeetingDetail?> UpdateAsync(Guid userId, Guid meetingId, MeetingWrite request, MeetingType meetingType, DateTimeOffset end, CancellationToken cancellationToken)
    {
        var meeting = await context.Meetings
            .Include(item => item.Participants)
            .Include(item => item.ActionItems)
            .FirstOrDefaultAsync(item => item.Id == meetingId && item.OrganizerId == userId, cancellationToken);
        if (meeting is null)
        {
            return null;
        }

        await EnsureLinksAsync(request, cancellationToken);
        meeting.Title = request.Title;
        meeting.Description = request.Agenda;
        meeting.StartAt = request.StartAt;
        meeting.EndAt = end;
        meeting.Location = request.Location;
        meeting.MeetingLink = request.MeetingLink;
        meeting.MeetingType = meetingType;
        meeting.Agenda = request.Agenda;
        meeting.NotesText = request.Notes;
        meeting.Decisions = request.Decisions;
        meeting.FollowUpAt = request.FollowUpAt;
        meeting.ProjectId = request.ProjectId;
        meeting.TeamId = request.TeamId;
        SyncParticipants(meeting, userId, request.ParticipantIds);
        await ReplaceActionsAsync(meeting, userId, request.ActionItems, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, meetingId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await context.Meetings.FirstOrDefaultAsync(item => item.Id == meetingId && item.OrganizerId == userId, cancellationToken);
        if (meeting is null)
        {
            return false;
        }

        meeting.DeletedAt = DateTimeOffset.UtcNow;
        meeting.DeletedBy = userId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Meeting> Visible(Guid userId)
    {
        return context.Meetings.Where(meeting =>
            meeting.OrganizerId == userId
            || meeting.Participants.Any(participant => participant.UserId == userId)
            || (meeting.TeamId != null && context.TeamMembers.Any(member => member.UserId == userId && member.TeamId == meeting.TeamId)));
    }

    private async Task EnsureLinksAsync(MeetingWrite request, CancellationToken cancellationToken)
    {
        if (request.ProjectId is Guid projectId && !await context.Projects.AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            throw new ConflictException("Choose a project that exists.");
        }

        if (request.TeamId is Guid teamId && !await context.Teams.AnyAsync(team => team.Id == teamId, cancellationToken))
        {
            throw new ConflictException("Choose a team that exists.");
        }

        var people = (request.ParticipantIds ?? [])
            .Concat((request.ActionItems ?? []).Select(item => item.AssigneeId).Where(id => id.HasValue).Select(id => id!.Value))
            .Distinct()
            .ToArray();
        if (people.Length == 0)
        {
            return;
        }

        var found = await context.Users.CountAsync(user => people.Contains(user.Id) && user.IsActive, cancellationToken);
        if (found != people.Length)
        {
            throw new ConflictException("Choose people who are active in the workspace.");
        }
    }

    private void AddParticipants(Meeting meeting, Guid organizerId, IReadOnlyList<Guid>? participantIds)
    {
        foreach (var userId in (participantIds ?? []).Append(organizerId).Distinct())
        {
            context.MeetingParticipants.Add(new MeetingParticipant
            {
                Id = Guid.NewGuid(),
                MeetingId = meeting.Id,
                UserId = userId,
                ResponseStatus = userId == organizerId ? ParticipantResponse.Accepted : ParticipantResponse.Pending
            });
        }
    }

    private void SyncParticipants(Meeting meeting, Guid organizerId, IReadOnlyList<Guid>? participantIds)
    {
        var desired = (participantIds ?? []).Append(organizerId).Distinct().ToHashSet();
        foreach (var participant in meeting.Participants.Where(participant => !desired.Contains(participant.UserId)).ToList())
        {
            context.MeetingParticipants.Remove(participant);
        }

        foreach (var userId in desired.Where(id => meeting.Participants.All(participant => participant.UserId != id)))
        {
            var participant = new MeetingParticipant
            {
                Id = Guid.NewGuid(),
                MeetingId = meeting.Id,
                UserId = userId,
                ResponseStatus = userId == organizerId ? ParticipantResponse.Accepted : ParticipantResponse.Pending
            };
            context.MeetingParticipants.Add(participant);
            context.Entry(participant).State = EntityState.Added;
        }
    }

    private void AddActions(Meeting meeting, IReadOnlyList<MeetingActionWrite>? actionItems)
    {
        foreach (var item in actionItems ?? [])
        {
            var action = new MeetingActionItem
            {
                Id = Guid.NewGuid(),
                MeetingId = meeting.Id,
                Title = item.Title,
                Description = item.Description,
                AssigneeId = item.AssigneeId,
                DueAt = item.DueAt,
                IsCompleted = item.IsCompleted
            };
            context.MeetingActionItems.Add(action);
            context.Entry(action).State = EntityState.Added;
        }
    }

    private async Task ReplaceActionsAsync(Meeting meeting, Guid userId, IReadOnlyList<MeetingActionWrite>? actionItems, CancellationToken cancellationToken)
    {
        await MeetingActionSync.ApplyAsync(
            context,
            meeting,
            userId,
            (actionItems ?? []).Select(item => new ActionChange(item.Id, item.Title, item.Description, item.AssigneeId, item.DueAt, item.IsCompleted, item.Description is not null)).ToArray(),
            cancellationToken);
    }

    private static MeetingDetail Map(Meeting meeting, bool editable)
    {
        return new MeetingDetail(
            meeting.Id,
            meeting.Title,
            meeting.MeetingType.ToString(),
            meeting.StartAt,
            meeting.EndAt,
            MeetingTime.Minutes(meeting.StartAt, meeting.EndAt),
            meeting.Location,
            meeting.MeetingLink,
            meeting.Agenda,
            meeting.NotesText,
            meeting.Decisions,
            meeting.FollowUpAt,
            meeting.ProjectId,
            meeting.Project?.Name,
            meeting.TeamId,
            meeting.Team?.Name,
            meeting.Participants.Select(participant => new NamedRef(participant.UserId, participant.User.DisplayName)).ToArray(),
            meeting.ActionItems
                .OrderBy(item => item.IsCompleted)
                .ThenBy(item => item.Title)
                .Select(item => new MeetingActionItemDto(item.Id, item.Title, item.AssigneeId, item.Assignee?.DisplayName, item.IsCompleted, item.DueAt, item.Description, item.TaskId))
                .ToArray(),
            editable);
    }

    private sealed record MeetingRow(
        Guid Id,
        string Title,
        string MeetingType,
        DateTimeOffset StartAt,
        DateTimeOffset EndAt,
        string? Location,
        string? MeetingLink,
        Guid? ProjectId,
        string? ProjectName,
        Guid? TeamId,
        string? TeamName,
        List<NamedRef> Participants);
}

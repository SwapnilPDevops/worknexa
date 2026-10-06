using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Common;
using Worknexa.Application.Meetings;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Meetings;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Meetings;

public sealed class EfStandupStore(WorknexaDbContext context) : IStandupStore
{
    public async Task<StandupBoard> BoardAsync(Guid userId, DateOnly date, Guid? teamId, CancellationToken cancellationToken)
    {
        if (teamId is Guid team)
        {
            await RequireMembershipAsync(userId, team, cancellationToken);
        }

        var standup = await LoadAsync(userId, teamId, date, cancellationToken);
        var meeting = await MeetingForAsync(userId, teamId, date, standup, cancellationToken);
        var people = await PeopleAsync(userId, teamId, standup, cancellationToken);
        return new StandupBoard(date, standup?.Id, teamId, standup?.Team?.Name ?? await TeamNameAsync(teamId, cancellationToken), meeting.Id, meeting.Title, Lines(userId, people, standup));
    }

    public async Task<StandupBoard> SaveAsync(Guid userId, StandupWrite request, StandupStatus status, CancellationToken cancellationToken)
    {
        if (request.TeamId is Guid teamId)
        {
            await RequireMembershipAsync(userId, teamId, cancellationToken);
        }

        var standup = await LoadAsync(userId, request.TeamId, request.Date, cancellationToken) ?? Create(userId, request.TeamId, request.Date);
        var created = false;
        var entry = standup.Entries.FirstOrDefault(item => item.UserId == userId);
        if (entry is null)
        {
            created = true;
            entry = new StandupEntry { Id = Guid.NewGuid(), UserId = userId, StandupId = standup.Id };
            context.StandupEntries.Add(entry);
        }

        entry.Yesterday = request.Yesterday;
        entry.Today = request.Today;
        entry.Blockers = request.Blockers;
        entry.Notes = request.Notes;
        entry.Status = status;
        if (created)
        {
            context.Entry(entry).State = EntityState.Added;
        }

        await context.SaveChangesAsync(cancellationToken);
        return await BoardAsync(userId, request.Date, request.TeamId, cancellationToken);
    }

    public async Task<IReadOnlyList<StandupHit>> SearchAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        var entries = context.StandupEntries.Where(entry =>
            entry.Standup.OwnerId == userId
            || (entry.Standup.TeamId != null && teamIds.Contains(entry.Standup.TeamId.Value)));
        if (!string.IsNullOrWhiteSpace(query))
        {
            entries = entries.Where(entry =>
                (entry.Yesterday != null && entry.Yesterday.Contains(query))
                || (entry.Today != null && entry.Today.Contains(query))
                || (entry.Blockers != null && entry.Blockers.Contains(query))
                || (entry.Notes != null && entry.Notes.Contains(query))
                || entry.User.DisplayName.Contains(query));
        }

        return await entries
            .OrderByDescending(entry => entry.Standup.StandupDate)
            .ThenBy(entry => entry.User.DisplayName)
            .Take(50)
            .Select(entry => new StandupHit(
                entry.Standup.StandupDate,
                entry.User.DisplayName,
                entry.Yesterday,
                entry.Today,
                entry.Blockers,
                entry.Notes,
                entry.Status.ToString(),
                entry.Standup.Team != null ? entry.Standup.Team.Name : null))
            .ToListAsync(cancellationToken);
    }

    public async Task LinkMeetingAsync(Guid userId, Guid? teamId, DateOnly date, Guid meetingId, string meetingTitle, CancellationToken cancellationToken)
    {
        var standup = await LoadAsync(userId, teamId, date, cancellationToken) ?? Create(userId, teamId, date);
        standup.MeetingId = meetingId;
        await context.SaveChangesAsync(cancellationToken);
        _ = meetingTitle;
    }

    private async Task RequireMembershipAsync(Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        var member = await context.TeamMembers.AnyAsync(item => item.TeamId == teamId && item.UserId == userId, cancellationToken);
        if (!member)
        {
            throw new NotFoundException("Choose a team you belong to.");
        }
    }

    private async Task<Standup?> LoadAsync(Guid userId, Guid? teamId, DateOnly date, CancellationToken cancellationToken)
    {
        return await context.Standups
            .Include(standup => standup.Team)
            .Include(standup => standup.Meeting)
            .Include(standup => standup.Entries).ThenInclude(entry => entry.User)
            .FirstOrDefaultAsync(standup => standup.StandupDate == date && (teamId == null ? standup.OwnerId == userId : standup.TeamId == teamId), cancellationToken);
    }

    private Standup Create(Guid userId, Guid? teamId, DateOnly date)
    {
        var standup = new Standup
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            OwnerId = teamId is null ? userId : null,
            StandupDate = date
        };
        context.Standups.Add(standup);
        return standup;
    }

    private async Task<string?> TeamNameAsync(Guid? teamId, CancellationToken cancellationToken)
    {
        return teamId is null ? null : await context.Teams.Where(team => team.Id == teamId).Select(team => team.Name).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<(Guid? Id, string? Title)> MeetingForAsync(Guid userId, Guid? teamId, DateOnly date, Standup? standup, CancellationToken cancellationToken)
    {
        if (standup?.Meeting is not null)
        {
            return (standup.Meeting.Id, standup.Meeting.Title);
        }

        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-1);
        var candidates = await context.Meetings.AsNoTracking()
            .Where(meeting => meeting.MeetingType == MeetingType.Standup && meeting.StartAt >= start && meeting.StartAt < start.AddDays(3))
            .Where(meeting => teamId == null ? meeting.TeamId == null && meeting.OrganizerId == userId : meeting.TeamId == teamId)
            .Select(meeting => new { meeting.Id, meeting.Title, meeting.StartAt })
            .ToListAsync(cancellationToken);
        var match = candidates.FirstOrDefault(meeting => DateOnly.FromDateTime(meeting.StartAt.DateTime) == date);
        return match is null ? (null, null) : (match.Id, match.Title);
    }

    private async Task<IReadOnlyList<(Guid UserId, string Name)>> PeopleAsync(Guid userId, Guid? teamId, Standup? standup, CancellationToken cancellationToken)
    {
        if (teamId is null)
        {
            var name = await context.Users.Where(user => user.Id == userId).Select(user => user.DisplayName).FirstAsync(cancellationToken);
            return [(userId, name)];
        }

        var members = await context.TeamMembers
            .Where(member => member.TeamId == teamId)
            .Select(member => new { member.UserId, member.User.DisplayName })
            .ToListAsync(cancellationToken);
        var people = members.Select(member => (member.UserId, member.DisplayName)).ToList();
        foreach (var entry in standup?.Entries ?? [])
        {
            if (people.All(person => person.UserId != entry.UserId))
            {
                people.Add((entry.UserId, entry.User.DisplayName));
            }
        }

        return people;
    }

    private static IReadOnlyList<StandupLine> Lines(Guid userId, IReadOnlyList<(Guid UserId, string Name)> people, Standup? standup)
    {
        return people
            .Select(person =>
            {
                var entry = standup?.Entries.FirstOrDefault(item => item.UserId == person.UserId);
                return new StandupLine(entry?.Id, person.UserId, person.Name, entry?.Yesterday, entry?.Today, entry?.Blockers, entry?.Notes, (entry?.Status ?? StandupStatus.Clear).ToString(), person.UserId == userId);
            })
            .OrderByDescending(line => line.Mine)
            .ThenBy(line => line.MemberName)
            .ToArray();
    }
}

using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Common;
using Worknexa.Application.Organization;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Organization;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Organization;

public sealed class EfTeamStore(WorknexaDbContext context) : ITeamStore
{
    public async Task<IReadOnlyList<TeamListItem>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (await VisibleTeams(userId, cancellationToken))
            .OrderBy(team => team.Name)
            .Select(team => new TeamListItem(team.Id, team.Name, team.Description, team.Members.Count))
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<TeamOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var teams = await ListAsync(userId, cancellationToken);
        var people = await (await VisiblePeople(userId, cancellationToken))
            .OrderBy(person => person.DisplayName)
            .Select(person => new NamedPerson(person.Id, person.DisplayName, person.Designation))
            .Take(200)
            .ToListAsync(cancellationToken);
        return new TeamOptions(teams, people);
    }

    public async Task<TeamDetail?> FindAsync(Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        var team = await (await VisibleTeams(userId, cancellationToken))
            .Where(item => item.Id == teamId)
            .Select(item => new { item.Id, item.Name, item.Description })
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null)
        {
            return null;
        }

        var members = await MemberRows(teamId, cancellationToken);
        return new TeamDetail(team.Id, team.Name, team.Description, members);
    }

    public async Task<TeamDetail> CreateAsync(Guid userId, TeamDraft draft, CancellationToken cancellationToken)
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = draft.Name,
            Description = draft.Description
        };
        context.Teams.Add(team);
        AddMember(team.Id, userId, TeamMemberRole.Owner);
        await context.SaveChangesAsync(cancellationToken);
        return (await FindAsync(userId, team.Id, cancellationToken))!;
    }

    public async Task<TeamDetail?> UpdateAsync(Guid userId, Guid teamId, TeamDraft draft, CancellationToken cancellationToken)
    {
        var team = await Editable(userId, teamId, cancellationToken);
        if (team is null)
        {
            return null;
        }

        team.Name = draft.Name;
        team.Description = draft.Description;
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, team.Id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        var team = await Editable(userId, teamId, cancellationToken);
        if (team is null)
        {
            return false;
        }

        team.DeletedAt = DateTimeOffset.UtcNow;
        team.DeletedBy = userId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TeamDetail?> AddMemberAsync(Guid userId, Guid teamId, Guid memberUserId, TeamMemberRole role, CancellationToken cancellationToken)
    {
        var team = await Editable(userId, teamId, cancellationToken);
        if (team is null)
        {
            return null;
        }

        var person = await context.Users.FirstOrDefaultAsync(item => item.Id == memberUserId, cancellationToken)
            ?? throw new NotFoundException("Choose a person from the directory.");
        var existing = await context.TeamMembers.FirstOrDefaultAsync(item => item.TeamId == teamId && item.UserId == memberUserId, cancellationToken);
        if (existing is null)
        {
            AddMember(teamId, memberUserId, role);
        }
        else
        {
            existing.Role = role;
        }

        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, team.Id, cancellationToken);
    }

    public async Task<TeamDetail?> RemoveMemberAsync(Guid userId, Guid teamId, Guid memberUserId, CancellationToken cancellationToken)
    {
        var team = await Editable(userId, teamId, cancellationToken);
        if (team is null)
        {
            return null;
        }

        var membership = await context.TeamMembers.FirstOrDefaultAsync(item => item.TeamId == teamId && item.UserId == memberUserId, cancellationToken);
        if (membership is null)
        {
            return await FindAsync(userId, team.Id, cancellationToken);
        }

        context.TeamMembers.Remove(membership);
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, team.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<MemberListItem>> MembersAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        var people = await VisiblePeople(userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.ToLowerInvariant();
            people = people.Where(person =>
                person.DisplayName.ToLower().Contains(term)
                || person.Email.ToLower().Contains(term)
                || (person.Designation != null && person.Designation.ToLower().Contains(term))
                || (person.Department != null && person.Department.ToLower().Contains(term)));
        }

        var rows = await people
            .OrderBy(person => person.DisplayName)
            .Select(person => new
            {
                person.Id,
                person.DisplayName,
                person.Email,
                person.PhotoUrl,
                person.Designation,
                person.Department,
                person.Status
            })
            .Take(100)
            .ToListAsync(cancellationToken);
        return rows
            .Select(person => new MemberListItem(person.Id, person.DisplayName, person.Email, person.PhotoUrl, person.Designation, person.Department, person.Status.ToString()))
            .ToArray();
    }

    public async Task<MemberProfile?> FindMemberAsync(Guid userId, Guid memberId, CancellationToken cancellationToken)
    {
        var person = await (await VisiblePeople(userId, cancellationToken))
            .Where(item => item.Id == memberId)
            .Select(item => new PersonRow(
                item.Id,
                item.DisplayName,
                item.Email,
                item.Phone,
                item.PhotoUrl,
                item.Designation,
                item.Department,
                item.JoinedOn,
                item.ManagerId,
                item.Manager != null ? item.Manager.DisplayName : null,
                item.Status,
                item.Notes))
            .FirstOrDefaultAsync(cancellationToken);
        if (person is null)
        {
            return null;
        }

        var competencies = await context.MemberCompetencies.AsNoTracking()
            .Where(item => item.UserId == memberId)
            .Select(item => new { item.Kind, item.Name })
            .ToListAsync(cancellationToken);
        var teams = await context.TeamMembers.AsNoTracking()
            .Where(item => item.UserId == memberId)
            .OrderBy(item => item.Team.Name)
            .Select(item => new { item.TeamId, item.Team.Name, item.Role })
            .ToListAsync(cancellationToken);
        return new MemberProfile(
            person.Id,
            person.Name,
            person.Email,
            person.Phone,
            person.PhotoUrl,
            person.Designation,
            person.Department,
            person.JoinedOn,
            person.ManagerId,
            person.ManagerName,
            person.Status.ToString(),
            person.Notes,
            competencies.Where(item => item.Kind == MemberCompetencyKind.Skill).Select(item => item.Name).OrderBy(name => name).ToArray(),
            competencies.Where(item => item.Kind == MemberCompetencyKind.Technology).Select(item => item.Name).OrderBy(name => name).ToArray(),
            teams.Select(item => new MemberTeamItem(item.TeamId, item.Name, item.Role.ToString())).ToArray());
    }

    public async Task<MemberProfile?> UpdateMemberAsync(Guid userId, Guid memberId, MemberDraft draft, CancellationToken cancellationToken)
    {
        if (!await CanAsync(userId, PermissionCatalog.TeamsManage, cancellationToken))
        {
            throw new ForbiddenException("You do not have permission to edit team members.");
        }

        var person = await (await VisiblePeople(userId, cancellationToken)).FirstOrDefaultAsync(item => item.Id == memberId, cancellationToken);
        if (person is null)
        {
            return null;
        }

        var emailTaken = await context.Users.AnyAsync(item => item.Id != memberId && item.Email == draft.Email, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException("That email is already in use.");
        }

        if (draft.ManagerId is Guid managerId && !await context.Users.AnyAsync(item => item.Id == managerId, cancellationToken))
        {
            throw new NotFoundException("Choose a manager who is in the directory.");
        }

        person.DisplayName = draft.Name;
        person.Email = draft.Email;
        person.Phone = draft.Phone;
        person.PhotoUrl = draft.PhotoUrl;
        person.Designation = draft.Designation;
        person.Department = draft.Department;
        person.JoinedOn = draft.JoinedOn;
        person.ManagerId = draft.ManagerId;
        person.Status = draft.Status;
        person.Notes = draft.Notes;
        await ReplaceCompetenciesAsync(memberId, draft, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return await FindMemberAsync(userId, memberId, cancellationToken);
    }

    public async Task<IReadOnlyList<MemberProjectItem>> ProjectsAsync(Guid userId, Guid memberId, CancellationToken cancellationToken)
    {
        if (await FindMemberAsync(userId, memberId, cancellationToken) is null)
        {
            throw new NotFoundException("Team member was not found.");
        }

        var rows = await context.Projects.AsNoTracking()
            .Where(project => project.OwnerId == memberId || project.Members.Any(member => member.UserId == memberId))
            .OrderBy(project => project.Name)
            .Select(project => new
            {
                project.Id,
                project.Name,
                project.Code,
                project.Status,
                project.OwnerId,
                Role = project.Members.Where(member => member.UserId == memberId).Select(member => (ProjectMemberRole?)member.Role).FirstOrDefault()
            })
            .Take(50)
            .ToListAsync(cancellationToken);
        return rows
            .Select(project => new MemberProjectItem(
                project.Id,
                project.Name,
                project.Code,
                project.Status.ToString(),
                project.OwnerId == memberId ? ProjectMemberRole.Owner.ToString() : (project.Role ?? ProjectMemberRole.Member).ToString()))
            .ToArray();
    }

    public async Task<IReadOnlyList<MemberStandupItem>> StandupsAsync(Guid userId, Guid memberId, CancellationToken cancellationToken)
    {
        if (await FindMemberAsync(userId, memberId, cancellationToken) is null)
        {
            throw new NotFoundException("Team member was not found.");
        }

        var rows = await context.StandupEntries.AsNoTracking()
            .Where(entry => entry.UserId == memberId)
            .OrderByDescending(entry => entry.Standup.StandupDate)
            .Select(entry => new
            {
                entry.Id,
                entry.Standup.StandupDate,
                TeamName = entry.Standup.Team != null ? entry.Standup.Team.Name : null,
                entry.Status,
                entry.Yesterday,
                entry.Today,
                entry.Blockers
            })
            .Take(30)
            .ToListAsync(cancellationToken);
        return rows
            .Select(entry => new MemberStandupItem(entry.Id, entry.StandupDate, entry.TeamName, entry.Status.ToString(), entry.Yesterday, entry.Today, entry.Blockers))
            .ToArray();
    }

    public async Task<IReadOnlyList<MemberActivityItem>> ActivityAsync(Guid userId, Guid memberId, CancellationToken cancellationToken)
    {
        if (await FindMemberAsync(userId, memberId, cancellationToken) is null)
        {
            throw new NotFoundException("Team member was not found.");
        }

        var logs = await context.ActivityLogs.AsNoTracking()
            .Where(entry => entry.EntityName == "Member" && entry.EntityId == memberId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Select(entry => new MemberActivityItem(entry.Action, entry.Summary, entry.CreatedAt))
            .Take(40)
            .ToListAsync(cancellationToken);
        var tasks = await context.Tasks.AsNoTracking()
            .Where(task => task.AssigneeId == memberId)
            .OrderByDescending(task => task.UpdatedAt)
            .Select(task => new { task.Title, task.Status, task.UpdatedAt })
            .Take(15)
            .ToListAsync(cancellationToken);
        return logs
            .Concat(tasks.Select(task => new MemberActivityItem("Task", $"{task.Title} is {task.Status}.", task.UpdatedAt)))
            .OrderByDescending(item => item.OccurredAt)
            .Take(40)
            .ToArray();
    }

    public async Task<IReadOnlyList<TeamWorkloadRow>?> WorkloadAsync(Guid userId, Guid teamId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var visible = await (await VisibleTeams(userId, cancellationToken)).AnyAsync(team => team.Id == teamId, cancellationToken);
        if (!visible)
        {
            return null;
        }

        var members = await context.TeamMembers.AsNoTracking()
            .Where(member => member.TeamId == teamId)
            .Select(member => new { member.UserId, member.User.DisplayName })
            .ToListAsync(cancellationToken);
        var tasks = await context.Tasks.AsNoTracking()
            .Where(task => task.TeamId == teamId && task.AssigneeId != null)
            .Select(task => new { task.AssigneeId, task.Status, task.DueAt })
            .ToListAsync(cancellationToken);
        return TeamWorkload.Build(
            members.Select(member => (member.UserId, member.DisplayName)).ToArray(),
            tasks.Select(task => (task.AssigneeId, task.Status, task.DueAt)).ToArray(),
            now);
    }

    private async Task ReplaceCompetenciesAsync(Guid memberId, MemberDraft draft, CancellationToken cancellationToken)
    {
        var wanted = draft.Skills.Select(name => (Kind: MemberCompetencyKind.Skill, Name: name))
            .Concat(draft.Technologies.Select(name => (Kind: MemberCompetencyKind.Technology, Name: name)))
            .ToArray();
        var existing = await context.MemberCompetencies.Where(item => item.UserId == memberId).ToListAsync(cancellationToken);
        foreach (var item in existing)
        {
            var match = wanted.FirstOrDefault(entry => entry.Kind == item.Kind && string.Equals(entry.Name, item.Name, StringComparison.OrdinalIgnoreCase));
            if (match.Name is null)
            {
                context.MemberCompetencies.Remove(item);
            }
            else
            {
                item.Name = match.Name;
            }
        }

        foreach (var entry in wanted)
        {
            if (existing.Any(item => item.Kind == entry.Kind && string.Equals(item.Name, entry.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var competency = new MemberCompetency
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                Kind = entry.Kind,
                Name = entry.Name
            };
            context.MemberCompetencies.Add(competency);
            context.Entry(competency).State = EntityState.Added;
        }
    }

    private async Task<IReadOnlyList<TeamMemberItem>> MemberRows(Guid teamId, CancellationToken cancellationToken)
    {
        var rows = await context.TeamMembers.AsNoTracking()
            .Where(member => member.TeamId == teamId)
            .OrderBy(member => member.User.DisplayName)
            .Select(member => new
            {
                member.UserId,
                member.User.DisplayName,
                member.User.Email,
                member.Role,
                member.User.Status,
                member.User.Designation
            })
            .ToListAsync(cancellationToken);
        return rows
            .Select(member => new TeamMemberItem(member.UserId, member.DisplayName, member.Email, member.Role.ToString(), member.Status.ToString(), member.Designation))
            .ToArray();
    }

    private void AddMember(Guid teamId, Guid memberUserId, TeamMemberRole role)
    {
        var member = new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = memberUserId,
            Role = role
        };
        context.TeamMembers.Add(member);
        context.Entry(member).State = EntityState.Added;
    }

    private async Task<Team?> Editable(Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        if (!await CanAsync(userId, PermissionCatalog.TeamsManage, cancellationToken))
        {
            throw new ForbiddenException("You do not have permission to manage teams.");
        }

        return await (await VisibleTeams(userId, cancellationToken)).FirstOrDefaultAsync(team => team.Id == teamId, cancellationToken);
    }

    private async Task<IQueryable<Team>> VisibleTeams(Guid userId, CancellationToken cancellationToken)
    {
        var teams = context.Teams.AsQueryable();
        if (await CanAsync(userId, PermissionCatalog.TeamsManage, cancellationToken))
        {
            return teams;
        }

        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        return teams.Where(team => teamIds.Contains(team.Id));
    }

    private async Task<IQueryable<User>> VisiblePeople(Guid userId, CancellationToken cancellationToken)
    {
        var people = context.Users.AsQueryable();
        if (await CanAsync(userId, PermissionCatalog.TeamsManage, cancellationToken))
        {
            return people;
        }

        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        return people.Where(person =>
            person.Id == userId
            || context.TeamMembers.Any(member => member.UserId == person.Id && teamIds.Contains(member.TeamId)));
    }

    private Task<bool> CanAsync(Guid userId, string permission, CancellationToken cancellationToken)
    {
        return context.UserRoles.AsNoTracking().AnyAsync(
            assignment => assignment.UserId == userId
                && assignment.Role.RolePermissions.Any(grant => grant.Permission.Key == permission),
            cancellationToken);
    }

    private sealed record PersonRow(
        Guid Id,
        string Name,
        string Email,
        string? Phone,
        string? PhotoUrl,
        string? Designation,
        string? Department,
        DateOnly? JoinedOn,
        Guid? ManagerId,
        string? ManagerName,
        MemberStatus Status,
        string? Notes);
}

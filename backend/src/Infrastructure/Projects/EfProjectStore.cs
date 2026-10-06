using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Common;
using Worknexa.Application.Projects;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Projects;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Projects;

public sealed class EfProjectStore(WorknexaDbContext context) : IProjectStore
{
    public async Task<IReadOnlyList<ProjectListItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        var projects = Visible(userId);
        if (!string.IsNullOrWhiteSpace(query))
        {
            projects = projects.Where(project =>
                project.Name.Contains(query)
                || project.Code.Contains(query)
                || (project.Technology != null && project.Technology.Contains(query)));
        }

        var rows = await projects
            .OrderBy(project => project.Name)
            .Take(100)
            .Select(project => new
            {
                project.Id,
                project.Name,
                project.Code,
                Status = project.Status.ToString(),
                Priority = project.Priority.ToString(),
                project.OwnerId,
                OwnerName = project.Owner != null ? project.Owner.DisplayName : "Someone",
                project.TeamId,
                TeamName = project.Team != null ? project.Team.Name : null,
                project.StartDate,
                project.EndDate,
                Completed = project.Tasks.Count(task => task.Status == WorkTaskStatus.Completed),
                Counted = project.Tasks.Count(task => task.Status != WorkTaskStatus.Cancelled),
                Open = project.Tasks.Count(task => task.Status != WorkTaskStatus.Completed && task.Status != WorkTaskStatus.Cancelled)
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new ProjectListItem(
            row.Id,
            row.Name,
            row.Code,
            row.Status,
            row.Priority,
            row.OwnerId,
            row.OwnerName,
            row.TeamId,
            row.TeamName,
            row.StartDate,
            row.EndDate,
            ProjectMetrics.Percent(row.Completed, row.Counted),
            row.Open)).ToArray();
    }

    public async Task<ProjectOptions> OptionsAsync(CancellationToken cancellationToken)
    {
        var people = await context.Users.AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .Select(user => new NamedRef(user.Id, user.DisplayName))
            .ToListAsync(cancellationToken);
        var teams = await context.Teams.AsNoTracking()
            .OrderBy(team => team.Name)
            .Select(team => new NamedRef(team.Id, team.Name))
            .ToListAsync(cancellationToken);
        return new ProjectOptions(people, teams);
    }

    public async Task<ProjectDetail?> FindAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        var row = await DetailRows(Visible(userId).Where(item => item.Id == projectId)).FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var tasks = await context.Tasks.AsNoTracking()
            .Where(task => task.ProjectId == projectId)
            .Select(task => new ProjectTaskRow(
                task.Id,
                task.Title,
                task.Status,
                task.AssigneeId,
                task.Assignee != null ? task.Assignee.DisplayName : null,
                task.DueAt,
                task.UpdatedAt))
            .ToListAsync(cancellationToken);
        return ToDetail(row, ProjectMetrics.Build(tasks, DateTimeOffset.UtcNow));
    }

    public async Task<IReadOnlyList<ProjectTaskRow>> TasksAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        if (!await Visible(userId).AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            return [];
        }

        return await context.Tasks.AsNoTracking()
            .Where(task => task.ProjectId == projectId)
            .OrderBy(task => task.DueAt)
            .ThenBy(task => task.Title)
            .Select(task => new ProjectTaskRow(
                task.Id,
                task.Title,
                task.Status,
                task.AssigneeId,
                task.Assignee != null ? task.Assignee.DisplayName : null,
                task.DueAt,
                task.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectDetail> CreateAsync(Guid userId, ProjectDraft draft, CancellationToken cancellationToken)
    {
        await EnsureReferencesAsync(draft, cancellationToken);
        var project = new Project { Id = Guid.NewGuid() };
        Apply(project, draft);
        context.Projects.Add(project);
        AddMember(project.Id, draft.OwnerId, ProjectMemberRole.Owner);
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, project.Id, cancellationToken) ?? throw new ConflictException("Project could not be saved.");
    }

    public async Task<ProjectDetail?> UpdateAsync(Guid userId, Guid projectId, ProjectDraft draft, CancellationToken cancellationToken)
    {
        var project = await Visible(userId).Include(item => item.Members).FirstOrDefaultAsync(item => item.Id == projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        await EnsureReferencesAsync(draft, projectId, cancellationToken);
        var previousOwner = project.OwnerId;
        Apply(project, draft);
        if (previousOwner != draft.OwnerId)
        {
            Demote(project, previousOwner);
            EnsureOwnerMember(project, draft.OwnerId);
        }

        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, projectId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        var project = await Visible(userId).FirstOrDefaultAsync(item => item.Id == projectId, cancellationToken);
        if (project is null)
        {
            return false;
        }

        project.DeletedAt = DateTimeOffset.UtcNow;
        project.DeletedBy = userId;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ProjectDetail?> AddMemberAsync(Guid userId, Guid projectId, Guid memberId, ProjectMemberRole role, CancellationToken cancellationToken)
    {
        var project = await Visible(userId).Include(item => item.Members).FirstOrDefaultAsync(item => item.Id == projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        if (!await context.Users.AnyAsync(user => user.Id == memberId && user.IsActive, cancellationToken))
        {
            throw new ConflictException("Choose an active person.");
        }

        if (role == ProjectMemberRole.Owner)
        {
            var previous = project.OwnerId;
            project.OwnerId = memberId;
            Demote(project, previous);
        }

        var existing = project.Members.FirstOrDefault(member => member.UserId == memberId);
        if (existing is null)
        {
            AddMember(project.Id, memberId, role);
        }
        else
        {
            existing.Role = role;
        }

        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, projectId, cancellationToken);
    }

    public async Task<ProjectDetail?> RemoveMemberAsync(Guid userId, Guid projectId, Guid memberId, CancellationToken cancellationToken)
    {
        var project = await Visible(userId).Include(item => item.Members).FirstOrDefaultAsync(item => item.Id == projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        if (memberId == project.OwnerId)
        {
            throw new ConflictException("Choose another owner before removing this person.");
        }

        var member = project.Members.FirstOrDefault(item => item.UserId == memberId);
        if (member is null)
        {
            throw new NotFoundException("That person is not on the project.");
        }

        context.ProjectMembers.Remove(member);
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectActivityItem>?> ActivityAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        if (!await Visible(userId).AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            return null;
        }

        var logs = await context.ActivityLogs.AsNoTracking()
            .Where(log => log.EntityName == "Project" && log.EntityId == projectId)
            .OrderByDescending(log => log.CreatedAt)
            .Take(40)
            .Select(log => new { log.CreatedAt, log.Action, log.Summary, log.CreatedBy })
            .ToListAsync(cancellationToken);
        var actorIds = logs.Where(log => log.CreatedBy != null).Select(log => log.CreatedBy!.Value).Distinct().ToArray();
        var actors = await context.Users.AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .Select(user => new { user.Id, user.DisplayName })
            .ToListAsync(cancellationToken);
        var actorNames = actors.ToDictionary(actor => actor.Id, actor => actor.DisplayName);
        var tasks = await context.Tasks.AsNoTracking()
            .Where(task => task.ProjectId == projectId)
            .OrderByDescending(task => task.UpdatedAt)
            .Take(20)
            .Select(task => new
            {
                task.UpdatedAt,
                task.Title,
                Status = task.Status.ToString(),
                Actor = task.Assignee != null ? task.Assignee.DisplayName : "Unassigned"
            })
            .ToListAsync(cancellationToken);
        var logItems = logs.Select(log => new ProjectActivityItem(
            log.CreatedAt,
            log.Action,
            log.Summary,
            log.CreatedBy is Guid actorId && actorNames.TryGetValue(actorId, out var name) ? name : "Someone"));
        var taskItems = tasks.Select(task => new ProjectActivityItem(task.UpdatedAt, "Task", $"{task.Title} is {task.Status}", task.Actor));
        return logItems.Concat(taskItems).OrderByDescending(item => item.At).Take(40).ToArray();
    }

    public async Task<IReadOnlyList<ProjectMeetingItem>?> MeetingsAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        if (!await Visible(userId).AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            return null;
        }

        return await context.Meetings.AsNoTracking()
            .Where(meeting => meeting.ProjectId == projectId)
            .OrderBy(meeting => meeting.StartAt)
            .Take(50)
            .Select(meeting => new ProjectMeetingItem(meeting.Id, meeting.Title, meeting.MeetingType.ToString(), meeting.StartAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectNoteItem>?> NotesAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        if (!await Visible(userId).AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            return null;
        }

        return await context.Notes.AsNoTracking()
            .Where(note => note.UserId == userId && note.ProjectId == projectId)
            .OrderByDescending(note => note.IsPinned)
            .ThenByDescending(note => note.UpdatedAt)
            .Take(50)
            .Select(note => new ProjectNoteItem(note.Id, note.Title, note.UpdatedAt, note.IsPinned))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Project> Visible(Guid userId)
    {
        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        return context.Projects.Where(project =>
            project.OwnerId == userId
            || project.CreatedBy == userId
            || project.Members.Any(member => member.UserId == userId)
            || (project.TeamId != null && teamIds.Contains(project.TeamId.Value)));
    }

    private sealed record DetailRow(
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
        IReadOnlyList<ProjectMemberItem> Members);

    private static IQueryable<DetailRow> DetailRows(IQueryable<Project> projects)
    {
        return projects.Select(project => new DetailRow(
            project.Id,
            project.Name,
            project.Code,
            project.Description,
            project.OwnerId,
            project.Owner != null ? project.Owner.DisplayName : "Someone",
            project.TeamId,
            project.Team != null ? project.Team.Name : null,
            project.Status.ToString(),
            project.Priority.ToString(),
            project.StartDate,
            project.EndDate,
            project.Technology,
            project.RepositoryUrl,
            project.DocumentationUrl,
            project.Members
                .OrderBy(member => member.User.DisplayName)
                .Select(member => new ProjectMemberItem(member.UserId, member.User.DisplayName, member.Role.ToString()))
                .ToList()));
    }

    private static ProjectDetail ToDetail(DetailRow row, ProjectDashboard dashboard)
    {
        return new ProjectDetail(
            row.Id,
            row.Name,
            row.Code,
            row.Description,
            row.OwnerId,
            row.OwnerName,
            row.TeamId,
            row.TeamName,
            row.Status,
            row.Priority,
            row.StartDate,
            row.TargetDate,
            row.Technology,
            row.RepositoryUrl,
            row.DocumentationUrl,
            row.Members,
            dashboard);
    }

    private async Task EnsureReferencesAsync(ProjectDraft draft, CancellationToken cancellationToken)
    {
        await EnsureReferencesAsync(draft, null, cancellationToken);
    }

    private async Task EnsureReferencesAsync(ProjectDraft draft, Guid? projectId, CancellationToken cancellationToken)
    {
        if (!await context.Users.AnyAsync(user => user.Id == draft.OwnerId && user.IsActive, cancellationToken))
        {
            throw new ConflictException("Choose an active owner.");
        }

        if (draft.TeamId is Guid teamId && !await context.Teams.AnyAsync(team => team.Id == teamId, cancellationToken))
        {
            throw new ConflictException("Choose a team.");
        }

        var codeTaken = projectId is Guid currentId
            ? await context.Projects.AnyAsync(project => project.Code == draft.Code && project.Id != currentId, cancellationToken)
            : await context.Projects.AnyAsync(project => project.Code == draft.Code, cancellationToken);
        if (codeTaken)
        {
            throw new ConflictException("That project code is already in use.");
        }
    }

    private static void Apply(Project project, ProjectDraft draft)
    {
        project.Name = draft.Name;
        project.Code = draft.Code;
        project.Description = draft.Description;
        project.OwnerId = draft.OwnerId;
        project.TeamId = draft.TeamId;
        project.Status = draft.Status;
        project.Priority = draft.Priority;
        project.StartDate = draft.StartDate;
        project.EndDate = draft.TargetDate;
        project.Technology = draft.Technology;
        project.RepositoryUrl = draft.RepositoryUrl;
        project.DocumentationUrl = draft.DocumentationUrl;
    }

    private void AddMember(Guid projectId, Guid memberId, ProjectMemberRole role)
    {
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = memberId,
            Role = role
        };
        context.ProjectMembers.Add(member);
        context.Entry(member).State = EntityState.Added;
    }

    private void EnsureOwnerMember(Project project, Guid ownerId)
    {
        var member = project.Members.FirstOrDefault(item => item.UserId == ownerId);
        if (member is null)
        {
            AddMember(project.Id, ownerId, ProjectMemberRole.Owner);
            return;
        }

        member.Role = ProjectMemberRole.Owner;
    }

    private static void Demote(Project project, Guid userId)
    {
        var member = project.Members.FirstOrDefault(item => item.UserId == userId && item.Role == ProjectMemberRole.Owner);
        if (member is not null)
        {
            member.Role = ProjectMemberRole.Member;
        }
    }

}

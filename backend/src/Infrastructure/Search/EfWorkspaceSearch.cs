using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Search;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Meetings;
using Worknexa.Domain.Projects;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Search;

public sealed class EfWorkspaceSearch(WorknexaDbContext context) : IWorkspaceSearch
{
    public async Task<IReadOnlyList<SearchGroup>> SearchAsync(Guid userId, SearchRequest request, CancellationToken cancellationToken)
    {
        var text = request.Query.Trim();
        if (text.Length == 0)
        {
            return [];
        }

        if (text.Length > 120)
        {
            text = text[..120];
        }

        var wanted = Wanted(request.Entities);
        if (request.Entities is { Count: > 0 } && wanted.Count == 0)
        {
            return [];
        }

        var limit = Math.Clamp(request.Limit, 1, 8);
        var pattern = Pattern(text);
        var granted = await PermissionsAsync(userId, cancellationToken);
        var groups = new List<SearchGroup>();
        foreach (var entity in SearchEntity.Order)
        {
            if (wanted.Count > 0 && !wanted.Contains(entity))
            {
                continue;
            }

            var permission = RequiredPermission(entity);
            if (permission is not null && !granted.Contains(permission))
            {
                continue;
            }

            var hits = await HitsAsync(entity, userId, granted, pattern, limit, cancellationToken);
            if (hits.Count > 0)
            {
                groups.Add(new SearchGroup(entity, SearchEntity.Label(entity), hits));
            }
        }

        return groups;
    }

    private async Task<IReadOnlyList<SearchHit>> HitsAsync(
        string entity,
        Guid userId,
        HashSet<string> granted,
        string pattern,
        int limit,
        CancellationToken cancellationToken)
    {
        return entity switch
        {
            SearchEntity.Project => await ProjectsAsync(userId, pattern, limit, cancellationToken),
            SearchEntity.Task => await TasksAsync(userId, pattern, limit, cancellationToken),
            SearchEntity.Meeting => await MeetingsAsync(userId, pattern, limit, cancellationToken),
            SearchEntity.Note => await NotesAsync(userId, pattern, limit, learning: false, cancellationToken),
            SearchEntity.Person => await PeopleAsync(userId, granted, pattern, limit, cancellationToken),
            SearchEntity.Todo => await TodosAsync(userId, pattern, limit, cancellationToken),
            SearchEntity.MeetingNote => await MeetingNotesAsync(userId, pattern, limit, cancellationToken),
            SearchEntity.Standup => await StandupsAsync(userId, pattern, limit, cancellationToken),
            SearchEntity.LearningTopic => await TopicsAsync(userId, pattern, limit, cancellationToken),
            SearchEntity.LearningNote => await NotesAsync(userId, pattern, limit, learning: true, cancellationToken),
            SearchEntity.CalendarEvent => await EventsAsync(userId, pattern, limit, cancellationToken),
            _ => []
        };
    }

    private async Task<IReadOnlyList<SearchHit>> ProjectsAsync(Guid userId, string pattern, int limit, CancellationToken cancellationToken)
    {
        var rows = await VisibleProjects(userId).AsNoTracking()
            .Where(project =>
                EF.Functions.ILike(project.Name, pattern)
                || EF.Functions.ILike(project.Code, pattern)
                || (project.Description != null && EF.Functions.ILike(project.Description, pattern))
                || (project.Technology != null && EF.Functions.ILike(project.Technology, pattern)))
            .OrderBy(project => project.Name)
            .Take(limit)
            .Select(project => new { project.Id, project.Name, project.Code })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new SearchHit(SearchEntity.Project, row.Id, row.Name, row.Code, $"/projects?project={row.Id}")).ToList();
    }

    private async Task<IReadOnlyList<SearchHit>> TasksAsync(Guid userId, string pattern, int limit, CancellationToken cancellationToken)
    {
        var rows = await VisibleTasks(userId).AsNoTracking()
            .Where(task =>
                EF.Functions.ILike(task.Title, pattern)
                || (task.Description != null && EF.Functions.ILike(task.Description, pattern)))
            .OrderBy(task => task.Title)
            .Take(limit)
            .Select(task => new { task.Id, task.Title, Status = task.Status.ToString() })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new SearchHit(SearchEntity.Task, row.Id, row.Title, row.Status, $"/tasks?task={row.Id}")).ToList();
    }

    private async Task<IReadOnlyList<SearchHit>> MeetingsAsync(Guid userId, string pattern, int limit, CancellationToken cancellationToken)
    {
        var rows = await VisibleMeetings(userId).AsNoTracking()
            .Where(meeting =>
                EF.Functions.ILike(meeting.Title, pattern)
                || (meeting.Description != null && EF.Functions.ILike(meeting.Description, pattern))
                || (meeting.Location != null && EF.Functions.ILike(meeting.Location, pattern)))
            .OrderByDescending(meeting => meeting.StartAt)
            .Take(limit)
            .Select(meeting => new { meeting.Id, meeting.Title, meeting.StartAt })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new SearchHit(SearchEntity.Meeting, row.Id, row.Title, row.StartAt.ToString("yyyy-MM-dd"), $"/meetings?meeting={row.Id}")).ToList();
    }

    private async Task<IReadOnlyList<SearchHit>> MeetingNotesAsync(Guid userId, string pattern, int limit, CancellationToken cancellationToken)
    {
        var rows = await VisibleMeetings(userId).AsNoTracking()
            .Where(meeting =>
                (meeting.Agenda != null && EF.Functions.ILike(meeting.Agenda, pattern))
                || (meeting.NotesText != null && EF.Functions.ILike(meeting.NotesText, pattern))
                || (meeting.Decisions != null && EF.Functions.ILike(meeting.Decisions, pattern))
                || (meeting.FollowUps != null && EF.Functions.ILike(meeting.FollowUps, pattern))
                || meeting.ActionItems.Any(action =>
                    EF.Functions.ILike(action.Title, pattern)
                    || (action.Description != null && EF.Functions.ILike(action.Description, pattern)))
                || meeting.Notes.Any(note => EF.Functions.ILike(note.Body, pattern)))
            .OrderByDescending(meeting => meeting.StartAt)
            .Take(limit)
            .Select(meeting => new { meeting.Id, meeting.Title, meeting.StartAt })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new SearchHit(SearchEntity.MeetingNote, row.Id, row.Title, row.StartAt.ToString("yyyy-MM-dd"), $"/meeting-notes?meeting={row.Id}")).ToList();
    }

    private async Task<IReadOnlyList<SearchHit>> NotesAsync(Guid userId, string pattern, int limit, bool learning, CancellationToken cancellationToken)
    {
        var entity = learning ? SearchEntity.LearningNote : SearchEntity.Note;
        var route = learning ? "/learning-notes?note=" : "/notes?note=";
        var notes = context.Notes.AsNoTracking().Where(note => note.UserId == userId);
        notes = learning ? notes.Where(note => note.LearningTopicId != null) : notes.Where(note => note.LearningTopicId == null);
        var rows = await notes
            .Where(note =>
                EF.Functions.ILike(note.Title, pattern)
                || EF.Functions.ILike(note.Body, pattern)
                || (note.Category != null && EF.Functions.ILike(note.Category, pattern))
                || note.NoteTags.Any(link => EF.Functions.ILike(link.Tag.Name, pattern)))
            .OrderByDescending(note => note.UpdatedAt)
            .Take(limit)
            .Select(note => new { note.Id, note.Title, note.Category })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new SearchHit(entity, row.Id, row.Title, row.Category, $"{route}{row.Id}")).ToList();
    }

    private async Task<IReadOnlyList<SearchHit>> PeopleAsync(Guid userId, HashSet<string> granted, string pattern, int limit, CancellationToken cancellationToken)
    {
        var rows = await VisiblePeople(userId, granted).AsNoTracking()
            .Where(person =>
                EF.Functions.ILike(person.DisplayName, pattern)
                || EF.Functions.ILike(person.Email, pattern)
                || (person.Designation != null && EF.Functions.ILike(person.Designation, pattern))
                || (person.Department != null && EF.Functions.ILike(person.Department, pattern))
                || (person.Notes != null && EF.Functions.ILike(person.Notes, pattern)))
            .OrderBy(person => person.DisplayName)
            .Take(limit)
            .Select(person => new { person.Id, person.DisplayName, person.Email, person.Designation })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new SearchHit(SearchEntity.Person, row.Id, row.DisplayName, row.Designation ?? row.Email, $"/team?member={row.Id}")).ToList();
    }

    private async Task<IReadOnlyList<SearchHit>> TodosAsync(Guid userId, string pattern, int limit, CancellationToken cancellationToken)
    {
        var rows = await context.TodoItems.AsNoTracking()
            .Where(item => item.UserId == userId && EF.Functions.ILike(item.Title, pattern))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Title)
            .Take(limit)
            .Select(item => new { item.Id, item.Title, item.IsCompleted })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new SearchHit(SearchEntity.Todo, row.Id, row.Title, row.IsCompleted ? "Completed" : "Open", "/todo")).ToList();
    }

    private async Task<IReadOnlyList<SearchHit>> StandupsAsync(Guid userId, string pattern, int limit, CancellationToken cancellationToken)
    {
        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        var rows = await context.StandupEntries.AsNoTracking()
            .Where(entry =>
                entry.Standup.OwnerId == userId
                || (entry.Standup.TeamId != null && teamIds.Contains(entry.Standup.TeamId.Value)))
            .Where(entry =>
                (entry.Yesterday != null && EF.Functions.ILike(entry.Yesterday, pattern))
                || (entry.Today != null && EF.Functions.ILike(entry.Today, pattern))
                || (entry.Blockers != null && EF.Functions.ILike(entry.Blockers, pattern))
                || (entry.Notes != null && EF.Functions.ILike(entry.Notes, pattern))
                || EF.Functions.ILike(entry.User.DisplayName, pattern))
            .OrderByDescending(entry => entry.Standup.StandupDate)
            .Take(limit)
            .Select(entry => new
            {
                entry.Id,
                entry.Standup.StandupDate,
                Name = entry.User.DisplayName,
                Team = entry.Standup.Team != null ? entry.Standup.Team.Name : null
            })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new SearchHit(
            SearchEntity.Standup,
            row.Id,
            $"{row.StandupDate:yyyy-MM-dd} · {row.Name}",
            row.Team,
            $"/standups?date={row.StandupDate:yyyy-MM-dd}")).ToList();
    }

    private async Task<IReadOnlyList<SearchHit>> TopicsAsync(Guid userId, string pattern, int limit, CancellationToken cancellationToken)
    {
        var rows = await context.LearningTopics.AsNoTracking()
            .Where(topic => topic.UserId == userId)
            .Where(topic =>
                EF.Functions.ILike(topic.Title, pattern)
                || (topic.Description != null && EF.Functions.ILike(topic.Description, pattern))
                || (topic.Category != null && EF.Functions.ILike(topic.Category, pattern))
                || (topic.Goal != null && EF.Functions.ILike(topic.Goal, pattern)))
            .OrderBy(topic => topic.Title)
            .Take(limit)
            .Select(topic => new { topic.Id, topic.Title, topic.Category })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new SearchHit(SearchEntity.LearningTopic, row.Id, row.Title, row.Category, $"/learning?topic={row.Id}")).ToList();
    }

    private async Task<IReadOnlyList<SearchHit>> EventsAsync(Guid userId, string pattern, int limit, CancellationToken cancellationToken)
    {
        var rows = await context.CalendarEvents.AsNoTracking()
            .Where(item => item.OrganizerId == userId || item.Participants.Any(participant => participant.UserId == userId))
            .Where(item =>
                EF.Functions.ILike(item.Title, pattern)
                || (item.Description != null && EF.Functions.ILike(item.Description, pattern))
                || (item.Location != null && EF.Functions.ILike(item.Location, pattern)))
            .OrderByDescending(item => item.StartAt)
            .Take(limit)
            .Select(item => new { item.Id, item.Title, item.StartAt })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new SearchHit(
            SearchEntity.CalendarEvent,
            row.Id,
            row.Title,
            row.StartAt.ToString("yyyy-MM-dd"),
            $"/calendar?event={row.Id}&date={row.StartAt:yyyy-MM-dd}")).ToList();
    }

    private IQueryable<Project> VisibleProjects(Guid userId)
    {
        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        return context.Projects.Where(project =>
            project.OwnerId == userId
            || project.CreatedBy == userId
            || project.Members.Any(member => member.UserId == userId)
            || (project.TeamId != null && teamIds.Contains(project.TeamId.Value)));
    }

    private IQueryable<Domain.Productivity.WorkTask> VisibleTasks(Guid userId)
    {
        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        var projectIds = context.ProjectMembers.Where(member => member.UserId == userId).Select(member => member.ProjectId);
        return context.Tasks.Where(task =>
            task.AssigneeId == userId
            || task.CreatedBy == userId
            || (task.TeamId != null && teamIds.Contains(task.TeamId.Value))
            || (task.ProjectId != null && projectIds.Contains(task.ProjectId.Value)));
    }

    private IQueryable<Meeting> VisibleMeetings(Guid userId)
    {
        return context.Meetings.Where(meeting =>
            meeting.OrganizerId == userId
            || meeting.Participants.Any(participant => participant.UserId == userId)
            || (meeting.TeamId != null && context.TeamMembers.Any(member => member.UserId == userId && member.TeamId == meeting.TeamId)));
    }

    private IQueryable<User> VisiblePeople(Guid userId, HashSet<string> granted)
    {
        var people = context.Users.AsQueryable();
        if (granted.Contains(PermissionCatalog.TeamsManage))
        {
            return people;
        }

        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        return people.Where(person =>
            person.Id == userId
            || context.TeamMembers.Any(member => member.UserId == person.Id && teamIds.Contains(member.TeamId)));
    }

    private async Task<HashSet<string>> PermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var keys = await context.UserRoles.AsNoTracking()
            .Where(assignment => assignment.UserId == userId)
            .SelectMany(assignment => assignment.Role.RolePermissions.Select(grant => grant.Permission.Key))
            .ToListAsync(cancellationToken);
        return keys.ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> Wanted(IReadOnlyCollection<string>? entities)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (entities is null)
        {
            return set;
        }

        foreach (var entity in entities)
        {
            var match = SearchEntity.Order.FirstOrDefault(item => item.Equals(entity, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                set.Add(match);
            }
        }

        return set;
    }

    private static string? RequiredPermission(string entity)
    {
        return entity switch
        {
            SearchEntity.Task or SearchEntity.Todo => PermissionCatalog.TasksRead,
            SearchEntity.Project => PermissionCatalog.ProjectsRead,
            SearchEntity.Meeting or SearchEntity.MeetingNote or SearchEntity.Standup => PermissionCatalog.MeetingsRead,
            SearchEntity.Person => PermissionCatalog.TeamsRead,
            SearchEntity.LearningTopic or SearchEntity.LearningNote => PermissionCatalog.LearningRead,
            SearchEntity.Note => PermissionCatalog.NotesRead,
            SearchEntity.CalendarEvent => PermissionCatalog.CalendarRead,
            _ => null
        };
    }

    private static string Pattern(string text)
    {
        var escaped = text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }
}

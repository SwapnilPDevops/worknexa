using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Worknexa.Application.Platform;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Learning;
using Worknexa.Domain.Leaves;
using Worknexa.Domain.Meetings;
using Worknexa.Domain.Organization;
using Worknexa.Domain.Platform;
using Worknexa.Domain.Productivity;
using Worknexa.Domain.Projects;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Platform;

internal static class ActivityCapture
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static void Apply(DbContext context, Guid? actorId)
    {
        if (!Start(context, actorId, out var db, out var entries, out var users, out var teams, out var leaveTypes))
        {
            return;
        }

        var facts = LoadFacts(db, users, teams, leaveTypes);
        var plan = Build(entries, actorId, facts);
        Write(db, plan, LoadDisabled(db, plan.Notices));
    }

    public static async Task ApplyAsync(DbContext context, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!Start(context, actorId, out var db, out var entries, out var users, out var teams, out var leaveTypes))
        {
            return;
        }

        var facts = await LoadFactsAsync(db, users, teams, leaveTypes, cancellationToken);
        var plan = Build(entries, actorId, facts);
        Write(db, plan, await LoadDisabledAsync(db, plan.Notices, cancellationToken));
    }

    private static bool Start(
        DbContext context,
        Guid? actorId,
        out WorknexaDbContext db,
        out List<EntityEntry> entries,
        out HashSet<Guid> users,
        out HashSet<Guid> teams,
        out HashSet<Guid> leaveTypes)
    {
        db = null!;
        entries = [];
        users = [];
        teams = [];
        leaveTypes = [];
        if (context is not WorknexaDbContext database)
        {
            return false;
        }

        db = database;
        entries = db.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(entry => entry.Entity is not ActivityLog and not Notification and not NotificationPreference)
            .ToList();
        if (entries.Count == 0)
        {
            return false;
        }

        if (actorId is Guid actor)
        {
            users.Add(actor);
        }

        foreach (var entry in entries)
        {
            switch (entry.Entity)
            {
                case WorkTask task:
                    Add(users, task.AssigneeId);
                    Add(users, task.CreatedBy);
                    if (task.TeamId is Guid teamId)
                    {
                        teams.Add(teamId);
                    }

                    break;
                case ProjectMember member:
                    users.Add(member.UserId);
                    break;
                case TeamMember member:
                    users.Add(member.UserId);
                    teams.Add(member.TeamId);
                    break;
                case Leave leave:
                    users.Add(leave.UserId);
                    leaveTypes.Add(leave.LeaveTypeId);
                    break;
            }
        }

        return true;
    }

    private static Plan Build(List<EntityEntry> entries, Guid? actorId, Facts facts)
    {
        var createdProjects = entries.Where(entry => entry.State == EntityState.Added && entry.Entity is Project).Select(entry => ((Project)entry.Entity).Id).ToHashSet();
        var createdTeams = entries.Where(entry => entry.State == EntityState.Added && entry.Entity is Team).Select(entry => ((Team)entry.Entity).Id).ToHashSet();
        var actor = facts.Names.User(actorId);
        var logs = new List<ActivityLog>();
        var notices = new List<Notice>();
        foreach (var entry in entries)
        {
            switch (entry.Entity)
            {
                case WorkTask task:
                    CaptureTask(entry, task, actor, actorId, facts, logs, notices);
                    break;
                case Project project:
                    CaptureProject(entry, project, actor, logs);
                    break;
                case ProjectMember member:
                    CaptureProjectMember(entry, member, createdProjects, facts.Names, logs);
                    break;
                case Team team:
                    CaptureTeam(entry, team, actor, logs);
                    break;
                case TeamMember member:
                    CaptureTeamMember(entry, member, createdTeams, facts, logs);
                    break;
                case Leave leave:
                    CaptureLeave(entry, leave, actorId, facts, logs, notices);
                    break;
                case Meeting meeting when entry.State == EntityState.Added:
                    logs.Add(Row("Meeting", meeting.Id, "Scheduled", ActivityPhrases.Scheduled(actor, meeting.Title), meeting.Id));
                    break;
                case LearningTopic topic:
                    CaptureLearning(entry, topic, actor, logs);
                    break;
                case User user:
                    CaptureProfile(entry, user, actor, logs);
                    break;
            }
        }

        return new Plan(logs, notices);
    }

    private static void Write(WorknexaDbContext db, Plan plan, HashSet<(Guid UserId, NotificationType Type)> disabled)
    {
        foreach (var log in plan.Logs)
        {
            db.ActivityLogs.Add(log);
        }

        foreach (var notice in plan.Notices)
        {
            if (disabled.Contains((notice.UserId, notice.Type)))
            {
                continue;
            }

            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = notice.UserId,
                Title = notice.Title,
                Body = Trim(notice.Body, 2000),
                Link = notice.Link,
                Type = notice.Type,
                SourceId = notice.SourceId
            });
        }
    }

    private static void CaptureTask(EntityEntry entry, WorkTask task, string actor, Guid? actorId, Facts facts, List<ActivityLog> logs, List<Notice> notices)
    {
        if (entry.State == EntityState.Added)
        {
            RecordTask(task, task.Status, actor, actorId, facts, logs, notices, task.AssigneeId is not null, true);
            return;
        }

        if (entry.State == EntityState.Modified && Changed(entry, nameof(WorkTask.DeletedAt)))
        {
            logs.Add(Row("Task", task.Id, "Deleted", $"{actor} deleted {task.Title}.", task.Id, task.Status, task.AssigneeId));
            return;
        }

        if (entry.State == EntityState.Modified && Changed(entry, nameof(WorkTask.Status)))
        {
            RecordTask(task, task.Status, actor, actorId, facts, logs, notices, false, false);
        }

        if (entry.State == EntityState.Modified && Changed(entry, nameof(WorkTask.AssigneeId)) && task.AssigneeId is Guid)
        {
            var summary = ActivityPhrases.Assigned(actor, task.Title, facts.Names.User(task.AssigneeId));
            logs.Add(Row("Task", task.Id, "Assigned", summary, task.Id, task.Status, task.AssigneeId));
            Notify(notices, task.AssigneeId, actorId, NotificationType.TaskAssigned, "Task assigned", summary, "/tasks", task.Id);
        }
    }

    private static void RecordTask(WorkTask task, WorkTaskStatus status, string actor, Guid? actorId, Facts facts, List<ActivityLog> logs, List<Notice> notices, bool assigned, bool created)
    {
        if (status == WorkTaskStatus.Completed)
        {
            var summary = ActivityPhrases.Completed(actor, task.Title);
            logs.Add(Row("Task", task.Id, "Completed", summary, task.Id, status, task.AssigneeId));
            Notify(notices, task.AssigneeId, actorId, NotificationType.TaskCompleted, "Task completed", summary, "/tasks", task.Id);
            Notify(notices, task.CreatedBy, actorId, NotificationType.TaskCompleted, "Task completed", summary, "/tasks", task.Id);
            return;
        }

        if (status == WorkTaskStatus.Blocked)
        {
            var summary = ActivityPhrases.Blocked(actor, task.Title);
            logs.Add(Row("Task", task.Id, "Blocked", summary, task.Id, status, task.AssigneeId));
            foreach (var person in BlockedRecipients(task, actorId, facts))
            {
                Notify(notices, person, actorId, NotificationType.TeamMemberBlocked, "Team member blocked", summary, "/tasks", task.Id);
            }

            if (assigned)
            {
                var assignedSummary = ActivityPhrases.Assigned(actor, task.Title, facts.Names.User(task.AssigneeId));
                logs.Add(Row("Task", task.Id, "Assigned", assignedSummary, task.Id, status, task.AssigneeId));
                Notify(notices, task.AssigneeId, actorId, NotificationType.TaskAssigned, "Task assigned", assignedSummary, "/tasks", task.Id);
            }

            return;
        }

        if (assigned && task.AssigneeId is Guid)
        {
            var summary = ActivityPhrases.Assigned(actor, task.Title, facts.Names.User(task.AssigneeId));
            logs.Add(Row("Task", task.Id, "Assigned", summary, task.Id, status, task.AssigneeId));
            Notify(notices, task.AssigneeId, actorId, NotificationType.TaskAssigned, "Task assigned", summary, "/tasks", task.Id);
            return;
        }

        if (created)
        {
            logs.Add(Row("Task", task.Id, "Created", ActivityPhrases.Created(actor, task.Title), task.Id, status, task.AssigneeId));
            return;
        }

        logs.Add(Row("Task", task.Id, "Status", ActivityPhrases.Moved(task.Title, ActivityPhrases.Label(status.ToString())), task.Id, status, task.AssigneeId));
    }

    private static void CaptureProject(EntityEntry entry, Project project, string actor, List<ActivityLog> logs)
    {
        if (entry.State == EntityState.Added)
        {
            logs.Add(Row("Project", project.Id, "Created", ActivityPhrases.Created(actor, project.Name)));
            return;
        }

        if (entry.State != EntityState.Modified)
        {
            return;
        }

        if (Changed(entry, nameof(Project.DeletedAt)))
        {
            logs.Add(Row("Project", project.Id, "Deleted", $"{actor} deleted {project.Name}."));
            return;
        }

        if (Changed(entry, nameof(Project.Status)))
        {
            logs.Add(Row("Project", project.Id, "Status", ActivityPhrases.Moved(project.Name, ActivityPhrases.Label(project.Status.ToString()))));
        }

        if (Changed(entry, nameof(Project.OwnerId)))
        {
            logs.Add(Row("Project", project.Id, "OwnerChanged", "Changed the project owner."));
        }

        if (AnyChanged(entry, "Name", "Code", "Description", "TeamId", "Priority", "StartDate", "EndDate", "Technology", "RepositoryUrl", "DocumentationUrl"))
        {
            logs.Add(Row("Project", project.Id, "Updated", $"{actor} updated {project.Name}."));
        }
    }

    private static void CaptureProjectMember(EntityEntry entry, ProjectMember member, HashSet<Guid> createdProjects, Names names, List<ActivityLog> logs)
    {
        if (createdProjects.Contains(member.ProjectId))
        {
            return;
        }

        if (entry.State is EntityState.Added || (entry.State == EntityState.Modified && Changed(entry, nameof(ProjectMember.Role))))
        {
            logs.Add(Row("Project", member.ProjectId, "MemberAdded", $"Updated the project team for {names.User(member.UserId)}."));
            return;
        }

        if (entry.State == EntityState.Deleted)
        {
            logs.Add(Row("Project", member.ProjectId, "MemberRemoved", "Removed a person from the project."));
        }
    }

    private static void CaptureTeam(EntityEntry entry, Team team, string actor, List<ActivityLog> logs)
    {
        if (entry.State == EntityState.Added)
        {
            logs.Add(Row("Team", team.Id, "Created", ActivityPhrases.Created(actor, team.Name)));
            return;
        }

        if (entry.State != EntityState.Modified)
        {
            return;
        }

        if (Changed(entry, nameof(Team.DeletedAt)))
        {
            logs.Add(Row("Team", team.Id, "Deleted", $"{actor} deleted {team.Name}."));
            return;
        }

        if (AnyChanged(entry, nameof(Team.Name), nameof(Team.Description)))
        {
            logs.Add(Row("Team", team.Id, "Updated", $"{actor} updated {team.Name}."));
        }
    }

    private static void CaptureTeamMember(EntityEntry entry, TeamMember member, HashSet<Guid> createdTeams, Facts facts, List<ActivityLog> logs)
    {
        if (createdTeams.Contains(member.TeamId))
        {
            return;
        }

        var team = facts.Teams.TryGetValue(member.TeamId, out var name) ? name : "the team";
        var person = facts.Names.User(member.UserId);
        if (entry.State == EntityState.Deleted)
        {
            logs.Add(Row("Team", member.TeamId, "Updated", "Updated the team membership."));
            logs.Add(Row("Member", member.UserId, "Team", $"{person} left {team}."));
            return;
        }

        if (entry.State == EntityState.Added || (entry.State == EntityState.Modified && Changed(entry, nameof(TeamMember.Role))))
        {
            logs.Add(Row("Team", member.TeamId, "Updated", $"Updated the team membership for {person}."));
            logs.Add(Row("Member", member.UserId, "Team", $"{person} is {member.Role} on {team}."));
        }
    }

    private static void CaptureLeave(EntityEntry entry, Leave leave, Guid? actorId, Facts facts, List<ActivityLog> logs, List<Notice> notices)
    {
        var employee = facts.Names.User(leave.UserId);
        if (entry.State == EntityState.Added)
        {
            var typeName = facts.LeaveTypes.TryGetValue(leave.LeaveTypeId, out var name) ? name : "leave";
            var summary = ActivityPhrases.LeaveRequested(employee, typeName);
            logs.Add(Row("Member", leave.UserId, "Leave requested", summary));
            NotifyLeave(leave, actorId, facts, summary, notices);
            return;
        }

        if (entry.State != EntityState.Modified || Changed(entry, nameof(Leave.DeletedAt)))
        {
            return;
        }

        if (Changed(entry, nameof(Leave.Status)))
        {
            var summary = ActivityPhrases.LeaveStatus(employee, leave.Status.ToString());
            logs.Add(Row("Member", leave.UserId, leave.Status.ToString(), summary));
            NotifyLeave(leave, actorId, facts, summary, notices);
            return;
        }

        if (AnyChanged(entry, nameof(Leave.StartDate), nameof(Leave.EndDate), nameof(Leave.Reason), nameof(Leave.LeaveTypeId), nameof(Leave.UserId)))
        {
            logs.Add(Row("Member", leave.UserId, "Leave updated", "Updated a leave request."));
        }
    }

    private static void CaptureLearning(EntityEntry entry, LearningTopic topic, string actor, List<ActivityLog> logs)
    {
        if (entry.State == EntityState.Added)
        {
            logs.Add(Row("LearningTopic", topic.Id, "Created", ActivityPhrases.Created(actor, topic.Title)));
            return;
        }

        if (entry.State == EntityState.Modified && Changed(entry, nameof(LearningTopic.Status)))
        {
            logs.Add(Row("LearningTopic", topic.Id, "Status", ActivityPhrases.Moved(topic.Title, ActivityPhrases.Label(topic.Status.ToString()))));
        }
    }

    private static void CaptureProfile(EntityEntry entry, User user, string actor, List<ActivityLog> logs)
    {
        if (entry.State == EntityState.Modified && AnyChanged(entry, nameof(User.DisplayName), nameof(User.Email), nameof(User.Phone), nameof(User.PhotoUrl), nameof(User.Designation), nameof(User.Department), nameof(User.JoinedOn), nameof(User.ManagerId), nameof(User.Status), nameof(User.Notes)))
        {
            logs.Add(Row("Member", user.Id, "Updated", $"{actor} updated {user.DisplayName}."));
        }
    }

    private static void NotifyLeave(Leave leave, Guid? actorId, Facts facts, string summary, List<Notice> notices)
    {
        Notify(notices, leave.UserId, actorId, NotificationType.LeaveUpdate, "Leave update", summary, "/leaves", leave.Id);
        if (leave.UserId == actorId && facts.Managers.TryGetValue(leave.UserId, out var managerId))
        {
            Notify(notices, managerId, actorId, NotificationType.LeaveUpdate, "Leave update", summary, "/leaves", leave.Id);
        }
    }

    private static IReadOnlyList<Guid> BlockedRecipients(WorkTask task, Guid? actorId, Facts facts)
    {
        var people = new List<Guid>();
        if (task.TeamId is Guid teamId && facts.TeamMembers.TryGetValue(teamId, out var members))
        {
            people.AddRange(members.Where(id => id != actorId));
        }

        if (people.Count > 0)
        {
            return people.Distinct().ToArray();
        }

        var personId = task.AssigneeId ?? task.CreatedBy;
        if (personId is Guid id && facts.Managers.TryGetValue(id, out var managerId) && managerId is Guid manager && manager != actorId)
        {
            return [manager];
        }

        if (task.CreatedBy is Guid creator && creator != actorId)
        {
            return [creator];
        }

        return [];
    }

    private static Facts LoadFacts(WorknexaDbContext db, HashSet<Guid> users, HashSet<Guid> teams, HashSet<Guid> leaveTypes)
    {
        var facts = RememberTracked(db);
        if (users.Count > 0)
        {
            Fill(facts, db.Users.AsNoTracking().Where(user => users.Contains(user.Id)).Select(user => new Person(user.Id, user.DisplayName, user.ManagerId)).ToList());
        }

        if (teams.Count > 0)
        {
            foreach (var team in db.Teams.AsNoTracking().Where(team => teams.Contains(team.Id)).Select(team => new { team.Id, team.Name }).ToList())
            {
                facts.Teams[team.Id] = team.Name;
            }

            foreach (var member in db.TeamMembers.AsNoTracking().Where(member => teams.Contains(member.TeamId)).Select(member => new { member.TeamId, member.UserId }).ToList())
            {
                if (!facts.TeamMembers.TryGetValue(member.TeamId, out var people))
                {
                    people = [];
                    facts.TeamMembers[member.TeamId] = people;
                }

                people.Add(member.UserId);
            }
        }

        if (leaveTypes.Count > 0)
        {
            foreach (var type in db.LeaveTypes.AsNoTracking().Where(type => leaveTypes.Contains(type.Id)).Select(type => new { type.Id, type.Name }).ToList())
            {
                facts.LeaveTypes[type.Id] = type.Name;
            }
        }

        return facts;
    }

    private static async Task<Facts> LoadFactsAsync(WorknexaDbContext db, HashSet<Guid> users, HashSet<Guid> teams, HashSet<Guid> leaveTypes, CancellationToken cancellationToken)
    {
        var facts = RememberTracked(db);
        if (users.Count > 0)
        {
            Fill(facts, await db.Users.AsNoTracking().Where(user => users.Contains(user.Id)).Select(user => new Person(user.Id, user.DisplayName, user.ManagerId)).ToListAsync(cancellationToken));
        }

        if (teams.Count > 0)
        {
            foreach (var team in await db.Teams.AsNoTracking().Where(team => teams.Contains(team.Id)).Select(team => new { team.Id, team.Name }).ToListAsync(cancellationToken))
            {
                facts.Teams[team.Id] = team.Name;
            }

            foreach (var member in await db.TeamMembers.AsNoTracking().Where(member => teams.Contains(member.TeamId)).Select(member => new { member.TeamId, member.UserId }).ToListAsync(cancellationToken))
            {
                if (!facts.TeamMembers.TryGetValue(member.TeamId, out var people))
                {
                    people = [];
                    facts.TeamMembers[member.TeamId] = people;
                }

                people.Add(member.UserId);
            }
        }

        if (leaveTypes.Count > 0)
        {
            foreach (var type in await db.LeaveTypes.AsNoTracking().Where(type => leaveTypes.Contains(type.Id)).Select(type => new { type.Id, type.Name }).ToListAsync(cancellationToken))
            {
                facts.LeaveTypes[type.Id] = type.Name;
            }
        }

        return facts;
    }

    private static Facts RememberTracked(WorknexaDbContext db)
    {
        var facts = new Facts();
        foreach (var user in db.ChangeTracker.Entries<User>())
        {
            facts.Names.Remember(user.Entity.Id, user.Entity.DisplayName);
            facts.Managers[user.Entity.Id] = user.Entity.ManagerId;
        }

        foreach (var team in db.ChangeTracker.Entries<Team>())
        {
            facts.Teams[team.Entity.Id] = team.Entity.Name;
        }

        foreach (var type in db.ChangeTracker.Entries<LeaveType>())
        {
            facts.LeaveTypes[type.Entity.Id] = type.Entity.Name;
        }

        return facts;
    }

    private static void Fill(Facts facts, IEnumerable<Person> people)
    {
        foreach (var person in people)
        {
            facts.Names.Remember(person.Id, person.Name);
            facts.Managers[person.Id] = person.ManagerId;
        }
    }

    private static HashSet<(Guid UserId, NotificationType Type)> LoadDisabled(WorknexaDbContext db, List<Notice> notices)
    {
        return LoadDisabledAsync(db, notices, CancellationToken.None).GetAwaiter().GetResult();
    }

    private static async Task<HashSet<(Guid UserId, NotificationType Type)>> LoadDisabledAsync(WorknexaDbContext db, List<Notice> notices, CancellationToken cancellationToken)
    {
        var recipients = notices.Select(notice => notice.UserId).Distinct().ToArray();
        if (recipients.Length == 0)
        {
            return [];
        }

        var rows = await db.NotificationPreferences.AsNoTracking()
            .Where(preference => recipients.Contains(preference.UserId) && !preference.IsEnabled)
            .Select(preference => new { preference.UserId, preference.Type })
            .ToListAsync(cancellationToken);
        return rows.Select(preference => (preference.UserId, preference.Type)).ToHashSet();
    }

    private static void Notify(List<Notice> notices, Guid? userId, Guid? actorId, NotificationType type, string title, string body, string link, Guid sourceId)
    {
        if (userId is not Guid id || id == actorId || notices.Any(notice => notice.UserId == id && notice.Type == type && notice.SourceId == sourceId))
        {
            return;
        }

        notices.Add(new Notice(id, type, title, body, link, sourceId));
    }

    private static ActivityLog Row(string entity, Guid entityId, string action, string summary, Guid? sourceId = null, WorkTaskStatus? status = null, Guid? assigneeId = null)
    {
        string? metadata = sourceId is null && status is null && assigneeId is null
            ? null
            : JsonSerializer.Serialize(new { status = status?.ToString(), assigneeId }, Json);
        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            EntityName = entity,
            EntityId = entityId,
            Action = action,
            Summary = Trim(summary, 2000),
            Metadata = metadata
        };
    }

    private static bool Changed(EntityEntry entry, string property)
    {
        var item = entry.Property(property);
        return !Equals(item.CurrentValue, item.OriginalValue);
    }

    private static bool AnyChanged(EntityEntry entry, params string[] properties)
    {
        return properties.Any(property => Changed(entry, property));
    }

    private static void Add(HashSet<Guid> ids, Guid? id)
    {
        if (id is Guid value)
        {
            ids.Add(value);
        }
    }

    private static string Trim(string value, int length)
    {
        return value.Length <= length ? value : value[..length];
    }

    private sealed class Names
    {
        private readonly Dictionary<Guid, string> _names = [];

        public void Remember(Guid id, string name) => _names[id] = name;

        public string User(Guid? id) => id is Guid value && _names.TryGetValue(value, out var name) && name.Length > 0 ? name : "Someone";
    }

    private sealed class Facts
    {
        public Names Names { get; } = new();

        public Dictionary<Guid, string> Teams { get; } = [];

        public Dictionary<Guid, string> LeaveTypes { get; } = [];

        public Dictionary<Guid, List<Guid>> TeamMembers { get; } = [];

        public Dictionary<Guid, Guid?> Managers { get; } = [];
    }

    private sealed record Person(Guid Id, string Name, Guid? ManagerId);

    private sealed record Plan(List<ActivityLog> Logs, List<Notice> Notices);

    private sealed record Notice(Guid UserId, NotificationType Type, string Title, string Body, string? Link, Guid SourceId);
}

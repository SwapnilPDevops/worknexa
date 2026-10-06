using System.Text.Json.Nodes;
using Worknexa.Application.Organization;
using Worknexa.Application.Projects;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;

namespace Worknexa.Application.Assistant.Tools;

public sealed record SearchProjectsArgs(string? Query, string? Status);

public sealed class SearchProjectsTool(IProjectService projects) : ReadTool<SearchProjectsArgs>
{
    private const int Limit = 15;

    public override string Name => "searchProjects";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.ProjectsRead];

    public override JsonObject Parameters => ToolSchema.Object(
        [],
        ("query", ToolSchema.Text("Words from the project name or code. Leave out to list projects.")),
        ("status", ToolSchema.Choice("Only projects with this status.", ToolFormat.Names<ProjectStatus>())));

    protected override async Task<object> RunAsync(ToolContext context, SearchProjectsArgs args, CancellationToken cancellationToken)
    {
        var rows = await projects.ListAsync(context.UserId, string.IsNullOrWhiteSpace(args.Query) ? null : args.Query.Trim(), cancellationToken);
        if (ToolFormat.Parse<ProjectStatus>(args.Status) is { } status)
        {
            rows = rows.Where(project => project.Status == status.ToString()).ToArray();
        }

        return new
        {
            count = rows.Count,
            projects = rows.Take(Limit).Select(project => new
            {
                id = project.Id,
                project.Name,
                project.Code,
                project.Status,
                project.Priority,
                owner = project.OwnerName,
                team = project.TeamName,
                target = project.TargetDate,
                progress = project.Progress,
                openTasks = project.OpenTasks
            })
        };
    }
}

public sealed record ProjectArgs(string? Project);

public sealed class GetProjectStatusTool(IProjectService projects) : ReadTool<ProjectArgs>
{
    public override string Name => "getProjectStatus";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.ProjectsRead];

    public override JsonObject Parameters => ToolSchema.Object(["project"], ("project", ToolSchema.Text("Project name, code, or id.")));

    protected override async Task<object> RunAsync(ToolContext context, ProjectArgs args, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.Project))
        {
            return new { status = "needs_input", question = "Which project?" };
        }

        var all = await projects.ListAsync(context.UserId, null, cancellationToken);
        var match = NameMatch.Find(all, project => project.Id, project => project.Name, args.Project, project => project.Code);
        if (match.Item is not { } found)
        {
            if (!match.Ambiguous)
            {
                return new { status = "not_found", question = $"No project matches \"{args.Project.Trim()}\"." };
            }

            return new
            {
                status = "ambiguous",
                question = "Several projects match. Ask which one, by code.",
                candidates = match.Candidates.Select(project => new { project.Name, project.Code, project.Status, project.Progress })
            };
        }

        var project = await projects.GetAsync(context.UserId, found.Id, cancellationToken);
        var board = project.Dashboard;
        return new
        {
            project.Name,
            project.Code,
            project.Status,
            project.Priority,
            owner = project.OwnerName,
            team = project.TeamName,
            start = project.StartDate,
            target = project.TargetDate,
            board.Progress,
            board.OpenTasks,
            board.CompletedTasks,
            board.BlockedTasks,
            upcomingDeadlines = board.UpcomingDeadlines.Take(10).Select(deadline => new { deadline.Title, due = ToolFormat.Local(context, deadline.DueAt), assignee = deadline.AssigneeName }),
            workload = board.TeamWorkload.Select(row => new { row.Name, row.OpenTasks, row.BlockedTasks })
        };
    }
}

public sealed record SearchTeamArgs(string? Query);

public sealed class SearchTeamTool(ITeamService teams) : ReadTool<SearchTeamArgs>
{
    private const int Limit = 15;

    public override string Name => "searchTeam";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.TeamsRead];

    public override JsonObject Parameters => ToolSchema.Object([], ("query", ToolSchema.Text("Name, email, designation, or department. Leave out to list people.")));

    protected override async Task<object> RunAsync(ToolContext context, SearchTeamArgs args, CancellationToken cancellationToken)
    {
        var people = await teams.MembersAsync(context.UserId, string.IsNullOrWhiteSpace(args.Query) ? null : args.Query.Trim(), cancellationToken);
        return new
        {
            count = people.Count,
            people = people.Take(Limit).Select(person => new { person.Name, person.Email, person.Designation, person.Department, person.Status })
        };
    }
}

public sealed record TeamArgs(string? Team);

public sealed class GetTeamWorkloadTool(ITeamService teams) : ReadTool<TeamArgs>
{
    public override string Name => "getTeamWorkload";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.TeamsRead];

    public override JsonObject Parameters => ToolSchema.Object([], ("team", ToolSchema.Text("Team name. Leave out when the user belongs to one team.")));

    protected override async Task<object> RunAsync(ToolContext context, TeamArgs args, CancellationToken cancellationToken)
    {
        var list = await teams.ListAsync(context.UserId, cancellationToken);
        TeamListItem? team;
        if (!string.IsNullOrWhiteSpace(args.Team))
        {
            var match = NameMatch.Find(list, item => item.Id, item => item.Name, args.Team);
            team = match.Item;
            if (team is null)
            {
                return new { status = match.Ambiguous ? "ambiguous" : "not_found", teams = (match.Ambiguous ? match.Candidates : list).Select(item => item.Name) };
            }
        }
        else if (list.Count == 1)
        {
            team = list[0];
        }
        else
        {
            return new { status = list.Count == 0 ? "no_teams" : "choose_team", teams = list.Select(item => item.Name) };
        }

        var rows = await teams.WorkloadAsync(context.UserId, team.Id, cancellationToken);
        return new
        {
            team = team.Name,
            members = rows.Select(row => new { row.Name, row.Assigned, row.InProgress, row.Completed, row.Overdue, row.Blocked })
        };
    }
}

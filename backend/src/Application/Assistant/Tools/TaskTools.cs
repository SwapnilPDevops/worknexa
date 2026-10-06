using System.Text.Json.Nodes;
using Worknexa.Application.Search;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;

namespace Worknexa.Application.Assistant.Tools;

public sealed record CreateTaskArgs(string? Title, string? Description, string? Assignee, string? Due, string? Project, string? Priority);

public sealed class CreateTaskTool(ITaskService tasks, AssistantLookup lookup) : ActionTool<CreateTaskArgs, TaskWriteRequest>
{
    private static readonly TimeOnly EndOfDay = new(23, 59);

    public override string Name => "createTask";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.TasksRead, PermissionCatalog.TasksManage];

    public override JsonObject Parameters => ToolSchema.Object(
        ["title"],
        ("title", ToolSchema.Text("Short task title, for example \"TravelDesk API documentation\".")),
        ("description", ToolSchema.Text("Optional details.")),
        ("assignee", ToolSchema.Text("Name of the person to assign, as the user said it, or \"me\". Leave out to assign the user.")),
        ("due", ToolSchema.Text("Due date as yyyy-MM-dd, or words such as \"Friday\".")),
        ("project", ToolSchema.Text("Project name or code. Leave out to infer it from the title; use \"none\" for no project.")),
        ("priority", ToolSchema.Choice("Priority. Defaults to Medium.", ToolFormat.Names<WorkPriority>())));

    protected override async Task<Planned<TaskWriteRequest>> PlanAsync(ToolContext context, CreateTaskArgs args, CancellationToken cancellationToken)
    {
        var questions = new List<string>();
        var title = args.Title?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            questions.Add("What should the task be called?");
        }

        var assignee = await lookup.PersonAsync(context, string.IsNullOrWhiteSpace(args.Assignee) ? "me" : args.Assignee, cancellationToken);
        Collect(questions, assignee.Question);

        var due = AssistantDates.Resolve(args.Due, context.Today);
        if (!string.IsNullOrWhiteSpace(args.Due) && due is null)
        {
            questions.Add($"When is it due? I could not read \"{args.Due.Trim()}\" as a date.");
        }

        var project = await lookup.ProjectForAsync(context, args.Project, title, cancellationToken);
        Collect(questions, project.Question);

        if (questions.Count > 0)
        {
            return Planned<TaskWriteRequest>.Ask(questions);
        }

        var priority = ToolFormat.Parse<WorkPriority>(args.Priority) ?? WorkPriority.Medium;
        var person = assignee.Value!;
        var dueAt = due is { } day ? context.At(day, EndOfDay) : (DateTimeOffset?)null;
        var request = new TaskWriteRequest(
            title!,
            string.IsNullOrWhiteSpace(args.Description) ? null : args.Description.Trim(),
            nameof(WorkTaskStatus.Todo),
            priority.ToString(),
            null,
            dueAt,
            person.Id,
            project.Value?.Id,
            null,
            null,
            null,
            null,
            null);

        var who = person.Id == context.UserId ? $"{person.Name} (you)" : person.Name;
        var dueText = due is { } date ? AssistantDates.Format(date) : "No due date";
        var summary = $"Create task \"{title}\" for {who}"
            + (due is { } d ? $", due {AssistantDates.Format(d)}" : "")
            + (project.Value is { } p ? $", in project {p.Label}" : "")
            + ".";
        IReadOnlyList<IntentField> fields =
        [
            new("Task", title!),
            new("Assignee", who),
            new("Due", dueText),
            new("Project", project.Value?.Label ?? "None"),
            new("Priority", priority.ToString())
        ];
        return new Planned<TaskWriteRequest>(summary, fields, request, []);
    }

    protected override async Task<ActionOutcome> ExecuteAsync(ToolContext context, TaskWriteRequest payload, CancellationToken cancellationToken)
    {
        var created = await tasks.CreateAsync(context.UserId, payload, cancellationToken);
        return new ActionOutcome($"Created task \"{created.Title}\".", $"/tasks?task={created.Id}");
    }

    internal static void Collect(List<string> questions, string? question)
    {
        if (!string.IsNullOrEmpty(question))
        {
            questions.Add(question);
        }
    }
}

public sealed record UpdateTaskArgs(string? Task, string? Title, string? Status, string? Priority, string? Due, string? Assignee);

public sealed record TaskChanges(Guid TaskId, string? Title, string? Status, string? Priority, DateTimeOffset? DueAt, Guid? AssigneeId);

public sealed class UpdateTaskTool(ITaskService tasks, AssistantLookup lookup) : ActionTool<UpdateTaskArgs, TaskChanges>
{
    private static readonly TimeOnly EndOfDay = new(23, 59);

    public override string Name => "updateTask";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.TasksRead, PermissionCatalog.TasksManage];

    public override JsonObject Parameters => ToolSchema.Object(
        ["task"],
        ("task", ToolSchema.Text("The task's current title, or its id from an earlier tool result.")),
        ("title", ToolSchema.Text("New title.")),
        ("status", ToolSchema.Choice("New status.", ToolFormat.Names<WorkTaskStatus>())),
        ("priority", ToolSchema.Choice("New priority.", ToolFormat.Names<WorkPriority>())),
        ("due", ToolSchema.Text("New due date as yyyy-MM-dd, or words such as \"next Monday\".")),
        ("assignee", ToolSchema.Text("New assignee's name, or \"me\".")));

    protected override async Task<Planned<TaskChanges>> PlanAsync(ToolContext context, UpdateTaskArgs args, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.Task))
        {
            return Planned<TaskChanges>.Ask(["Which task should I change?"]);
        }

        var found = await lookup.TaskAsync(context, args.Task, cancellationToken);
        if (found.Value is not { } task)
        {
            return Planned<TaskChanges>.Ask([found.Question!]);
        }

        var questions = new List<string>();
        var fields = new List<IntentField> { new("Task", task.Title) };
        var changes = new TaskChanges(task.Id, null, null, null, null, null);

        if (!string.IsNullOrWhiteSpace(args.Title) && args.Title.Trim() != task.Title)
        {
            changes = changes with { Title = args.Title.Trim() };
            fields.Add(new IntentField("Title", $"{task.Title} → {changes.Title}"));
        }

        if (!string.IsNullOrWhiteSpace(args.Status))
        {
            if (ToolFormat.Parse<WorkTaskStatus>(args.Status) is { } status)
            {
                if (status.ToString() != task.Status)
                {
                    changes = changes with { Status = status.ToString() };
                    fields.Add(new IntentField("Status", $"{task.Status} → {status}"));
                }
            }
            else
            {
                questions.Add($"Which status should it have: {ToolFormat.Join(ToolFormat.Names<WorkTaskStatus>())}?");
            }
        }

        if (!string.IsNullOrWhiteSpace(args.Priority))
        {
            if (ToolFormat.Parse<WorkPriority>(args.Priority) is { } priority)
            {
                if (priority.ToString() != task.Priority)
                {
                    changes = changes with { Priority = priority.ToString() };
                    fields.Add(new IntentField("Priority", $"{task.Priority} → {priority}"));
                }
            }
            else
            {
                questions.Add($"Which priority should it have: {ToolFormat.Join(ToolFormat.Names<WorkPriority>())}?");
            }
        }

        if (!string.IsNullOrWhiteSpace(args.Due))
        {
            if (AssistantDates.Resolve(args.Due, context.Today) is { } due)
            {
                changes = changes with { DueAt = context.At(due, EndOfDay) };
                fields.Add(new IntentField("Due", $"{Day(context, task.DueAt)} → {AssistantDates.Format(due)}"));
            }
            else
            {
                questions.Add($"When should it be due? I could not read \"{args.Due.Trim()}\" as a date.");
            }
        }

        if (!string.IsNullOrWhiteSpace(args.Assignee))
        {
            var person = await lookup.PersonAsync(context, args.Assignee, cancellationToken);
            CreateTaskTool.Collect(questions, person.Question);
            if (person.Value is { } assignee && assignee.Id != task.AssigneeId)
            {
                changes = changes with { AssigneeId = assignee.Id };
                fields.Add(new IntentField("Assignee", $"{task.AssigneeName ?? "Unassigned"} → {assignee.Name}"));
            }
        }

        if (questions.Count > 0)
        {
            return Planned<TaskChanges>.Ask(questions);
        }

        if (fields.Count == 1)
        {
            return Planned<TaskChanges>.Ask([$"What should change on \"{task.Title}\"?"]);
        }

        var summary = $"Update task \"{task.Title}\": {string.Join("; ", fields.Skip(1).Select(field => $"{field.Label.ToLowerInvariant()} {field.Value}"))}.";
        return new Planned<TaskChanges>(summary, fields, changes, []);
    }

    protected override async Task<ActionOutcome> ExecuteAsync(ToolContext context, TaskChanges payload, CancellationToken cancellationToken)
    {
        var current = await tasks.GetAsync(context.UserId, payload.TaskId, cancellationToken);
        if (payload.Title is not null || payload.Priority is not null || payload.DueAt is not null || payload.AssigneeId is not null)
        {
            current = await tasks.UpdateAsync(context.UserId, current.Id, new TaskWriteRequest(
                payload.Title ?? current.Title,
                current.Description,
                current.Status,
                payload.Priority ?? current.Priority,
                current.StartAt,
                payload.DueAt ?? current.DueAt,
                payload.AssigneeId ?? current.AssigneeId,
                current.ProjectId,
                current.ParentTaskId,
                current.TeamId,
                current.EstimatedHours,
                current.ActualHours,
                current.Tags), cancellationToken);
        }

        if (payload.Status is not null && payload.Status != current.Status)
        {
            current = await tasks.SetStatusAsync(context.UserId, current.Id, payload.Status, cancellationToken);
        }

        return new ActionOutcome($"Updated task \"{current.Title}\".", $"/tasks?task={current.Id}");
    }

    private static string Day(ToolContext context, DateTimeOffset? value)
    {
        return value is { } due ? AssistantDates.Format(DateOnly.FromDateTime(due.ToOffset(context.Offset).DateTime)) : "No due date";
    }
}

public sealed record SearchTasksArgs(string? Query, string? Filter, string? Status);

public sealed class SearchTasksTool(ITaskService tasks, AssistantLookup lookup) : ReadTool<SearchTasksArgs>
{
    private const int Limit = 15;

    public override string Name => "searchTasks";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.TasksRead];

    public override JsonObject Parameters => ToolSchema.Object(
        [],
        ("query", ToolSchema.Text("Words from the task title.")),
        ("filter", ToolSchema.Choice("Which tasks to list when there is no query. Defaults to mine.", "mine", "overdue", "team", "all")),
        ("status", ToolSchema.Choice("Only tasks with this status.", ToolFormat.Names<WorkTaskStatus>())));

    protected override async Task<object> RunAsync(ToolContext context, SearchTasksArgs args, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(args.Query))
        {
            var hits = await lookup.SearchAsync(context, args.Query, [SearchEntity.Task], Limit, cancellationToken);
            return new { count = hits.Count, tasks = hits.Select(hit => new { id = hit.Id, title = hit.Title, status = hit.Detail }) };
        }

        var view = args.Filter?.Trim().ToLowerInvariant() switch
        {
            "overdue" => "overdue",
            "team" => "team",
            "all" => "list",
            _ => "mine"
        };
        var status = ToolFormat.Parse<WorkTaskStatus>(args.Status)?.ToString();
        var rows = await tasks.ListAsync(context.UserId, view, status, null, null, null, null, null, null, null, context.Today, context.OffsetMinutes, cancellationToken);
        return new
        {
            count = rows.Count,
            shown = Math.Min(rows.Count, Limit),
            tasks = rows.Take(Limit).Select(task => new
            {
                id = task.Id,
                title = task.Title,
                status = task.Status,
                priority = task.Priority,
                due = ToolFormat.Local(context, task.DueAt),
                assignee = task.AssigneeName,
                project = task.ProjectName
            })
        };
    }
}

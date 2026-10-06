using System.Globalization;
using Worknexa.Application.Common;
using Worknexa.Application.Learning;
using Worknexa.Application.Projects;
using Worknexa.Application.Search;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;

namespace Worknexa.Application.Assistant.Tools;

public sealed record ProjectRef(Guid Id, string Name, string? Code)
{
    public string Label => string.IsNullOrWhiteSpace(Code) ? Name : $"{Name} ({Code})";
}

/// <summary>A resolved record, or the question to ask when the name was unknown or ambiguous. Both empty means "none".</summary>
public sealed record Lookup<T>(T? Value, string? Question)
    where T : class
{
    public static Lookup<T> Empty { get; } = new(null, null);
}

/// <summary>Turns the names people use into records, through the same services and permission checks as the API.</summary>
public sealed class AssistantLookup(ITaskService tasks, IProjectService projects, ILearningService learning, IWorkspaceSearch search)
{
    private static readonly string[] Self = ["me", "myself", "i", "self", "you"];
    private static readonly string[] Nothing = ["none", "no project", "no", "-"];

    private WorkOptions? _options;

    public static bool IsNone(string? value) => value is not null && Nothing.Contains(value.Trim().ToLowerInvariant());

    public async Task<Lookup<NamedRef>> PersonAsync(ToolContext context, string query, CancellationToken cancellationToken)
    {
        var options = await OptionsAsync(context, cancellationToken);
        if (Self.Contains(query.Trim().ToLowerInvariant()))
        {
            return new Lookup<NamedRef>(options.People.FirstOrDefault(person => person.Id == context.UserId) ?? new NamedRef(context.UserId, "You"), null);
        }

        return Pick(NameMatch.Find(options.People, person => person.Id, person => person.Name, query), "person", query, person => person.Name);
    }

    /// <summary>Open projects the person can see. Uses the project list when allowed, otherwise the task form options.</summary>
    public async Task<IReadOnlyList<ProjectRef>> OpenProjectsAsync(ToolContext context, CancellationToken cancellationToken)
    {
        if (!context.Can(PermissionCatalog.ProjectsRead))
        {
            return (await OptionsAsync(context, cancellationToken)).Projects.Select(project => new ProjectRef(project.Id, project.Name, null)).ToArray();
        }

        return (await projects.ListAsync(context.UserId, null, cancellationToken))
            .Where(project => project.Status is not (nameof(ProjectStatus.Completed) or nameof(ProjectStatus.Cancelled)))
            .Select(project => new ProjectRef(project.Id, project.Name, project.Code))
            .ToArray();
    }

    /// <summary>The named project, or the single project whose name appears in the title when none was named.</summary>
    public async Task<Lookup<ProjectRef>> ProjectForAsync(ToolContext context, string? query, string? title, CancellationToken cancellationToken)
    {
        if (IsNone(query))
        {
            return Lookup<ProjectRef>.Empty;
        }

        var open = await OpenProjectsAsync(context, cancellationToken);
        if (!string.IsNullOrWhiteSpace(query))
        {
            return Pick(NameMatch.Find(open, project => project.Id, project => project.Name, query, project => project.Code), "project", query, project => project.Label);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Lookup<ProjectRef>.Empty;
        }

        var mentioned = NameMatch.MentionedIn(open, project => project.Name, title);
        return mentioned.Count switch
        {
            1 => new Lookup<ProjectRef>(mentioned[0], null),
            > 1 => new Lookup<ProjectRef>(null, $"Which {mentioned[0].Name} project should this go in: {ToolFormat.Join(mentioned.Take(5).Select(project => project.Label))}? Say none for no project."),
            _ => Lookup<ProjectRef>.Empty
        };
    }

    public async Task<Lookup<TaskDetail>> TaskAsync(ToolContext context, string query, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(query.Trim(), out var id))
        {
            try
            {
                return new Lookup<TaskDetail>(await tasks.GetAsync(context.UserId, id, cancellationToken), null);
            }
            catch (NotFoundException)
            {
                return new Lookup<TaskDetail>(null, "I could not find that task.");
            }
        }

        var hit = await HitAsync(context, query, SearchEntity.Task, "task", cancellationToken);
        return hit.Value is null
            ? new Lookup<TaskDetail>(null, hit.Question)
            : new Lookup<TaskDetail>(await tasks.GetAsync(context.UserId, hit.Value.Id, cancellationToken), null);
    }

    public async Task<Lookup<LearningTopicItem>> LearningTopicAsync(ToolContext context, string query, CancellationToken cancellationToken)
    {
        var topics = await learning.ListAsync(context.UserId, null, cancellationToken);
        return Pick(NameMatch.Find(topics, topic => topic.Id, topic => topic.Name, query), "learning topic", query, topic => topic.Name);
    }

    /// <summary>Finds one record of a searchable kind by id or title, through permission-filtered workspace search.</summary>
    public async Task<Lookup<SearchHit>> HitAsync(ToolContext context, string query, string entity, string kind, CancellationToken cancellationToken)
    {
        var hits = await SearchAsync(context, query, [entity], 6, cancellationToken);
        if (Guid.TryParse(query.Trim(), out var id))
        {
            var byId = hits.FirstOrDefault(hit => hit.Id == id);
            return byId is null ? new Lookup<SearchHit>(null, $"I could not find that {kind}.") : new Lookup<SearchHit>(byId, null);
        }

        var match = NameMatch.Find(hits, hit => hit.Id, hit => hit.Title, query);
        if (match.Item is not null)
        {
            return new Lookup<SearchHit>(match.Item, null);
        }

        if (!match.Ambiguous && hits.Count == 1)
        {
            return new Lookup<SearchHit>(hits[0], null);
        }

        var options = match.Ambiguous ? match.Candidates : hits;
        return options.Count > 1
            ? new Lookup<SearchHit>(null, $"Which {kind} do you mean: {ToolFormat.Join(options.Take(5).Select(Describe))}?")
            : new Lookup<SearchHit>(null, $"I could not find a {kind} matching \"{query.Trim()}\".");
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(ToolContext context, string query, IReadOnlyCollection<string> entities, int limit, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(query.Trim(), out _))
        {
            return [];
        }

        var groups = await search.SearchAsync(context.UserId, new SearchRequest(query.Trim(), entities, limit), cancellationToken);
        return groups.SelectMany(group => group.Hits).ToArray();
    }

    private async Task<WorkOptions> OptionsAsync(ToolContext context, CancellationToken cancellationToken)
    {
        return _options ??= await tasks.OptionsAsync(context.UserId, cancellationToken);
    }

    private static Lookup<T> Pick<T>(Match<T> match, string kind, string query, Func<T, string> label)
        where T : class
    {
        if (match.Item is not null)
        {
            return new Lookup<T>(match.Item, null);
        }

        return match.Ambiguous
            ? new Lookup<T>(null, $"Which {kind} do you mean: {ToolFormat.Join(match.Candidates.Take(5).Select(label))}?")
            : new Lookup<T>(null, $"I could not find a {kind} called \"{query.Trim()}\".");
    }

    private static string Describe(SearchHit hit) => string.IsNullOrWhiteSpace(hit.Detail) ? hit.Title : $"{hit.Title} ({hit.Detail})";
}

public static class ToolFormat
{
    public static string Join(IEnumerable<string> values)
    {
        var list = values.ToArray();
        return list.Length <= 1 ? string.Concat(list) : $"{string.Join(", ", list[..^1])} or {list[^1]}";
    }

    /// <summary>A timestamp in the person's own time zone, which is how the model should talk about it.</summary>
    public static string? Local(ToolContext context, DateTimeOffset? value)
    {
        return value?.ToOffset(context.Offset).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    public static string When(ToolContext context, DateTimeOffset value)
    {
        var local = value.ToOffset(context.Offset);
        return $"{AssistantDates.Format(DateOnly.FromDateTime(local.DateTime))} at {local:HH:mm}";
    }

    public static TEnum? Parse<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        var compact = value?.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        return Enum.TryParse<TEnum>(compact, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) && !int.TryParse(compact, out _) ? parsed : null;
    }

    public static string[] Names<TEnum>()
        where TEnum : struct, Enum => Enum.GetNames<TEnum>();
}

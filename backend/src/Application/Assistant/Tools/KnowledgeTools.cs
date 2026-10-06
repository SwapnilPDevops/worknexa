using System.Text.Json.Nodes;
using Worknexa.Application.Common;
using Worknexa.Application.Knowledge;
using Worknexa.Application.Learning;
using Worknexa.Application.Meetings;
using Worknexa.Application.Search;
using Worknexa.Domain.Identity;

namespace Worknexa.Application.Assistant.Tools;

public sealed record SearchNotesArgs(string? Query);

public sealed class SearchNotesTool(AssistantLookup lookup) : ReadTool<SearchNotesArgs>
{
    private static readonly string[] Entities = [SearchEntity.Note, SearchEntity.LearningNote, SearchEntity.MeetingNote];

    public override string Name => "searchNotes";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.NotesRead];

    public override JsonObject Parameters => ToolSchema.Object(["query"], ("query", ToolSchema.Text("Words to look for in note titles and content.")));

    protected override async Task<object> RunAsync(ToolContext context, SearchNotesArgs args, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.Query))
        {
            return new { status = "needs_input", question = "What should I look for?" };
        }

        var hits = await lookup.SearchAsync(context, args.Query, Entities, 8, cancellationToken);
        return new
        {
            count = hits.Count,
            notes = hits.Select(hit => new { kind = SearchEntity.Label(hit.Entity), hit.Title, hit.Detail })
        };
    }
}

public sealed record CreateMeetingNoteArgs(string? Meeting, string? Discussion, string? Decisions, string? FollowUps);

public sealed record MeetingNoteAddition(Guid MeetingId, string? Discussion, string? Decisions, string? FollowUps);

/// <summary>Adds to a meeting's notes. Existing text and action items are always kept.</summary>
public sealed class CreateMeetingNoteTool(IMeetingNoteService meetingNotes, AssistantLookup lookup) : ActionTool<CreateMeetingNoteArgs, MeetingNoteAddition>
{
    public override string Name => "createMeetingNote";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.MeetingsRead];

    public override JsonObject Parameters => ToolSchema.Object(
        ["meeting"],
        ("meeting", ToolSchema.Text("The meeting's title, or its id from an earlier tool result.")),
        ("discussion", ToolSchema.Text("Discussion points to add.")),
        ("decisions", ToolSchema.Text("Decisions to add.")),
        ("followUps", ToolSchema.Text("Follow-ups to add.")));

    protected override async Task<Planned<MeetingNoteAddition>> PlanAsync(ToolContext context, CreateMeetingNoteArgs args, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.Meeting))
        {
            return Planned<MeetingNoteAddition>.Ask(["Which meeting are the notes for?"]);
        }

        Guid meetingId;
        if (Guid.TryParse(args.Meeting.Trim(), out var id))
        {
            meetingId = id;
        }
        else
        {
            var hit = await lookup.HitAsync(context, args.Meeting, SearchEntity.Meeting, "meeting", cancellationToken);
            if (hit.Value is null)
            {
                return Planned<MeetingNoteAddition>.Ask([hit.Question!]);
            }

            meetingId = hit.Value.Id;
        }

        MeetingNoteDocument document;
        try
        {
            document = await meetingNotes.GetAsync(context.UserId, meetingId, cancellationToken);
        }
        catch (NotFoundException)
        {
            return Planned<MeetingNoteAddition>.Ask(["I could not find notes for that meeting."]);
        }

        var addition = new MeetingNoteAddition(document.Id, Clean(args.Discussion), Clean(args.Decisions), Clean(args.FollowUps));
        if (addition is { Discussion: null, Decisions: null, FollowUps: null })
        {
            return Planned<MeetingNoteAddition>.Ask([$"What should I add to the notes for \"{document.Title}\"?"]);
        }

        var fields = new List<IntentField> { new("Meeting", $"{document.Title} ({ToolFormat.When(context, document.StartAt)})") };
        if (addition.Discussion is not null)
        {
            fields.Add(new IntentField("Discussion", addition.Discussion));
        }

        if (addition.Decisions is not null)
        {
            fields.Add(new IntentField("Decisions", addition.Decisions));
        }

        if (addition.FollowUps is not null)
        {
            fields.Add(new IntentField("Follow-ups", addition.FollowUps));
        }

        return new Planned<MeetingNoteAddition>($"Add to the notes for \"{document.Title}\". Existing notes stay as they are.", fields, addition, []);
    }

    protected override async Task<ActionOutcome> ExecuteAsync(ToolContext context, MeetingNoteAddition payload, CancellationToken cancellationToken)
    {
        var current = await meetingNotes.GetAsync(context.UserId, payload.MeetingId, cancellationToken);
        var saved = await meetingNotes.SaveAsync(context.UserId, current.Id, new MeetingNoteWrite(
            current.Agenda,
            Append(current.Discussion, payload.Discussion),
            Append(current.Decisions, payload.Decisions),
            Append(current.FollowUps, payload.FollowUps),
            current.FollowUpAt,
            current.Actions.Select(action => new MeetingNoteActionWrite(action.Id, action.Title, action.Description, action.AssigneeId, action.DueAt, action.IsCompleted)).ToArray()), cancellationToken);
        return new ActionOutcome($"Added to the notes for \"{saved.Title}\".", $"/meeting-notes?meeting={saved.Id}");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Append(string? existing, string? addition)
    {
        if (addition is null)
        {
            return existing;
        }

        return string.IsNullOrWhiteSpace(existing) ? addition : $"{existing.TrimEnd()}\n\n{addition}";
    }
}

public sealed record CreateLearningNoteArgs(string? Topic, string? Title, string? Content);

public sealed class CreateLearningNoteTool(INoteService notes, AssistantLookup lookup) : ActionTool<CreateLearningNoteArgs, NoteWrite>
{
    private const string Category = "Learning";

    public override string Name => "createLearningNote";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.NotesManage, PermissionCatalog.LearningRead];

    public override JsonObject Parameters => ToolSchema.Object(
        ["topic", "title", "content"],
        ("topic", ToolSchema.Text("The learning topic's name.")),
        ("title", ToolSchema.Text("Note title.")),
        ("content", ToolSchema.Text("Note text. Markdown is allowed.")));

    protected override async Task<Planned<NoteWrite>> PlanAsync(ToolContext context, CreateLearningNoteArgs args, CancellationToken cancellationToken)
    {
        var questions = new List<string>();
        LearningTopicItem? topic = null;
        if (string.IsNullOrWhiteSpace(args.Topic))
        {
            questions.Add("Which learning topic is this note for?");
        }
        else
        {
            var found = await lookup.LearningTopicAsync(context, args.Topic, cancellationToken);
            topic = found.Value;
            CreateTaskTool.Collect(questions, found.Question);
        }

        var title = args.Title?.Trim();
        var content = args.Content?.Trim();
        if (string.IsNullOrEmpty(content))
        {
            questions.Add("What should the note say?");
        }

        if (questions.Count > 0)
        {
            return Planned<NoteWrite>.Ask(questions);
        }

        title = string.IsNullOrEmpty(title) ? topic!.Name : title;
        var request = new NoteWrite(title, content!, Category, false, false, null, null, null, topic!.Id, null);
        IReadOnlyList<IntentField> fields = [new("Topic", topic.Name), new("Title", title), new("Note", content!)];
        return new Planned<NoteWrite>($"Add learning note \"{title}\" to {topic.Name}.", fields, request, []);
    }

    protected override async Task<ActionOutcome> ExecuteAsync(ToolContext context, NoteWrite payload, CancellationToken cancellationToken)
    {
        var created = await notes.CreateAsync(context.UserId, payload, cancellationToken);
        return new ActionOutcome($"Added learning note \"{created.Title}\".", $"/learning-notes?note={created.Id}");
    }
}

public sealed record NoArgs;

public sealed class GetLearningProgressTool(ILearningService learning) : ReadTool<NoArgs>
{
    public override string Name => "getLearningProgress";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.LearningRead];

    public override JsonObject Parameters => ToolSchema.Object([]);

    protected override async Task<object> RunAsync(ToolContext context, NoArgs args, CancellationToken cancellationToken)
    {
        var dashboard = await learning.DashboardAsync(context.UserId, context.Today, cancellationToken);
        var topics = await learning.ListAsync(context.UserId, null, cancellationToken);
        return new
        {
            overallProgress = dashboard.Progress,
            topicsByStatus = topics.GroupBy(topic => topic.Status).ToDictionary(group => group.Key, group => group.Count()),
            current = dashboard.CurrentTopics.Select(topic => new { topic.Name, topic.Status, topic.Progress, target = topic.TargetDate }),
            upcomingTargets = dashboard.UpcomingTargets.Select(item => new { item.Name, target = item.TargetDate, item.Progress }),
            goals = dashboard.Goals.Select(goal => new { goal.Name, goal.Goal })
        };
    }
}

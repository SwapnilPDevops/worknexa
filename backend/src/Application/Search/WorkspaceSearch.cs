namespace Worknexa.Application.Search;

public static class SearchEntity
{
    public const string Project = "Project";

    public const string Task = "Task";

    public const string Meeting = "Meeting";

    public const string Note = "Note";

    public const string Person = "Person";

    public const string Todo = "Todo";

    public const string MeetingNote = "MeetingNote";

    public const string Standup = "Standup";

    public const string LearningTopic = "LearningTopic";

    public const string LearningNote = "LearningNote";

    public const string CalendarEvent = "CalendarEvent";

    public static readonly IReadOnlyList<string> Order =
    [
        Project,
        Task,
        Meeting,
        Note,
        Person,
        Todo,
        MeetingNote,
        Standup,
        LearningTopic,
        LearningNote,
        CalendarEvent
    ];

    public static string Label(string entity)
    {
        return entity switch
        {
            Project => "Projects",
            Task => "Tasks",
            Meeting => "Meetings",
            Note => "Notes",
            Person => "People",
            Todo => "Todo",
            MeetingNote => "Meeting notes",
            Standup => "Standups",
            LearningTopic => "Learning",
            LearningNote => "Learning notes",
            CalendarEvent => "Calendar",
            _ => entity
        };
    }
}

public sealed record SearchRequest(string Query, IReadOnlyCollection<string>? Entities = null, int Limit = 5);

public sealed record SearchHit(string Entity, Guid Id, string Title, string? Detail, string Route);

public sealed record SearchGroup(string Entity, string Label, IReadOnlyList<SearchHit> Hits);

public interface IWorkspaceSearch
{
    Task<IReadOnlyList<SearchGroup>> SearchAsync(Guid userId, SearchRequest request, CancellationToken cancellationToken);
}

using System.Text.Json.Nodes;
using Worknexa.Application.Calendar;
using Worknexa.Application.Dashboard;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;

namespace Worknexa.Application.Assistant.Tools;

public sealed record DayArgs(string? Date);

public sealed class GetMyDayTool(IMyDayService myDay) : ReadTool<DayArgs>
{
    private const int Limit = 8;

    public override string Name => "getMyDay";

    public override IReadOnlyList<string> Permissions { get; } = [];

    public override JsonObject Parameters => ToolSchema.Object([], ("date", ToolSchema.Text("Day to summarise as yyyy-MM-dd or a word such as \"tomorrow\". Defaults to today.")));

    protected override async Task<object> RunAsync(ToolContext context, DayArgs args, CancellationToken cancellationToken)
    {
        var date = AssistantDates.Resolve(args.Date, context.Today) ?? context.Today;
        var day = await myDay.GetAsync(context.UserId, context.Permissions, date, context.OffsetMinutes, cancellationToken);
        object Tasks(IEnumerable<MyDayTaskItem> items) => items.Take(Limit).Select(task => new { task.Title, task.Kind, task.Priority, due = ToolFormat.Local(context, task.DueAt), project = task.ProjectName });
        object Team(IEnumerable<MyDayTeamItem> items) => items.Take(Limit).Select(item => new { item.Title, assignee = item.AssigneeName, project = item.ProjectName });
        return new
        {
            date = date.ToString("yyyy-MM-dd"),
            day.Summary,
            day.Counts,
            timeline = day.Timeline.Take(Limit * 2).Select(item => new { item.Kind, item.Title, at = ToolFormat.Local(context, item.At), until = ToolFormat.Local(context, item.EndAt) }),
            tasks = new { overdue = Tasks(day.Tasks.Overdue), highPriority = Tasks(day.Tasks.HighPriority), today = Tasks(day.Tasks.Today), upcoming = Tasks(day.Tasks.Upcoming) },
            meetings = day.Meetings.Today.Select(meeting => new { meeting.Title, start = ToolFormat.Local(context, meeting.StartAt), end = ToolFormat.Local(context, meeting.EndAt), project = meeting.ProjectName, meeting.Participants }),
            team = new { blocked = Team(day.Team.Blocked), waitingForReview = Team(day.Team.WaitingForReview), needsOwner = Team(day.Team.NeedsOwner) },
            projectsAtRisk = day.Projects.AtRisk.Select(project => new { project.Name, project.Progress, project.Deadline }),
            day.Learning
        };
    }
}

public sealed record RangeArgs(string? From, string? To);

public sealed class GetCalendarTool(ICalendarService calendar) : ReadTool<RangeArgs>
{
    private const int MaxDays = 31;
    private const int Limit = 60;

    public override string Name => "getCalendar";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.CalendarRead];

    public override JsonObject Parameters => ToolSchema.Object(
        [],
        ("from", ToolSchema.Text("First day as yyyy-MM-dd or a word such as \"today\". Defaults to today.")),
        ("to", ToolSchema.Text("Last day, inclusive. Defaults to six days after the first day.")));

    protected override async Task<object> RunAsync(ToolContext context, RangeArgs args, CancellationToken cancellationToken)
    {
        var from = AssistantDates.Resolve(args.From, context.Today) ?? context.Today;
        var to = AssistantDates.Resolve(args.To, context.Today) ?? from.AddDays(6);
        if (to < from)
        {
            (from, to) = (to, from);
        }

        var clipped = to.DayNumber - from.DayNumber + 1 > MaxDays;
        if (clipped)
        {
            to = from.AddDays(MaxDays - 1);
        }

        var entries = await calendar.ListAsync(
            new CalendarRead(
                context.UserId,
                context.At(from, TimeOnly.MinValue),
                context.At(to.AddDays(1), TimeOnly.MinValue),
                context.Can(PermissionCatalog.TasksRead),
                context.Can(PermissionCatalog.MeetingsRead)),
            cancellationToken);
        return new
        {
            from = from.ToString("yyyy-MM-dd"),
            to = to.ToString("yyyy-MM-dd"),
            note = clipped ? $"Only the first {MaxDays} days are shown." : null,
            count = entries.Count,
            entries = entries.OrderBy(entry => entry.StartAt).Take(Limit).Select(entry => new
            {
                entry.Kind,
                entry.Title,
                start = ToolFormat.Local(context, entry.StartAt),
                end = ToolFormat.Local(context, entry.EndAt),
                allDay = entry.IsAllDay,
                entry.Location,
                project = entry.ProjectName
            })
        };
    }
}

public sealed record CreateReminderArgs(string? Title, string? Date, string? Time, string? Description);

public sealed class CreateReminderTool(IReminderService reminders) : ActionTool<CreateReminderArgs, ReminderWrite>
{
    private static readonly TimeOnly Morning = new(9, 0);

    public override string Name => "createReminder";

    public override IReadOnlyList<string> Permissions { get; } = [PermissionCatalog.TasksManage];

    public override JsonObject Parameters => ToolSchema.Object(
        ["title"],
        ("title", ToolSchema.Text("What to remind the user about.")),
        ("date", ToolSchema.Text("Day as yyyy-MM-dd or a word such as \"tomorrow\".")),
        ("time", ToolSchema.Text("Time such as 15:00 or 3pm. Defaults to 09:00.")),
        ("description", ToolSchema.Text("Optional details.")));

    protected override Task<Planned<ReminderWrite>> PlanAsync(ToolContext context, CreateReminderArgs args, CancellationToken cancellationToken)
    {
        var questions = new List<string>();
        var title = args.Title?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            questions.Add("What should I remind you about?");
        }

        var date = AssistantDates.Resolve(args.Date, context.Today);
        var time = AssistantDates.ResolveTime(args.Time);
        if (!string.IsNullOrWhiteSpace(args.Date) && date is null)
        {
            questions.Add($"Which day? I could not read \"{args.Date.Trim()}\" as a date.");
        }

        if (!string.IsNullOrWhiteSpace(args.Time) && time is null)
        {
            questions.Add($"What time? I could not read \"{args.Time.Trim()}\" as a time.");
        }

        if (string.IsNullOrWhiteSpace(args.Date) && string.IsNullOrWhiteSpace(args.Time))
        {
            questions.Add("When should I remind you?");
        }

        if (questions.Count > 0)
        {
            return Task.FromResult(Planned<ReminderWrite>.Ask(questions));
        }

        var remindAt = context.At(date ?? context.Today, time ?? Morning);
        if (date is null && remindAt <= context.Now)
        {
            remindAt = remindAt.AddDays(1);
        }

        if (remindAt <= context.Now)
        {
            return Task.FromResult(Planned<ReminderWrite>.Ask(["That time has already passed. When should I remind you?"]));
        }

        var request = new ReminderWrite(title!, string.IsNullOrWhiteSpace(args.Description) ? null : args.Description.Trim(), remindAt, nameof(Recurrence.None), null, nameof(WorkPriority.Medium), null, null);
        var when = ToolFormat.When(context, remindAt);
        IReadOnlyList<IntentField> fields = [new("Reminder", title!), new("When", when)];
        return Task.FromResult(new Planned<ReminderWrite>($"Remind you \"{title}\" on {when}.", fields, request, []));
    }

    protected override async Task<ActionOutcome> ExecuteAsync(ToolContext context, ReminderWrite payload, CancellationToken cancellationToken)
    {
        var created = await reminders.CreateAsync(context.UserId, payload, cancellationToken);
        return new ActionOutcome($"Reminder \"{created.Title}\" set for {ToolFormat.When(context, created.RemindAt)}.", "/reminders");
    }
}

using FluentValidation;

namespace Worknexa.Application.Dashboard;

public static class QuickCaptureKinds
{
    public const string Task = "task";

    public const string Todo = "todo";

    public const string Meeting = "meeting";

    public const string Reminder = "reminder";

    public const string Note = "note";

    public const string TeamTask = "team-task";

    public const string LearningNote = "learning-note";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Task,
        Todo,
        Meeting,
        Reminder,
        Note,
        TeamTask,
        LearningNote
    };
}

public sealed record QuickCaptureRequest(string Kind, string Title, string? Notes, DateOnly Date, int OffsetMinutes);

public sealed class QuickCaptureRequestValidator : AbstractValidator<QuickCaptureRequest>
{
    public QuickCaptureRequestValidator()
    {
        RuleFor(request => request.Kind)
            .Must(kind => QuickCaptureKinds.All.Contains(kind))
            .WithMessage("Choose a supported quick action.");
        RuleFor(request => request.Title).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Notes).MaximumLength(4000);
        RuleFor(request => request.OffsetMinutes).InclusiveBetween(-840, 840);
    }
}

public interface IQuickCaptureStore
{
    Task CaptureAsync(Guid userId, QuickCaptureRequest request, DateTimeOffset dueAt, DateTimeOffset now, CancellationToken cancellationToken);
}

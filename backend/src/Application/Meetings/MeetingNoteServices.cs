using FluentValidation;
using Worknexa.Application.Common;
using Worknexa.Application.Work;

namespace Worknexa.Application.Meetings;

public sealed record MeetingNoteListItem(Guid Id, string Title, DateTimeOffset StartAt, DateTimeOffset UpdatedAt);

public sealed record MeetingNoteAction(
    Guid Id,
    string Title,
    string? Description,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTimeOffset? DueAt,
    bool IsCompleted,
    Guid? TaskId,
    string? TaskTitle);

public sealed record MeetingNoteDocument(
    Guid Id,
    string Title,
    DateTimeOffset StartAt,
    Guid? ProjectId,
    string? ProjectName,
    string? Agenda,
    string? Discussion,
    string? Decisions,
    string? FollowUps,
    DateTimeOffset? FollowUpAt,
    IReadOnlyList<MeetingNoteAction> Actions,
    DateTimeOffset UpdatedAt);

public sealed record MeetingNoteActionWrite(
    Guid? Id,
    string Title,
    string? Description,
    Guid? AssigneeId,
    DateTimeOffset? DueAt,
    bool IsCompleted);

public sealed record MeetingNoteWrite(
    string? Agenda,
    string? Discussion,
    string? Decisions,
    string? FollowUps,
    DateTimeOffset? FollowUpAt,
    IReadOnlyList<MeetingNoteActionWrite>? Actions);

public sealed record LinkedTask(
    Guid TaskId,
    Guid MeetingId,
    string Title,
    string? Description,
    Guid? AssigneeId,
    DateTimeOffset? DueAt,
    Guid? ProjectId);

public interface IMeetingNoteStore
{
    Task<IReadOnlyList<MeetingNoteListItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken);

    Task<MeetingNoteDocument?> FindAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken);

    Task<MeetingNoteDocument?> SaveAsync(Guid userId, Guid meetingId, MeetingNoteWrite request, CancellationToken cancellationToken);

    Task<MeetingNoteDocument?> ConvertAsync(Guid userId, Guid meetingId, Guid actionId, CancellationToken cancellationToken);
}

public interface IMeetingNoteService
{
    Task<IReadOnlyList<MeetingNoteListItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken);

    Task<MeetingNoteDocument> GetAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken);

    Task<MeetingNoteDocument> SaveAsync(Guid userId, Guid meetingId, MeetingNoteWrite request, CancellationToken cancellationToken);

    Task<MeetingNoteDocument> ConvertAsync(Guid userId, Guid meetingId, Guid actionId, CancellationToken cancellationToken);
}

public sealed class MeetingNoteService(IMeetingNoteStore store) : IMeetingNoteService
{
    public Task<IReadOnlyList<MeetingNoteListItem>> ListAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        var term = query?.Trim();
        return store.ListAsync(userId, string.IsNullOrEmpty(term) ? null : term, cancellationToken);
    }

    public async Task<MeetingNoteDocument> GetAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken)
    {
        return await store.FindAsync(userId, meetingId, cancellationToken) ?? throw new NotFoundException("Meeting notes were not found.");
    }

    public async Task<MeetingNoteDocument> SaveAsync(Guid userId, Guid meetingId, MeetingNoteWrite request, CancellationToken cancellationToken)
    {
        var write = new MeetingNoteWrite(
            Clean(request.Agenda),
            Clean(request.Discussion),
            Clean(request.Decisions),
            Clean(request.FollowUps),
            request.FollowUpAt,
            (request.Actions ?? []).Select(action => action with { Title = action.Title.Trim(), Description = Clean(action.Description) }).ToArray());
        return await store.SaveAsync(userId, meetingId, write, cancellationToken) ?? throw new NotFoundException("Meeting notes were not found.");
    }

    public async Task<MeetingNoteDocument> ConvertAsync(Guid userId, Guid meetingId, Guid actionId, CancellationToken cancellationToken)
    {
        return await store.ConvertAsync(userId, meetingId, actionId, cancellationToken) ?? throw new NotFoundException("That action item was not found.");
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}

public sealed class MeetingNoteWriteValidator : AbstractValidator<MeetingNoteWrite>
{
    public MeetingNoteWriteValidator()
    {
        RuleFor(request => request.Agenda).MaximumLength(100_000);
        RuleFor(request => request.Discussion).MaximumLength(100_000);
        RuleFor(request => request.Decisions).MaximumLength(100_000);
        RuleFor(request => request.FollowUps).MaximumLength(100_000);
        RuleFor(request => request.Actions).Must(actions => actions is null || actions.Count <= 40);
        RuleForEach(request => request.Actions).ChildRules(action =>
        {
            action.RuleFor(item => item.Title).NotEmpty().MaximumLength(300);
            action.RuleFor(item => item.Description).MaximumLength(8_000);
        });
    }
}

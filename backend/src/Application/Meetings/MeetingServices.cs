using FluentValidation;
using Worknexa.Application.Common;
using Worknexa.Application.Dashboard;
using Worknexa.Application.Validation;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;

namespace Worknexa.Application.Meetings;

public static class MeetingTime
{
    public const int MinimumMinutes = 5;

    public const int MaximumMinutes = 24 * 60;

    public static DateTimeOffset End(DateTimeOffset start, int durationMinutes)
    {
        if (durationMinutes is < MinimumMinutes or > MaximumMinutes)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("durationMinutes", "Duration must be between 5 minutes and 24 hours.")]);
        }

        return start.AddMinutes(durationMinutes);
    }

    public static int Minutes(DateTimeOffset start, DateTimeOffset end)
    {
        var minutes = (int)Math.Round((end - start).TotalMinutes);
        return minutes < MinimumMinutes ? MinimumMinutes : minutes;
    }
}

public sealed record MeetingActionItemDto(
    Guid Id,
    string Title,
    Guid? AssigneeId,
    string? AssigneeName,
    bool IsCompleted,
    DateTimeOffset? DueAt,
    string? Description,
    Guid? TaskId);

public sealed record MeetingActionWrite(
    string Title,
    Guid? AssigneeId,
    DateTimeOffset? DueAt,
    bool IsCompleted,
    Guid? Id = null,
    string? Description = null);

public sealed record MeetingListItem(
    Guid Id,
    string Title,
    string MeetingType,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int DurationMinutes,
    string? Location,
    string? MeetingLink,
    Guid? ProjectId,
    string? ProjectName,
    Guid? TeamId,
    string? TeamName,
    IReadOnlyList<NamedRef> Participants);

public sealed record MeetingDetail(
    Guid Id,
    string Title,
    string MeetingType,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int DurationMinutes,
    string? Location,
    string? MeetingLink,
    string? Agenda,
    string? Notes,
    string? Decisions,
    DateTimeOffset? FollowUpAt,
    Guid? ProjectId,
    string? ProjectName,
    Guid? TeamId,
    string? TeamName,
    IReadOnlyList<NamedRef> Participants,
    IReadOnlyList<MeetingActionItemDto> ActionItems,
    bool Editable);

public sealed record MeetingWrite(
    string Title,
    string MeetingType,
    DateTimeOffset StartAt,
    int DurationMinutes,
    Guid? ProjectId,
    Guid? TeamId,
    string? Location,
    string? MeetingLink,
    string? Agenda,
    string? Notes,
    string? Decisions,
    DateTimeOffset? FollowUpAt,
    IReadOnlyList<Guid>? ParticipantIds,
    IReadOnlyList<MeetingActionWrite>? ActionItems);

public sealed record StandupLine(
    Guid? EntryId,
    Guid UserId,
    string MemberName,
    string? Yesterday,
    string? Today,
    string? Blockers,
    string? Notes,
    string Status,
    bool Mine);

public sealed record StandupBoard(
    DateOnly Date,
    Guid? StandupId,
    Guid? TeamId,
    string? TeamName,
    Guid? MeetingId,
    string? MeetingTitle,
    IReadOnlyList<StandupLine> Lines);

public sealed record StandupWrite(
    DateOnly Date,
    Guid? TeamId,
    string? Yesterday,
    string? Today,
    string? Blockers,
    string? Notes,
    string Status);

public sealed record StandupHit(
    DateOnly Date,
    string MemberName,
    string? Yesterday,
    string? Today,
    string? Blockers,
    string? Notes,
    string Status,
    string? TeamName);

public interface IMeetingStore
{
    Task<IReadOnlyList<MeetingListItem>> ListAsync(Guid userId, string view, DateOnly date, int offsetMinutes, CancellationToken cancellationToken);

    Task<MeetingDetail?> FindAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken);

    Task<MeetingDetail> CreateAsync(Guid userId, MeetingWrite request, MeetingType meetingType, DateTimeOffset end, CancellationToken cancellationToken);

    Task<MeetingDetail?> UpdateAsync(Guid userId, Guid meetingId, MeetingWrite request, MeetingType meetingType, DateTimeOffset end, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken);
}

public interface IStandupStore
{
    Task<StandupBoard> BoardAsync(Guid userId, DateOnly date, Guid? teamId, CancellationToken cancellationToken);

    Task<StandupBoard> SaveAsync(Guid userId, StandupWrite request, StandupStatus status, CancellationToken cancellationToken);

    Task<IReadOnlyList<StandupHit>> SearchAsync(Guid userId, string? query, CancellationToken cancellationToken);

    Task LinkMeetingAsync(Guid userId, Guid? teamId, DateOnly date, Guid meetingId, string meetingTitle, CancellationToken cancellationToken);
}

public interface IMeetingService
{
    Task<IReadOnlyList<MeetingListItem>> ListAsync(Guid userId, string? view, DateOnly date, int offsetMinutes, CancellationToken cancellationToken);

    Task<MeetingDetail> GetAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken);

    Task<MeetingDetail> CreateAsync(Guid userId, MeetingWrite request, CancellationToken cancellationToken);

    Task<MeetingDetail> UpdateAsync(Guid userId, Guid meetingId, MeetingWrite request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken);
}

public interface IStandupService
{
    Task<StandupBoard> BoardAsync(Guid userId, DateOnly date, Guid? teamId, CancellationToken cancellationToken);

    Task<StandupBoard> SaveAsync(Guid userId, StandupWrite request, CancellationToken cancellationToken);

    Task<IReadOnlyList<StandupHit>> SearchAsync(Guid userId, string? query, CancellationToken cancellationToken);
}

public sealed class MeetingService(IMeetingStore store, IStandupStore standups) : IMeetingService
{
    public Task<IReadOnlyList<MeetingListItem>> ListAsync(Guid userId, string? view, DateOnly date, int offsetMinutes, CancellationToken cancellationToken)
    {
        Window(date, offsetMinutes);
        var selected = view is "today" or "upcoming" or "past" or "calendar" ? view : "upcoming";
        return store.ListAsync(userId, selected, date, offsetMinutes, cancellationToken);
    }

    public async Task<MeetingDetail> GetAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken)
    {
        return await store.FindAsync(userId, meetingId, cancellationToken) ?? throw new NotFoundException("Meeting was not found.");
    }

    public async Task<MeetingDetail> CreateAsync(Guid userId, MeetingWrite request, CancellationToken cancellationToken)
    {
        var type = TaskService.ParseRequired<MeetingType>(request.MeetingType, "meetingType");
        var end = MeetingTime.End(request.StartAt, request.DurationMinutes);
        var detail = await store.CreateAsync(userId, Normalize(request), type, end, cancellationToken);
        await LinkStandupAsync(userId, detail, cancellationToken);
        return detail;
    }

    public async Task<MeetingDetail> UpdateAsync(Guid userId, Guid meetingId, MeetingWrite request, CancellationToken cancellationToken)
    {
        var type = TaskService.ParseRequired<MeetingType>(request.MeetingType, "meetingType");
        var end = MeetingTime.End(request.StartAt, request.DurationMinutes);
        var detail = await store.UpdateAsync(userId, meetingId, Normalize(request), type, end, cancellationToken)
            ?? throw new NotFoundException("Meeting was not found.");
        await LinkStandupAsync(userId, detail, cancellationToken);
        return detail;
    }

    public async Task DeleteAsync(Guid userId, Guid meetingId, CancellationToken cancellationToken)
    {
        if (!await store.DeleteAsync(userId, meetingId, cancellationToken))
        {
            throw new NotFoundException("Meeting was not found.");
        }
    }

    private async Task LinkStandupAsync(Guid userId, MeetingDetail detail, CancellationToken cancellationToken)
    {
        if (detail.MeetingType != nameof(MeetingType.Standup))
        {
            return;
        }

        var local = detail.StartAt.ToOffset(detail.StartAt.Offset);
        await standups.LinkMeetingAsync(userId, detail.TeamId, DateOnly.FromDateTime(local.DateTime), detail.Id, detail.Title, cancellationToken);
    }

    private static MeetingWrite Normalize(MeetingWrite request)
    {
        return request with
        {
            Title = request.Title.Trim(),
            Location = Blank(request.Location),
            MeetingLink = Blank(request.MeetingLink),
            Agenda = Blank(request.Agenda),
            Notes = Blank(request.Notes),
            Decisions = Blank(request.Decisions),
            ParticipantIds = request.ParticipantIds?.Distinct().Take(24).ToArray() ?? [],
            ActionItems = request.ActionItems?
                .Where(item => !string.IsNullOrWhiteSpace(item.Title))
                .Take(20)
                .Select(item => item with { Title = item.Title.Trim() })
                .ToArray() ?? []
        };
    }

    private static string? Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static MyDayWindow Window(DateOnly date, int offsetMinutes)
    {
        if (offsetMinutes is < -840 or > 840)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("offsetMinutes", "Offset is out of range.")]);
        }

        return MyDayWindow.Create(date, offsetMinutes);
    }
}

public sealed class StandupService(IStandupStore store) : IStandupService
{
    public Task<StandupBoard> BoardAsync(Guid userId, DateOnly date, Guid? teamId, CancellationToken cancellationToken)
    {
        return store.BoardAsync(userId, date, teamId, cancellationToken);
    }

    public Task<StandupBoard> SaveAsync(Guid userId, StandupWrite request, CancellationToken cancellationToken)
    {
        var status = TaskService.ParseRequired<StandupStatus>(request.Status, "status");
        return store.SaveAsync(userId, request with
        {
            Yesterday = Blank(request.Yesterday),
            Today = Blank(request.Today),
            Blockers = Blank(request.Blockers),
            Notes = Blank(request.Notes)
        }, status, cancellationToken);
    }

    public Task<IReadOnlyList<StandupHit>> SearchAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        var text = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        return store.SearchAsync(userId, text, cancellationToken);
    }

    private static string? Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class MeetingWriteValidator : AbstractValidator<MeetingWrite>
{
    public MeetingWriteValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(300);
        RuleFor(request => request.MeetingType).NotEmpty();
        RuleFor(request => request.DurationMinutes).InclusiveBetween(MeetingTime.MinimumMinutes, MeetingTime.MaximumMinutes);
        RuleFor(request => request.Location).MaximumLength(300);
        RuleFor(request => request.MeetingLink).MaximumLength(500).WebUrl();
        RuleFor(request => request.Agenda).MaximumLength(8000);
        RuleFor(request => request.Notes).MaximumLength(8000);
        RuleFor(request => request.Decisions).MaximumLength(8000);
    }
}

public sealed class StandupWriteValidator : AbstractValidator<StandupWrite>
{
    public StandupWriteValidator()
    {
        RuleFor(request => request.Status).NotEmpty();
        RuleFor(request => request.Yesterday).MaximumLength(4000);
        RuleFor(request => request.Today).MaximumLength(4000);
        RuleFor(request => request.Blockers).MaximumLength(4000);
        RuleFor(request => request.Notes).MaximumLength(4000);
    }
}

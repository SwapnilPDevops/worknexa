using FluentValidation;
using Worknexa.Application.Common;
using Worknexa.Application.Validation;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;

namespace Worknexa.Application.Organization;

public sealed record TeamListItem(Guid Id, string Name, string? Description, int MemberCount);

public sealed record TeamMemberItem(Guid UserId, string Name, string Email, string Role, string Status, string? Designation);

public sealed record TeamDetail(Guid Id, string Name, string? Description, IReadOnlyList<TeamMemberItem> Members);

public sealed record TeamWrite(string Name, string? Description);

public sealed record TeamMembershipWrite(Guid UserId, string Role);

public sealed record NamedPerson(Guid Id, string Name, string? Designation);

public sealed record TeamOptions(IReadOnlyList<TeamListItem> Teams, IReadOnlyList<NamedPerson> People);

public sealed record MemberListItem(
    Guid Id,
    string Name,
    string Email,
    string? PhotoUrl,
    string? Designation,
    string? Department,
    string Status);

public sealed record MemberTeamItem(Guid TeamId, string Name, string Role);

public sealed record MemberProfile(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string? PhotoUrl,
    string? Designation,
    string? Department,
    DateOnly? JoinedOn,
    Guid? ManagerId,
    string? ManagerName,
    string Status,
    string? Notes,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Technologies,
    IReadOnlyList<MemberTeamItem> Teams);

public sealed record MemberWrite(
    string Name,
    string Email,
    string? Phone,
    string? PhotoUrl,
    string? Designation,
    string? Department,
    DateOnly? JoinedOn,
    Guid? ManagerId,
    string Status,
    string? Notes,
    IReadOnlyList<string>? Skills,
    IReadOnlyList<string>? Technologies);

public sealed record MemberProjectItem(Guid Id, string Name, string Code, string Status, string Role);

public sealed record MemberStandupItem(
    Guid Id,
    DateOnly StandupDate,
    string? TeamName,
    string Status,
    string? Yesterday,
    string? Today,
    string? Blockers);

public sealed record MemberActivityItem(string Action, string Summary, DateTimeOffset OccurredAt);

public sealed record TeamWorkloadRow(
    Guid UserId,
    string Name,
    int Assigned,
    int InProgress,
    int Completed,
    int Overdue,
    int Blocked);

public sealed record TeamDraft(string Name, string? Description);

public sealed record MemberDraft(
    string Name,
    string Email,
    string? Phone,
    string? PhotoUrl,
    string? Designation,
    string? Department,
    DateOnly? JoinedOn,
    Guid? ManagerId,
    MemberStatus Status,
    string? Notes,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Technologies);

public interface ITeamStore
{
    Task<IReadOnlyList<TeamListItem>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<TeamOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<TeamDetail?> FindAsync(Guid userId, Guid teamId, CancellationToken cancellationToken);

    Task<TeamDetail> CreateAsync(Guid userId, TeamDraft draft, CancellationToken cancellationToken);

    Task<TeamDetail?> UpdateAsync(Guid userId, Guid teamId, TeamDraft draft, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, Guid teamId, CancellationToken cancellationToken);

    Task<TeamDetail?> AddMemberAsync(Guid userId, Guid teamId, Guid memberUserId, TeamMemberRole role, CancellationToken cancellationToken);

    Task<TeamDetail?> RemoveMemberAsync(Guid userId, Guid teamId, Guid memberUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberListItem>> MembersAsync(Guid userId, string? query, CancellationToken cancellationToken);

    Task<MemberProfile?> FindMemberAsync(Guid userId, Guid memberId, CancellationToken cancellationToken);

    Task<MemberProfile?> UpdateMemberAsync(Guid userId, Guid memberId, MemberDraft draft, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberProjectItem>> ProjectsAsync(Guid userId, Guid memberId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberStandupItem>> StandupsAsync(Guid userId, Guid memberId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberActivityItem>> ActivityAsync(Guid userId, Guid memberId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeamWorkloadRow>?> WorkloadAsync(Guid userId, Guid teamId, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface ITeamService
{
    Task<IReadOnlyList<TeamListItem>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<TeamOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<TeamDetail> GetAsync(Guid userId, Guid teamId, CancellationToken cancellationToken);

    Task<TeamDetail> CreateAsync(Guid userId, TeamWrite request, CancellationToken cancellationToken);

    Task<TeamDetail> UpdateAsync(Guid userId, Guid teamId, TeamWrite request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid teamId, CancellationToken cancellationToken);

    Task<TeamDetail> AddMemberAsync(Guid userId, Guid teamId, TeamMembershipWrite request, CancellationToken cancellationToken);

    Task<TeamDetail> RemoveMemberAsync(Guid userId, Guid teamId, Guid memberUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberListItem>> MembersAsync(Guid userId, string? query, CancellationToken cancellationToken);

    Task<MemberProfile> GetMemberAsync(Guid userId, Guid memberId, CancellationToken cancellationToken);

    Task<MemberProfile> UpdateMemberAsync(Guid userId, Guid memberId, MemberWrite request, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberProjectItem>> ProjectsAsync(Guid userId, Guid memberId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberStandupItem>> StandupsAsync(Guid userId, Guid memberId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberActivityItem>> ActivityAsync(Guid userId, Guid memberId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeamWorkloadRow>> WorkloadAsync(Guid userId, Guid teamId, CancellationToken cancellationToken);
}

public sealed class TeamService(ITeamStore store) : ITeamService
{
    public Task<IReadOnlyList<TeamListItem>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return store.ListAsync(userId, cancellationToken);
    }

    public Task<TeamOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return store.OptionsAsync(userId, cancellationToken);
    }

    public async Task<TeamDetail> GetAsync(Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        return await store.FindAsync(userId, teamId, cancellationToken) ?? throw new NotFoundException("Team was not found.");
    }

    public Task<TeamDetail> CreateAsync(Guid userId, TeamWrite request, CancellationToken cancellationToken)
    {
        return store.CreateAsync(userId, new TeamDraft(request.Name.Trim(), Clean(request.Description)), cancellationToken);
    }

    public async Task<TeamDetail> UpdateAsync(Guid userId, Guid teamId, TeamWrite request, CancellationToken cancellationToken)
    {
        return await store.UpdateAsync(userId, teamId, new TeamDraft(request.Name.Trim(), Clean(request.Description)), cancellationToken)
            ?? throw new NotFoundException("Team was not found.");
    }

    public async Task DeleteAsync(Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        if (!await store.DeleteAsync(userId, teamId, cancellationToken))
        {
            throw new NotFoundException("Team was not found.");
        }
    }

    public async Task<TeamDetail> AddMemberAsync(Guid userId, Guid teamId, TeamMembershipWrite request, CancellationToken cancellationToken)
    {
        var role = TaskService.ParseRequired<TeamMemberRole>(request.Role, "role");
        return await store.AddMemberAsync(userId, teamId, request.UserId, role, cancellationToken)
            ?? throw new NotFoundException("Team was not found.");
    }

    public async Task<TeamDetail> RemoveMemberAsync(Guid userId, Guid teamId, Guid memberUserId, CancellationToken cancellationToken)
    {
        return await store.RemoveMemberAsync(userId, teamId, memberUserId, cancellationToken)
            ?? throw new NotFoundException("Team was not found.");
    }

    public Task<IReadOnlyList<MemberListItem>> MembersAsync(Guid userId, string? query, CancellationToken cancellationToken)
    {
        return store.MembersAsync(userId, string.IsNullOrWhiteSpace(query) ? null : query.Trim(), cancellationToken);
    }

    public async Task<MemberProfile> GetMemberAsync(Guid userId, Guid memberId, CancellationToken cancellationToken)
    {
        return await store.FindMemberAsync(userId, memberId, cancellationToken) ?? throw new NotFoundException("Team member was not found.");
    }

    public async Task<MemberProfile> UpdateMemberAsync(Guid userId, Guid memberId, MemberWrite request, CancellationToken cancellationToken)
    {
        if (request.ManagerId == memberId)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("managerId", "A person cannot be their own manager.")]);
        }

        var draft = new MemberDraft(
            request.Name.Trim(),
            request.Email.Trim().ToLowerInvariant(),
            Clean(request.Phone),
            Url(request.PhotoUrl, "photoUrl"),
            Clean(request.Designation),
            Clean(request.Department),
            request.JoinedOn,
            request.ManagerId,
            TaskService.ParseRequired<MemberStatus>(request.Status, "status"),
            Clean(request.Notes),
            Names(request.Skills, "skills"),
            Names(request.Technologies, "technologies"));
        return await store.UpdateMemberAsync(userId, memberId, draft, cancellationToken)
            ?? throw new NotFoundException("Team member was not found.");
    }

    public Task<IReadOnlyList<MemberProjectItem>> ProjectsAsync(Guid userId, Guid memberId, CancellationToken cancellationToken)
    {
        return store.ProjectsAsync(userId, memberId, cancellationToken);
    }

    public Task<IReadOnlyList<MemberStandupItem>> StandupsAsync(Guid userId, Guid memberId, CancellationToken cancellationToken)
    {
        return store.StandupsAsync(userId, memberId, cancellationToken);
    }

    public Task<IReadOnlyList<MemberActivityItem>> ActivityAsync(Guid userId, Guid memberId, CancellationToken cancellationToken)
    {
        return store.ActivityAsync(userId, memberId, cancellationToken);
    }

    public async Task<IReadOnlyList<TeamWorkloadRow>> WorkloadAsync(Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        return await store.WorkloadAsync(userId, teamId, DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new NotFoundException("Team was not found.");
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string? Url(string? value, string field)
    {
        var trimmed = Clean(value);
        if (trimmed is null)
        {
            return null;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(field, "Use an http or https URL.")]);
        }

        return trimmed;
    }

    private static IReadOnlyList<string> Names(IReadOnlyList<string>? values, string field)
    {
        var names = (values ?? [])
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (names.Length > 30 || names.Any(name => name.Length > 80))
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(field, "Use up to 30 names, each 80 characters or fewer.")]);
        }

        return names;
    }
}

public static class TeamWorkload
{
    public static IReadOnlyList<TeamWorkloadRow> Build(
        IReadOnlyList<(Guid UserId, string Name)> members,
        IReadOnlyList<(Guid? AssigneeId, WorkTaskStatus Status, DateTimeOffset? DueAt)> tasks,
        DateTimeOffset now)
    {
        return members
            .OrderBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
            .Select(member =>
            {
                var owned = tasks.Where(task => task.AssigneeId == member.UserId && task.Status != WorkTaskStatus.Cancelled).ToArray();
                return new TeamWorkloadRow(
                    member.UserId,
                    member.Name,
                    owned.Length,
                    owned.Count(task => task.Status == WorkTaskStatus.InProgress),
                    owned.Count(task => task.Status == WorkTaskStatus.Completed),
                    owned.Count(task => task.Status != WorkTaskStatus.Completed && task.DueAt is not null && task.DueAt < now),
                    owned.Count(task => task.Status == WorkTaskStatus.Blocked));
            })
            .ToArray();
    }
}

public sealed class TeamWriteValidator : AbstractValidator<TeamWrite>
{
    public TeamWriteValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Description).MaximumLength(2000);
    }
}

public sealed class TeamMembershipWriteValidator : AbstractValidator<TeamMembershipWrite>
{
    public TeamMembershipWriteValidator()
    {
        RuleFor(request => request.UserId).NotEmpty();
        RuleFor(request => request.Role).NotEmpty();
    }
}

public sealed class MemberWriteValidator : AbstractValidator<MemberWrite>
{
    public MemberWriteValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email).NotEmpty().MaximumLength(320).EmailAddress();
        RuleFor(request => request.Phone).MaximumLength(40);
        RuleFor(request => request.PhotoUrl).MaximumLength(500).WebUrl();
        RuleFor(request => request.Designation).MaximumLength(120);
        RuleFor(request => request.Department).MaximumLength(120);
        RuleFor(request => request.Status).NotEmpty();
        RuleFor(request => request.Notes).MaximumLength(8000);
    }
}

using FluentValidation;
using Worknexa.Application.Common;
using Worknexa.Application.Work;
using Worknexa.Domain.Enums;

namespace Worknexa.Application.Leaves;

public sealed record LeaveListItem(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid LeaveTypeId,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int Days,
    string? Reason,
    string Status,
    Guid? ApprovedById,
    string? ApprovedByName);

public sealed record LeaveWrite(Guid EmployeeId, Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate, string? Reason);

public sealed record LeaveDecision(string Status);

public sealed record LeaveTypeOption(Guid Id, string Name, bool IsPaid);

public sealed record LeavePersonOption(Guid Id, string Name);

public sealed record LeaveOptions(IReadOnlyList<LeaveTypeOption> LeaveTypes, IReadOnlyList<LeavePersonOption> People);

public sealed record AvailabilityWarning(string? Warning);

public sealed record LeaveSpan(string LeaveType, DateOnly StartDate, DateOnly EndDate, string Status);

public sealed record TeamAvailabilityRow(Guid UserId, string Name, string Availability, IReadOnlyList<LeaveSpan> Leaves);

public sealed record LeaveDraft(Guid EmployeeId, Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate, string? Reason);

public interface ILeaveStore
{
    Task<LeaveOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaveListItem>> ListAsync(Guid userId, Guid? employeeId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    Task<LeaveListItem> CreateAsync(Guid userId, LeaveDraft draft, CancellationToken cancellationToken);

    Task<LeaveListItem?> UpdateAsync(Guid userId, Guid leaveId, LeaveDraft draft, CancellationToken cancellationToken);

    Task<LeaveListItem?> DecideAsync(Guid userId, Guid leaveId, LeaveStatus status, CancellationToken cancellationToken);

    Task<string?> WarningAsync(Guid userId, Guid employeeId, DateOnly on, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeamAvailabilityRow>> TeamAvailabilityAsync(Guid userId, Guid? teamId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}

public interface ILeaveService
{
    Task<LeaveOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LeaveListItem>> ListAsync(Guid userId, Guid? employeeId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    Task<LeaveListItem> CreateAsync(Guid userId, LeaveWrite request, CancellationToken cancellationToken);

    Task<LeaveListItem> UpdateAsync(Guid userId, Guid leaveId, LeaveWrite request, CancellationToken cancellationToken);

    Task<LeaveListItem> DecideAsync(Guid userId, Guid leaveId, LeaveDecision request, CancellationToken cancellationToken);

    Task<AvailabilityWarning> WarningAsync(Guid userId, Guid employeeId, DateOnly on, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeamAvailabilityRow>> TeamAvailabilityAsync(Guid userId, Guid? teamId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}

public sealed class LeaveService(ILeaveStore store) : ILeaveService
{
    public Task<LeaveOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return store.OptionsAsync(userId, cancellationToken);
    }

    public Task<IReadOnlyList<LeaveListItem>> ListAsync(Guid userId, Guid? employeeId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        return store.ListAsync(userId, employeeId, from, to, cancellationToken);
    }

    public Task<LeaveListItem> CreateAsync(Guid userId, LeaveWrite request, CancellationToken cancellationToken)
    {
        return store.CreateAsync(userId, Draft(request), cancellationToken);
    }

    public async Task<LeaveListItem> UpdateAsync(Guid userId, Guid leaveId, LeaveWrite request, CancellationToken cancellationToken)
    {
        return await store.UpdateAsync(userId, leaveId, Draft(request), cancellationToken)
            ?? throw new NotFoundException("Leave was not found.");
    }

    public async Task<LeaveListItem> DecideAsync(Guid userId, Guid leaveId, LeaveDecision request, CancellationToken cancellationToken)
    {
        var status = TaskService.ParseRequired<LeaveStatus>(request.Status, "status");
        if (status == LeaveStatus.Pending)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("status", "Choose Approved, Rejected, or Cancelled.")]);
        }

        return await store.DecideAsync(userId, leaveId, status, cancellationToken)
            ?? throw new NotFoundException("Leave was not found.");
    }

    public async Task<AvailabilityWarning> WarningAsync(Guid userId, Guid employeeId, DateOnly on, CancellationToken cancellationToken)
    {
        return new AvailabilityWarning(await store.WarningAsync(userId, employeeId, on, cancellationToken));
    }

    public Task<IReadOnlyList<TeamAvailabilityRow>> TeamAvailabilityAsync(Guid userId, Guid? teamId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        if (from > to)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("from", "Start must be on or before the end date.")]);
        }

        return store.TeamAvailabilityAsync(userId, teamId, from, to, cancellationToken);
    }

    private static LeaveDraft Draft(LeaveWrite request)
    {
        if (request.StartDate > request.EndDate)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("startDate", "Start must be on or before the end date.")]);
        }

        var reason = request.Reason?.Trim();
        return new LeaveDraft(request.EmployeeId, request.LeaveTypeId, request.StartDate, request.EndDate, string.IsNullOrEmpty(reason) ? null : reason);
    }
}

public sealed class LeaveWriteValidator : AbstractValidator<LeaveWrite>
{
    public LeaveWriteValidator()
    {
        RuleFor(request => request.EmployeeId).NotEmpty();
        RuleFor(request => request.LeaveTypeId).NotEmpty();
        RuleFor(request => request.Reason).MaximumLength(2000);
    }
}

public sealed class LeaveDecisionValidator : AbstractValidator<LeaveDecision>
{
    public LeaveDecisionValidator()
    {
        RuleFor(request => request.Status).NotEmpty();
    }
}

using Microsoft.EntityFrameworkCore;
using Worknexa.Application.Common;
using Worknexa.Application.Leaves;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;
using Worknexa.Domain.Leaves;
using Worknexa.Infrastructure.Persistence;

namespace Worknexa.Infrastructure.Leaves;

public sealed class EfLeaveStore(WorknexaDbContext context) : ILeaveStore
{
    public async Task<LeaveOptions> OptionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var types = await context.LeaveTypes.AsNoTracking()
            .OrderBy(leaveType => leaveType.Name)
            .Select(leaveType => new LeaveTypeOption(leaveType.Id, leaveType.Name, leaveType.IsPaid))
            .ToListAsync(cancellationToken);
        var people = await (await VisiblePeople(userId, cancellationToken))
            .OrderBy(person => person.DisplayName)
            .Select(person => new LeavePersonOption(person.Id, person.DisplayName))
            .Take(200)
            .ToListAsync(cancellationToken);
        return new LeaveOptions(types, people);
    }

    public async Task<IReadOnlyList<LeaveListItem>> ListAsync(Guid userId, Guid? employeeId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var query = await VisibleLeaves(userId, cancellationToken);
        if (employeeId is not null)
        {
            query = query.Where(leave => leave.UserId == employeeId);
        }

        if (from is not null)
        {
            query = query.Where(leave => leave.EndDate >= from);
        }

        if (to is not null)
        {
            query = query.Where(leave => leave.StartDate <= to);
        }

        var rows = await query
            .OrderBy(leave => leave.StartDate)
            .ThenBy(leave => leave.User.DisplayName)
            .Select(leave => new LeaveRow(
                leave.Id,
                leave.UserId,
                leave.User.DisplayName,
                leave.LeaveTypeId,
                leave.LeaveType.Name,
                leave.StartDate,
                leave.EndDate,
                leave.Reason,
                leave.Status,
                leave.ReviewedById,
                leave.ReviewedBy != null ? leave.ReviewedBy.DisplayName : null))
            .Take(200)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<LeaveListItem> CreateAsync(Guid userId, LeaveDraft draft, CancellationToken cancellationToken)
    {
        if (draft.EmployeeId != userId && !await CanAsync(userId, PermissionCatalog.LeavesManage, cancellationToken))
        {
            throw new ForbiddenException("You can request leave for yourself.");
        }

        if (!await (await VisiblePeople(userId, cancellationToken)).AnyAsync(person => person.Id == draft.EmployeeId, cancellationToken))
        {
            throw new NotFoundException("Choose a person from the directory.");
        }

        var leaveType = await context.LeaveTypes.FirstOrDefaultAsync(item => item.Id == draft.LeaveTypeId, cancellationToken)
            ?? throw new NotFoundException("Choose a leave type.");
        var leave = new Leave
        {
            Id = Guid.NewGuid(),
            UserId = draft.EmployeeId,
            LeaveTypeId = leaveType.Id,
            StartDate = draft.StartDate,
            EndDate = draft.EndDate,
            Reason = draft.Reason,
            Status = LeaveStatus.Pending
        };
        context.Leaves.Add(leave);
        context.Entry(leave).State = EntityState.Added;
        await context.SaveChangesAsync(cancellationToken);
        return (await FindAsync(userId, leave.Id, cancellationToken))!;
    }

    public async Task<LeaveListItem?> UpdateAsync(Guid userId, Guid leaveId, LeaveDraft draft, CancellationToken cancellationToken)
    {
        var leave = await (await VisibleLeaves(userId, cancellationToken)).FirstOrDefaultAsync(item => item.Id == leaveId, cancellationToken);
        if (leave is null)
        {
            return null;
        }

        var manage = await CanAsync(userId, PermissionCatalog.LeavesManage, cancellationToken);
        if (leave.UserId != userId && !manage)
        {
            throw new ForbiddenException("You can edit your own leave.");
        }

        if (leave.Status != LeaveStatus.Pending)
        {
            throw new ConflictException("Only a pending request can be edited.");
        }

        if (draft.EmployeeId != leave.UserId && !manage)
        {
            throw new ForbiddenException("You can request leave for yourself.");
        }

        var leaveType = await context.LeaveTypes.FirstOrDefaultAsync(item => item.Id == draft.LeaveTypeId, cancellationToken)
            ?? throw new NotFoundException("Choose a leave type.");
        leave.UserId = draft.EmployeeId;
        leave.LeaveTypeId = leaveType.Id;
        leave.StartDate = draft.StartDate;
        leave.EndDate = draft.EndDate;
        leave.Reason = draft.Reason;
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, leave.Id, cancellationToken);
    }

    public async Task<LeaveListItem?> DecideAsync(Guid userId, Guid leaveId, LeaveStatus status, CancellationToken cancellationToken)
    {
        var leave = await (await VisibleLeaves(userId, cancellationToken)).FirstOrDefaultAsync(item => item.Id == leaveId, cancellationToken);
        if (leave is null)
        {
            return null;
        }

        var manage = await CanAsync(userId, PermissionCatalog.LeavesManage, cancellationToken);
        if (status == LeaveStatus.Cancelled)
        {
            if (leave.UserId != userId && !manage)
            {
                throw new ForbiddenException("You can cancel your own leave.");
            }
        }
        else if (!manage)
        {
            throw new ForbiddenException("You do not have permission to review leave.");
        }

        if (leave.Status == status)
        {
            throw new ConflictException($"This leave is already {status}.");
        }

        if (status != LeaveStatus.Cancelled && leave.Status != LeaveStatus.Pending)
        {
            throw new ConflictException("Only a pending request can be reviewed.");
        }

        leave.Status = status;
        leave.ReviewedById = status == LeaveStatus.Cancelled && leave.UserId == userId ? leave.ReviewedById : userId;
        await context.SaveChangesAsync(cancellationToken);
        return await FindAsync(userId, leave.Id, cancellationToken);
    }

    public async Task<string?> WarningAsync(Guid userId, Guid employeeId, DateOnly on, CancellationToken cancellationToken)
    {
        if (!await CanSeePersonAsync(userId, employeeId, cancellationToken))
        {
            return null;
        }

        var rows = await context.Leaves.AsNoTracking()
            .Where(leave => leave.UserId == employeeId && leave.Status == LeaveStatus.Approved && leave.StartDate <= on && leave.EndDate >= on)
            .Select(leave => new { leave.User.DisplayName, Type = leave.LeaveType.Name, leave.StartDate, leave.EndDate })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        return string.Join(" ", rows.Select(row => LeaveCalendar.Warning(row.DisplayName, row.Type, row.StartDate, row.EndDate)));
    }

    public async Task<IReadOnlyList<TeamAvailabilityRow>> TeamAvailabilityAsync(Guid userId, Guid? teamId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var people = await VisiblePeople(userId, cancellationToken);
        if (teamId is Guid selectedTeam)
        {
            var memberIds = context.TeamMembers.Where(member => member.TeamId == selectedTeam).Select(member => member.UserId);
            people = people.Where(person => memberIds.Contains(person.Id));
        }

        var directory = await people
            .OrderBy(person => person.DisplayName)
            .Select(person => new { person.Id, person.DisplayName })
            .Take(100)
            .ToListAsync(cancellationToken);
        var ids = directory.Select(person => person.Id).ToArray();
        var leaves = await context.Leaves.AsNoTracking()
            .Where(leave =>
                ids.Contains(leave.UserId)
                && leave.StartDate <= to
                && leave.EndDate >= from
                && (leave.Status == LeaveStatus.Approved || leave.Status == LeaveStatus.Pending))
            .Select(leave => new
            {
                leave.UserId,
                Type = leave.LeaveType.Name,
                leave.StartDate,
                leave.EndDate,
                leave.Status
            })
            .ToListAsync(cancellationToken);
        return directory.Select(person =>
        {
            var spans = leaves.Where(leave => leave.UserId == person.Id).ToArray();
            var availability = spans.Any(leave => leave.Status == LeaveStatus.Approved)
                ? "OnLeave"
                : spans.Any(leave => leave.Status == LeaveStatus.Pending) ? "Pending" : "Available";
            return new TeamAvailabilityRow(
                person.Id,
                person.DisplayName,
                availability,
                spans.Select(leave => new LeaveSpan(leave.Type, leave.StartDate, leave.EndDate, leave.Status.ToString())).ToArray());
        }).ToArray();
    }

    private async Task<LeaveListItem?> FindAsync(Guid userId, Guid leaveId, CancellationToken cancellationToken)
    {
        var row = await (await VisibleLeaves(userId, cancellationToken))
            .Where(leave => leave.Id == leaveId)
            .Select(leave => new LeaveRow(
                leave.Id,
                leave.UserId,
                leave.User.DisplayName,
                leave.LeaveTypeId,
                leave.LeaveType.Name,
                leave.StartDate,
                leave.EndDate,
                leave.Reason,
                leave.Status,
                leave.ReviewedById,
                leave.ReviewedBy != null ? leave.ReviewedBy.DisplayName : null))
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : Map(row);
    }

    private static LeaveListItem Map(LeaveRow row)
    {
        return new LeaveListItem(
            row.Id,
            row.EmployeeId,
            row.EmployeeName,
            row.LeaveTypeId,
            row.LeaveType,
            row.StartDate,
            row.EndDate,
            LeaveCalendar.Days(row.StartDate, row.EndDate),
            row.Reason,
            row.Status.ToString(),
            row.ApprovedById,
            row.ApprovedByName);
    }

    private async Task<IQueryable<Leave>> VisibleLeaves(Guid userId, CancellationToken cancellationToken)
    {
        var leaves = context.Leaves.AsQueryable();
        if (await CanAsync(userId, PermissionCatalog.LeavesManage, cancellationToken))
        {
            return leaves;
        }

        var people = await VisiblePeople(userId, cancellationToken);
        return leaves.Where(leave => people.Any(person => person.Id == leave.UserId));
    }

    private async Task<bool> CanSeePersonAsync(Guid userId, Guid employeeId, CancellationToken cancellationToken)
    {
        return await (await VisiblePeople(userId, cancellationToken)).AnyAsync(person => person.Id == employeeId, cancellationToken);
    }

    private async Task<IQueryable<User>> VisiblePeople(Guid userId, CancellationToken cancellationToken)
    {
        var people = context.Users.Where(person => person.IsActive);
        if (await CanAsync(userId, PermissionCatalog.LeavesManage, cancellationToken)
            || await CanAsync(userId, PermissionCatalog.TeamsManage, cancellationToken))
        {
            return people;
        }

        var teamIds = context.TeamMembers.Where(member => member.UserId == userId).Select(member => member.TeamId);
        return people.Where(person =>
            person.Id == userId
            || context.TeamMembers.Any(member => member.UserId == person.Id && teamIds.Contains(member.TeamId)));
    }

    private Task<bool> CanAsync(Guid userId, string permission, CancellationToken cancellationToken)
    {
        return context.UserRoles.AsNoTracking().AnyAsync(
            assignment => assignment.UserId == userId
                && assignment.Role.RolePermissions.Any(grant => grant.Permission.Key == permission),
            cancellationToken);
    }

    private sealed record LeaveRow(
        Guid Id,
        Guid EmployeeId,
        string EmployeeName,
        Guid LeaveTypeId,
        string LeaveType,
        DateOnly StartDate,
        DateOnly EndDate,
        string? Reason,
        LeaveStatus Status,
        Guid? ApprovedById,
        string? ApprovedByName);
}

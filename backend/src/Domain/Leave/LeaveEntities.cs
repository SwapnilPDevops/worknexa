using Worknexa.Domain.Common;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;

namespace Worknexa.Domain.Leaves;

public sealed class LeaveType : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;

    public bool IsPaid { get; set; }

    public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
}

public sealed class Leave : SoftDeletableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid LeaveTypeId { get; set; }

    public LeaveType LeaveType { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    public string? Reason { get; set; }

    public Guid? ReviewedById { get; set; }

    public User? ReviewedBy { get; set; }
}

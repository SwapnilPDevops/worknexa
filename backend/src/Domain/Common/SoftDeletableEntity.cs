namespace Worknexa.Domain.Common;

public abstract class SoftDeletableEntity : AuditableEntity
{
    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }
}

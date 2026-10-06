namespace Worknexa.Domain.Common;

public abstract class AuditableEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }
}

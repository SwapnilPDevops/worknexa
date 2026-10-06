using Worknexa.Domain.Common;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;

namespace Worknexa.Domain.Platform;

public sealed class Notification : SoftDeletableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public string? Link { get; set; }

    public Guid? ReminderId { get; set; }

    public NotificationType Type { get; set; }

    public Guid? SourceId { get; set; }
}

public sealed class NotificationPreference : AuditableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    public bool IsEnabled { get; set; } = true;
}

public sealed class ActivityLog : AuditableEntity
{
    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string? Metadata { get; set; }
}

public sealed class Attachment : SoftDeletableEntity
{
    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string StorageKey { get; set; } = string.Empty;
}

using Worknexa.Domain.Common;
using Worknexa.Domain.Enums;
using Worknexa.Domain.Identity;

namespace Worknexa.Domain.Integrations;

/// <summary>One person's link to an external account, such as their Outlook calendar.</summary>
public sealed class IntegrationConnection : AuditableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>Catalog key, for example "outlook-calendar". The domain never depends on a specific provider.</summary>
    public string Provider { get; set; } = string.Empty;

    public IntegrationCategory Category { get; set; }

    public IntegrationStatus Status { get; set; }

    public SyncDirection Direction { get; set; }

    public bool SyncEnabled { get; set; } = true;

    public string? ExternalAccountId { get; set; }

    public string? ExternalAccountName { get; set; }

    /// <summary>The external container being synchronized, such as a calendar id.</summary>
    public string? ExternalContainerId { get; set; }

    public string? ExternalContainerName { get; set; }

    /// <summary>IANA time zone of the person, used for all-day items.</summary>
    public string? TimeZone { get; set; }

    public string? AccessTokenProtected { get; set; }

    public string? RefreshTokenProtected { get; set; }

    public DateTimeOffset? TokenExpiresAt { get; set; }

    public string? SyncCursor { get; set; }

    public DateTimeOffset? CursorIssuedAt { get; set; }

    public DateTimeOffset? LastSyncedAt { get; set; }

    public DateTimeOffset? NextSyncAt { get; set; }

    public int ConsecutiveFailures { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public ICollection<ExternalLink> Links { get; set; } = new List<ExternalLink>();

    public ICollection<SyncRun> Runs { get; set; } = new List<SyncRun>();
}

/// <summary>Maps a Worknexa record to its copy in an external system, so items are never duplicated.</summary>
public sealed class ExternalLink : AuditableEntity
{
    public Guid ConnectionId { get; set; }

    public IntegrationConnection Connection { get; set; } = null!;

    public string EntityType { get; set; } = string.Empty;

    public Guid LocalId { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    /// <summary>The provider's change marker (for example an ETag or change key) at the last sync.</summary>
    public string? ExternalVersion { get; set; }

    /// <summary>Hash of the shared content at the last sync. A side whose content differs has changed since.</summary>
    public string SyncedHash { get; set; } = string.Empty;

    public LinkOrigin Origin { get; set; }

    public DateTimeOffset LastSyncedAt { get; set; }
}

public sealed class SyncRun : AuditableEntity
{
    public Guid ConnectionId { get; set; }

    public IntegrationConnection Connection { get; set; } = null!;

    public SyncTrigger Trigger { get; set; }

    public SyncDirection Direction { get; set; }

    public SyncRunStatus Status { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public int Imported { get; set; }

    public int Updated { get; set; }

    public int Exported { get; set; }

    public int Removed { get; set; }

    public int Skipped { get; set; }

    public int Conflicts { get; set; }

    public int Failed { get; set; }

    public string? Error { get; set; }

    public ICollection<SyncFailure> Failures { get; set; } = new List<SyncFailure>();
}

public sealed class SyncFailure : AuditableEntity
{
    public Guid RunId { get; set; }

    public SyncRun Run { get; set; } = null!;

    public string Operation { get; set; } = string.Empty;

    public Guid? LocalId { get; set; }

    public string? ExternalId { get; set; }

    public string? Title { get; set; }

    public string Message { get; set; } = string.Empty;
}

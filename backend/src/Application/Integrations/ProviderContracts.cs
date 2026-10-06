using Worknexa.Domain.Enums;

namespace Worknexa.Application.Integrations;

// Calendar (implemented: Outlook; next: Google Calendar)

public sealed record ExternalCalendar(string Id, string Name, bool IsDefault, bool CanEdit);

/// <summary>
/// An event as the provider reports it. All-day events carry midnight-UTC dates in Start and End
/// (End exclusive); timed events carry exact instants.
/// </summary>
public sealed record ExternalCalendarEvent(
    string Id,
    string? Version,
    string Title,
    string? Description,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAllDay,
    string? Location,
    string? MeetingLink,
    DateTimeOffset? UpdatedAt,
    bool Removed);

public sealed record CalendarChangePage(IReadOnlyList<ExternalCalendarEvent> Changes, string Cursor, bool HasMore);

public sealed record CalendarEventWriteModel(
    string Title,
    string? Description,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAllDay,
    string? Location,
    string? TimeZone,
    string IdempotencyKey);

public sealed record ExternalItemRef(string Id, string? Version);

public interface ICalendarProvider : IIntegrationProvider
{
    Task<IReadOnlyList<ExternalCalendar>> ListCalendarsAsync(IntegrationSession session, CancellationToken cancellationToken);

    /// <summary>
    /// Reads changes incrementally. A null cursor starts a full read of the window; the returned cursor
    /// continues it. Throws <see cref="IntegrationFailure.CursorExpired"/> when the cursor is no longer valid.
    /// </summary>
    Task<CalendarChangePage> ReadChangesAsync(IntegrationSession session, string calendarId, string? cursor, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    /// <summary>Creates an event. The idempotency key stops retries from creating duplicates.</summary>
    Task<ExternalItemRef> CreateEventAsync(IntegrationSession session, string calendarId, CalendarEventWriteModel model, CancellationToken cancellationToken);

    Task<ExternalItemRef> UpdateEventAsync(IntegrationSession session, string calendarId, string eventId, CalendarEventWriteModel model, CancellationToken cancellationToken);

    /// <summary>Deleting an event that no longer exists succeeds.</summary>
    Task DeleteEventAsync(IntegrationSession session, string calendarId, string eventId, CancellationToken cancellationToken);
}

// Email (planned: Gmail)

public sealed record EmailAddress(string Address, string? Name);

public sealed record EmailMessage(IReadOnlyList<EmailAddress> To, string Subject, string Body, bool IsHtml, IReadOnlyList<EmailAddress>? Cc = null);

public sealed record EmailSummary(string Id, string Subject, EmailAddress From, DateTimeOffset ReceivedAt, string? Snippet, bool IsRead);

public interface IEmailProvider : IIntegrationProvider
{
    Task<ExternalItemRef> SendAsync(IntegrationSession session, EmailMessage message, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmailSummary>> SearchAsync(IntegrationSession session, string query, int limit, CancellationToken cancellationToken);
}

// Messaging (planned: Microsoft Teams, Slack)

public sealed record MessagingChannel(string Id, string Name, string? Workspace);

public sealed record ChatMessage(string ChannelId, string Text, string? ThreadId = null);

public interface IMessagingProvider : IIntegrationProvider
{
    Task<IReadOnlyList<MessagingChannel>> ListChannelsAsync(IntegrationSession session, CancellationToken cancellationToken);

    Task<ExternalItemRef> PostMessageAsync(IntegrationSession session, ChatMessage message, CancellationToken cancellationToken);
}

// Source control (planned: GitHub, Azure DevOps)

public sealed record SourceRepository(string Id, string Name, string Url, string? DefaultBranch);

public sealed record PullRequestSummary(string Id, string Title, string Status, string Author, string Url, DateTimeOffset UpdatedAt);

public sealed record CommitSummary(string Sha, string Message, string Author, DateTimeOffset CommittedAt, string Url);

public interface ISourceControlProvider : IIntegrationProvider
{
    Task<IReadOnlyList<SourceRepository>> ListRepositoriesAsync(IntegrationSession session, CancellationToken cancellationToken);

    Task<IReadOnlyList<PullRequestSummary>> ListPullRequestsAsync(IntegrationSession session, string repositoryId, string? status, CancellationToken cancellationToken);

    Task<IReadOnlyList<CommitSummary>> ListCommitsAsync(IntegrationSession session, string repositoryId, DateTimeOffset since, CancellationToken cancellationToken);
}

// Project management (planned: Jira, Azure DevOps)

public sealed record ExternalProject(string Id, string Key, string Name);

public sealed record ExternalWorkItem(
    string Id,
    string Key,
    string Title,
    string Status,
    string? Assignee,
    DateTimeOffset? DueAt,
    string Url,
    DateTimeOffset UpdatedAt);

public interface IProjectManagementProvider : IIntegrationProvider
{
    Task<IReadOnlyList<ExternalProject>> ListProjectsAsync(IntegrationSession session, CancellationToken cancellationToken);

    Task<IReadOnlyList<ExternalWorkItem>> SearchWorkItemsAsync(IntegrationSession session, string projectId, string? query, CancellationToken cancellationToken);
}

public static class IntegrationCategories
{
    /// <summary>The permission a person needs to connect and run integrations of a category.</summary>
    public static string ManagePermission(IntegrationCategory category) => category switch
    {
        IntegrationCategory.Calendar => Domain.Identity.PermissionCatalog.CalendarManage,
        IntegrationCategory.ProjectManagement => Domain.Identity.PermissionCatalog.ProjectsManage,
        IntegrationCategory.SourceControl => Domain.Identity.PermissionCatalog.ProjectsManage,
        _ => Domain.Identity.PermissionCatalog.TasksManage
    };
}

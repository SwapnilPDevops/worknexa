using Worknexa.Domain.Enums;

namespace Worknexa.Application.Integrations;

public sealed record IntegrationDescriptor(string Key, string Name, string Vendor, IReadOnlyList<IntegrationCategory> Categories);

/// <summary>Every external system Worknexa is prepared for. A provider becomes available once an implementation is registered.</summary>
public static class IntegrationCatalog
{
    public const string OutlookCalendar = "outlook-calendar";
    public const string GoogleCalendar = "google-calendar";
    public const string MicrosoftTeams = "microsoft-teams";
    public const string Slack = "slack";
    public const string Gmail = "gmail";
    public const string GitHub = "github";
    public const string AzureDevOps = "azure-devops";
    public const string Jira = "jira";

    public static readonly IReadOnlyList<IntegrationDescriptor> All =
    [
        new(OutlookCalendar, "Outlook Calendar", "Microsoft", [IntegrationCategory.Calendar]),
        new(GoogleCalendar, "Google Calendar", "Google", [IntegrationCategory.Calendar]),
        new(Gmail, "Gmail", "Google", [IntegrationCategory.Email]),
        new(MicrosoftTeams, "Microsoft Teams", "Microsoft", [IntegrationCategory.Messaging]),
        new(Slack, "Slack", "Slack", [IntegrationCategory.Messaging]),
        new(GitHub, "GitHub", "GitHub", [IntegrationCategory.SourceControl]),
        new(AzureDevOps, "Azure DevOps", "Microsoft", [IntegrationCategory.SourceControl, IntegrationCategory.ProjectManagement]),
        new(Jira, "Jira", "Atlassian", [IntegrationCategory.ProjectManagement])
    ];

    public static IntegrationDescriptor? Find(string key) => All.FirstOrDefault(item => item.Key == key);
}

/// <summary>Base contract for every external provider. Implementations live in Infrastructure.</summary>
public interface IIntegrationProvider
{
    string Key { get; }

    IntegrationCategory Category { get; }

    /// <summary>False until the server has the provider's app credentials.</summary>
    bool IsConfigured { get; }
}

/// <summary>A provider that signs people in with OAuth 2.0 (authorization code with PKCE).</summary>
public interface IOAuthIntegration : IIntegrationProvider
{
    string AuthorizationUrl(string state, string codeChallenge);

    Task<OAuthGrant> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken);

    Task<OAuthGrant> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
}

public sealed record OAuthGrant(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt, string? AccountId, string? AccountName);

/// <summary>A live, authorized handle for one connection. Tokens never leave the server.</summary>
public sealed record IntegrationSession(Guid ConnectionId, string AccessToken);

public enum IntegrationFailure
{
    /// <summary>The person must reconnect (revoked or expired consent).</summary>
    Auth,

    /// <summary>Throttling, timeouts, or provider outages. Retry later.</summary>
    Transient,

    NotFound,

    /// <summary>The incremental sync token is no longer valid; read everything again.</summary>
    CursorExpired,

    /// <summary>The provider refused this specific request. Retrying will not help.</summary>
    Rejected
}

public sealed class IntegrationException(IntegrationFailure failure, string message, TimeSpan? retryAfter = null, Exception? inner = null)
    : Exception(message, inner)
{
    public IntegrationFailure Failure { get; } = failure;

    public TimeSpan? RetryAfter { get; } = retryAfter;
}

public sealed class IntegrationProviders(IEnumerable<IIntegrationProvider> providers)
{
    private readonly IReadOnlyList<IIntegrationProvider> _providers = providers.ToArray();

    public IIntegrationProvider? Find(string key) => _providers.FirstOrDefault(provider => provider.Key == key);

    public T? Find<T>(string key)
        where T : class, IIntegrationProvider => Find(key) as T;

    public IReadOnlyCollection<string> Keys => _providers.Select(provider => provider.Key).ToArray();
}

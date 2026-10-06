using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Worknexa.Application.Integrations;
using Worknexa.Domain.Enums;

namespace Worknexa.Infrastructure.Integrations.Outlook;

public sealed class OutlookCalendarSettings
{
    public const string SectionName = "Integrations:Outlook";

    public string? ClientId { get; set; }

    /// <summary>Server-side only. Supply it through Integrations__Outlook__ClientSecret or user secrets.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>"common" accepts work, school, and personal Microsoft accounts.</summary>
    public string TenantId { get; set; } = "common";

    /// <summary>Must match the redirect URI registered for the app, ending in /api/integrations/oauth/callback.</summary>
    public string? RedirectUri { get; set; }

    public string AuthorityHost { get; set; } = "https://login.microsoftonline.com";

    public string GraphUrl { get; set; } = "https://graph.microsoft.com/v1.0";
}

/// <summary>Outlook calendars through Microsoft Graph.</summary>
public sealed class OutlookCalendarProvider(HttpClient http, IOptions<OutlookCalendarSettings> options, ILogger<OutlookCalendarProvider> logger)
    : ICalendarProvider, IOAuthIntegration
{
    private const string Scopes = "offline_access openid profile User.Read Calendars.ReadWrite";

    private readonly OutlookCalendarSettings _settings = options.Value;

    public string Key => IntegrationCatalog.OutlookCalendar;

    public IntegrationCategory Category => IntegrationCategory.Calendar;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.ClientId)
        && !string.IsNullOrWhiteSpace(_settings.ClientSecret)
        && !string.IsNullOrWhiteSpace(_settings.RedirectUri);

    private string Authority => $"{_settings.AuthorityHost.TrimEnd('/')}/{Uri.EscapeDataString(_settings.TenantId)}/oauth2/v2.0";

    private string Graph => _settings.GraphUrl.TrimEnd('/');

    public string AuthorizationUrl(string state, string codeChallenge)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId!,
            ["response_type"] = "code",
            ["redirect_uri"] = _settings.RedirectUri!,
            ["response_mode"] = "query",
            ["scope"] = Scopes,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account"
        };
        return $"{Authority}/authorize?{string.Join('&', query.Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value)}"))}";
    }

    public async Task<OAuthGrant> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
    {
        var grant = await TokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _settings.RedirectUri!,
            ["code_verifier"] = codeVerifier
        }, cancellationToken);

        using var profile = await SendAsync(new IntegrationSession(Guid.Empty, grant.AccessToken), HttpMethod.Get, $"{Graph}/me?$select=id,displayName,mail,userPrincipalName", null, cancellationToken);
        var root = profile!.RootElement;
        var name = Text(root, "mail") ?? Text(root, "userPrincipalName") ?? Text(root, "displayName");
        return grant with { AccountId = Text(root, "id"), AccountName = name };
    }

    public Task<OAuthGrant> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return TokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalCalendar>> ListCalendarsAsync(IntegrationSession session, CancellationToken cancellationToken)
    {
        using var document = await SendAsync(session, HttpMethod.Get, $"{Graph}/me/calendars?$select=id,name,isDefaultCalendar,canEdit&$top=100", null, cancellationToken);
        return document!.RootElement.GetProperty("value").EnumerateArray()
            .Select(item => new ExternalCalendar(
                Text(item, "id")!,
                Text(item, "name") ?? "Calendar",
                Flag(item, "isDefaultCalendar"),
                Flag(item, "canEdit")))
            .ToArray();
    }

    public async Task<CalendarChangePage> ReadChangesAsync(IntegrationSession session, string calendarId, string? cursor, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        string url;
        if (cursor is null)
        {
            url = $"{Graph}/me/calendars/{Uri.EscapeDataString(calendarId)}/calendarView/delta"
                + $"?startDateTime={Uri.EscapeDataString(from.UtcDateTime.ToString("o", CultureInfo.InvariantCulture))}"
                + $"&endDateTime={Uri.EscapeDataString(to.UtcDateTime.ToString("o", CultureInfo.InvariantCulture))}";
        }
        else if (cursor.StartsWith(Graph + "/", StringComparison.OrdinalIgnoreCase))
        {
            url = cursor;
        }
        else
        {
            throw new IntegrationException(IntegrationFailure.CursorExpired, "The stored sync position is not valid.");
        }

        using var document = await SendAsync(session, HttpMethod.Get, url, null, cancellationToken, delta: true);
        var root = document!.RootElement;
        var changes = new List<ExternalCalendarEvent>();
        foreach (var item in root.GetProperty("value").EnumerateArray())
        {
            var id = Text(item, "id");
            if (id is null || Text(item, "type") == "seriesMaster")
            {
                continue;
            }

            if (item.TryGetProperty("@removed", out _) || Flag(item, "isCancelled"))
            {
                changes.Add(new ExternalCalendarEvent(id, null, "", null, default, default, false, null, null, null, true));
                continue;
            }

            var description = item.TryGetProperty("body", out var body) ? Text(body, "content") : null;
            var allDay = Flag(item, "isAllDay");
            DateTimeOffset start, end;
            try
            {
                start = Time(item, "start", allDay);
                end = Time(item, "end", allDay);
            }
            catch (IntegrationException)
            {
                logger.LogWarning("Skipped an Outlook event without valid start and end times");
                continue;
            }

            changes.Add(new ExternalCalendarEvent(
                id,
                Text(item, "changeKey"),
                Text(item, "subject") ?? "",
                description ?? Text(item, "bodyPreview"),
                start,
                end,
                allDay,
                item.TryGetProperty("location", out var location) ? Text(location, "displayName") : null,
                (item.TryGetProperty("onlineMeeting", out var meeting) && meeting.ValueKind == JsonValueKind.Object ? Text(meeting, "joinUrl") : null) ?? Text(item, "onlineMeetingUrl"),
                DateTimeOffset.TryParse(Text(item, "lastModifiedDateTime"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var modified) ? modified : null,
                false));
        }

        var next = Text(root, "@odata.nextLink");
        var delta = Text(root, "@odata.deltaLink");
        return new CalendarChangePage(changes, next ?? delta ?? throw new IntegrationException(IntegrationFailure.Transient, "Outlook returned an incomplete page."), next is not null);
    }

    public async Task<ExternalItemRef> CreateEventAsync(IntegrationSession session, string calendarId, CalendarEventWriteModel model, CancellationToken cancellationToken)
    {
        var body = Body(model);
        body["transactionId"] = model.IdempotencyKey;
        using var document = await SendAsync(session, HttpMethod.Post, $"{Graph}/me/calendars/{Uri.EscapeDataString(calendarId)}/events", body, cancellationToken);
        return Ref(document!.RootElement);
    }

    public async Task<ExternalItemRef> UpdateEventAsync(IntegrationSession session, string calendarId, string eventId, CalendarEventWriteModel model, CancellationToken cancellationToken)
    {
        using var document = await SendAsync(session, HttpMethod.Patch, $"{Graph}/me/events/{Uri.EscapeDataString(eventId)}", Body(model), cancellationToken);
        return Ref(document!.RootElement);
    }

    public async Task DeleteEventAsync(IntegrationSession session, string calendarId, string eventId, CancellationToken cancellationToken)
    {
        try
        {
            using var _ = await SendAsync(session, HttpMethod.Delete, $"{Graph}/me/events/{Uri.EscapeDataString(eventId)}", null, cancellationToken);
        }
        catch (IntegrationException exception) when (exception.Failure == IntegrationFailure.NotFound)
        {
        }
    }

    private static JsonObject Body(CalendarEventWriteModel model)
    {
        var zone = "UTC";
        string Stamp(DateTimeOffset value) => model.IsAllDay
            ? value.UtcDateTime.ToString("yyyy-MM-dd'T'00:00:00", CultureInfo.InvariantCulture)
            : value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        if (model.IsAllDay && model.TimeZone is not null)
        {
            zone = TimeZoneInfo.TryConvertIanaIdToWindowsId(model.TimeZone, out var windows) ? windows : model.TimeZone;
        }

        return new JsonObject
        {
            ["subject"] = model.Title,
            ["body"] = new JsonObject { ["contentType"] = "text", ["content"] = model.Description ?? "" },
            ["start"] = new JsonObject { ["dateTime"] = Stamp(model.Start), ["timeZone"] = zone },
            ["end"] = new JsonObject { ["dateTime"] = Stamp(model.End), ["timeZone"] = zone },
            ["isAllDay"] = model.IsAllDay,
            ["location"] = new JsonObject { ["displayName"] = model.Location ?? "" }
        };
    }

    private async Task<OAuthGrant> TokenAsync(Dictionary<string, string> fields, CancellationToken cancellationToken)
    {
        fields["client_id"] = _settings.ClientId!;
        fields["client_secret"] = _settings.ClientSecret!;
        fields["scope"] = Scopes;

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsync($"{Authority}/token", new FormUrlEncodedContent(fields), cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new IntegrationException(IntegrationFailure.Transient, "Microsoft sign-in could not be reached.", inner: exception);
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var code = TryRead(text, "error");
                logger.LogWarning("Microsoft token request failed with {StatusCode} {Error}", (int)response.StatusCode, code);
                if (code is "invalid_grant" or "interaction_required" or "consent_required")
                {
                    throw new IntegrationException(IntegrationFailure.Auth, "Microsoft asked you to sign in again.");
                }

                throw Classify(response, "Microsoft sign-in failed.");
            }

            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            var seconds = root.TryGetProperty("expires_in", out var expires) && expires.TryGetInt32(out var value) ? value : 3600;
            return new OAuthGrant(
                Text(root, "access_token") ?? throw new IntegrationException(IntegrationFailure.Rejected, "Microsoft returned no access token."),
                Text(root, "refresh_token"),
                DateTimeOffset.UtcNow.AddSeconds(seconds),
                null,
                null);
        }
    }

    private async Task<JsonDocument?> SendAsync(IntegrationSession session, HttpMethod method, string url, JsonObject? body, CancellationToken cancellationToken, bool delta = false)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        request.Headers.TryAddWithoutValidation("Prefer", "outlook.timezone=\"UTC\"");
        request.Headers.TryAddWithoutValidation("Prefer", "outlook.body-content-type=\"text\"");
        if (delta)
        {
            request.Headers.TryAddWithoutValidation("Prefer", "odata.maxpagesize=100");
        }

        if (body is not null)
        {
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new IntegrationException(IntegrationFailure.Transient, "Outlook could not be reached.", inner: exception);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            var message = TryRead(text, "error", "message");
            logger.LogWarning("Microsoft Graph {Method} returned {StatusCode}", method, (int)response.StatusCode);
            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new IntegrationException(IntegrationFailure.Auth, "Outlook rejected the saved sign-in."),
                HttpStatusCode.Forbidden => new IntegrationException(IntegrationFailure.Rejected, "Worknexa does not have permission to use this calendar."),
                HttpStatusCode.NotFound => new IntegrationException(IntegrationFailure.NotFound, "The Outlook item no longer exists."),
                HttpStatusCode.Gone => new IntegrationException(IntegrationFailure.CursorExpired, "Outlook asked for a full resync."),
                _ => Classify(response, string.IsNullOrWhiteSpace(message) ? "Outlook refused the request." : $"Outlook refused the request: {message}")
            };
        }
    }

    private static IntegrationException Classify(HttpResponseMessage response, string message)
    {
        var status = (int)response.StatusCode;
        if (status is 408 or 429 or >= 500)
        {
            var retry = response.Headers.RetryAfter?.Delta
                ?? (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : null);
            return new IntegrationException(IntegrationFailure.Transient, "Outlook is busy or unavailable.", retry);
        }

        return new IntegrationException(IntegrationFailure.Rejected, message);
    }

    private static ExternalItemRef Ref(JsonElement root) => new(Text(root, "id") ?? throw new IntegrationException(IntegrationFailure.Rejected, "Outlook returned no event id."), Text(root, "changeKey"));

    private static DateTimeOffset Time(JsonElement item, string property, bool allDay)
    {
        if (!item.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Object
            || !DateTime.TryParse(Text(value, "dateTime"), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var stamp))
        {
            throw new IntegrationException(IntegrationFailure.Rejected, "Outlook returned an event without a valid time.");
        }

        if (allDay)
        {
            return new DateTimeOffset(stamp.Date, TimeSpan.Zero);
        }

        var zone = Text(value, "timeZone");
        if (zone is null or "UTC" or "Etc/UTC" || !TimeZoneInfo.TryFindSystemTimeZoneById(zone, out var info))
        {
            return new DateTimeOffset(DateTime.SpecifyKind(stamp, DateTimeKind.Utc));
        }

        var local = DateTime.SpecifyKind(stamp, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, info.GetUtcOffset(local)).ToUniversalTime();
    }

    private static string? Text(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool Flag(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

    private static string? TryRead(string json, params string[] path)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var current = document.RootElement;
            foreach (var name in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(name, out current))
                {
                    return null;
                }
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

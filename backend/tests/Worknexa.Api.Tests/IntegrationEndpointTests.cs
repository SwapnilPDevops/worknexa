using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Worknexa.Application.Integrations;
using Worknexa.Domain.Enums;

namespace Worknexa.Api.Tests;

/// <summary>Drives the whole sync pipeline against the real database, with a fake standing in for Outlook.</summary>
public sealed class IntegrationEndpointTests : IClassFixture<IntegrationApplicationFactory>, IAsyncLifetime
{
    private const string Provider = IntegrationCatalog.OutlookCalendar;
    private const string Zone = "Asia/Kolkata";

    /// <summary>Setup disconnects this person's integrations, so it avoids the admin account used for manual testing.</summary>
    private const string TestUser = "manager@worknexa.local";

    private readonly IntegrationApplicationFactory _factory;
    private readonly List<Guid> _localEvents = [];
    private readonly string _token = Guid.NewGuid().ToString("N")[..6];
    private HttpClient _client = null!;

    public IntegrationEndpointTests(IntegrationApplicationFactory factory)
    {
        _factory = factory;
    }

    private FakeCalendarProvider Fake => _factory.Calendar;

    public async Task InitializeAsync()
    {
        Fake.Reset();
        _client = await SignIn(TestUser);
        await DisconnectAll(_client);
    }

    public async Task DisposeAsync()
    {
        await DisconnectAll(_client);
        foreach (var id in _localEvents)
        {
            await _client.DeleteAsync($"/api/calendar/events/{id}");
        }
    }

    [Fact]
    public async Task Overview_ListsEveryPreparedProviderAndOnlyOutlookIsAvailable()
    {
        var overview = await Overview(_client);

        Assert.Equal(8, overview.Providers.Length);
        Assert.Equal(
            ["outlook-calendar", "google-calendar", "gmail", "microsoft-teams", "slack", "github", "azure-devops", "jira"],
            overview.Providers.Select(provider => provider.Key).ToArray());
        var outlook = Assert.Single(overview.Providers, provider => provider.Available);
        Assert.Equal(Provider, outlook.Key);
        Assert.True(outlook.Configured);
        Assert.True(outlook.CanManage);
        Assert.Equal(["SourceControl", "ProjectManagement"], overview.Providers.Single(provider => provider.Key == "azure-devops").Categories);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync("/api/integrations/github/connect", new { timeZone = Zone })).StatusCode);
    }

    [Fact]
    public async Task Connect_StoresTheAccountAndNeverReturnsTokens()
    {
        var connection = await Connect();

        Assert.Equal("person@fake.test", connection.AccountName);
        Assert.Equal("ImportOnly", connection.Direction);
        Assert.Equal("Connected", connection.Status);
        Assert.Equal(Zone, connection.TimeZone);
        var raw = await (await _client.GetAsync("/api/integrations")).Content.ReadAsStringAsync();
        Assert.DoesNotContain(FakeCalendarProvider.AccessToken, raw);
        Assert.DoesNotContain(FakeCalendarProvider.RefreshToken, raw);
        Assert.DoesNotContain("Protected", raw);
    }

    [Fact]
    public async Task Callback_RejectsDeniedConsentMissingSignInAndAnotherPersonsState()
    {
        var start = await _client.PostAsJsonAsync($"/api/integrations/{Provider}/connect", new { timeZone = Zone });
        start.EnsureSuccessStatusCode();
        var state = StateOf((await start.Content.ReadFromJsonAsync<Envelope<ConnectBody>>())!.Data!.AuthorizationUrl);

        var member = await SignIn("member@worknexa.local", redirects: false);
        var anonymous = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var callback = $"/api/integrations/oauth/callback?code={FakeCalendarProvider.GoodCode}&state={Uri.EscapeDataString(state)}";

        Assert.Equal("/settings?integration=error&reason=expired", await RedirectOf(member, callback));
        Assert.Equal("/settings?integration=error&reason=signin", await RedirectOf(anonymous, callback));
        Assert.Equal("/settings?integration=error&reason=denied", await RedirectOf(member, "/api/integrations/oauth/callback?error=access_denied"));
        Assert.Equal("/settings?integration=error&reason=expired", await RedirectOf(member, "/api/integrations/oauth/callback?code=x&state=tampered"));
        Assert.Empty((await Overview(_client)).Connections);
    }

    [Fact]
    public async Task ImportOnly_ImportsOnceTracksRemoteChangesAndKeepsCopiesReadOnly()
    {
        var connection = await Connect();
        var day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var timedId = Fake.Add($"Vendor review {_token}", Utc(day, 9), Utc(day, 10), location: "Room 4");
        Fake.Add($"Offsite {_token}", Utc(day, 0), Utc(day.AddDays(1), 0), allDay: true);

        var first = await Sync(connection.Id);
        Assert.Equal("Succeeded", first.Status);
        Assert.Equal(2, first.Imported);

        var second = await Sync(connection.Id);
        Assert.Equal(0, second.Imported);
        Assert.Equal(0, second.Updated);
        var timed = Assert.Single(await Entries($"Vendor review {_token}"));
        Assert.Equal("Room 4", timed.Location);
        Assert.False(timed.Editable);
        var allDay = Assert.Single(await Entries($"Offsite {_token}"));
        Assert.True(allDay.IsAllDay);
        Assert.Equal(new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.FromMinutes(330)), allDay.StartAt);

        var edit = await _client.PutAsJsonAsync($"/api/calendar/events/{timed.OriginId}", EventBody("Changed here", Utc(day, 9), Utc(day, 10)));
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        Assert.Contains("Outlook Calendar", await edit.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/calendar/events/{timed.OriginId}")).StatusCode);

        Fake.Edit(timedId, $"Vendor review moved {_token}");
        var third = await Sync(connection.Id);
        Assert.Equal(1, third.Updated);
        Assert.Empty(await Entries($"Vendor review {_token}"));
        Assert.Single(await Entries($"Vendor review moved {_token}"));

        Fake.Remove(timedId);
        var fourth = await Sync(connection.Id);
        Assert.Equal(1, fourth.Removed);
        Assert.Empty(await Entries($"Vendor review moved {_token}"));
    }

    [Fact]
    public async Task TwoWay_ExportsLocalEventsOnceAndMirrorsLocalEditsAndDeletes()
    {
        var connection = await Connect();
        await Settings(connection.Id, "TwoWay");
        var day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4));
        var localId = await CreateLocal($"Planning {_token}", Utc(day, 11), Utc(day, 12));

        var first = await Sync(connection.Id);
        Assert.Contains(first.Status, new[] { "Succeeded", "PartiallySucceeded" });
        var exported = Assert.Single(Fake.Named($"Planning {_token}"));
        Assert.Equal(localId.ToString("N"), exported.TransactionId);

        var second = await Sync(connection.Id);
        Assert.Equal(0, second.Imported);
        Assert.Single(Fake.Named($"Planning {_token}"));
        Assert.Single(await Entries($"Planning {_token}"));

        (await _client.PutAsJsonAsync($"/api/calendar/events/{localId}", EventBody($"Planning moved {_token}", Utc(day, 13), Utc(day, 14)))).EnsureSuccessStatusCode();
        await Sync(connection.Id);
        var moved = Assert.Single(Fake.Named($"Planning moved {_token}"));
        Assert.Equal(Utc(day, 13), moved.Start);
        Assert.Empty(Fake.Named($"Planning {_token}"));

        var remoteId = Fake.Add($"Customer call {_token}", Utc(day, 15), Utc(day, 16));
        await Sync(connection.Id);
        var imported = Assert.Single(await Entries($"Customer call {_token}"));
        Assert.True(imported.Editable);
        (await _client.PutAsJsonAsync($"/api/calendar/events/{imported.OriginId}", EventBody($"Customer call renamed {_token}", Utc(day, 15), Utc(day, 16)))).EnsureSuccessStatusCode();
        await Sync(connection.Id);
        Assert.Equal($"Customer call renamed {_token}", Fake.Get(remoteId).Title);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/calendar/events/{localId}")).StatusCode);
        var last = await Sync(connection.Id);
        Assert.True(last.Removed >= 1);
        Assert.True(Fake.Get(moved.Id).Removed);
    }

    [Fact]
    public async Task ItemFailures_FinishAsPartialAndAppearInTheSyncLog()
    {
        var connection = await Connect();
        await Settings(connection.Id, "ExportOnly");
        var day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        Fake.RejectTitle = $"Rejected {_token}";
        await CreateLocal($"Rejected {_token}", Utc(day, 9), Utc(day, 10));

        var run = await Sync(connection.Id);

        Assert.Equal("PartiallySucceeded", run.Status);
        var failure = Assert.Single(run.Failures, item => item.Title == $"Rejected {_token}");
        Assert.Equal("Export", failure.Operation);
        Assert.Contains("refused", failure.Message);
        var runs = (await _client.GetFromJsonAsync<Envelope<RunBody[]>>($"/api/integrations/connections/{connection.Id}/runs"))!.Data!;
        Assert.Equal(run.Id, runs[0].Id);
        Assert.Equal("Connected", (await Overview(_client)).Connections.Single().Status);
    }

    [Fact]
    public async Task ProviderFailures_BackOffThenRecoverOrAskToReconnect()
    {
        var connection = await Connect();
        Fake.Add($"Standup {_token}", DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddMinutes(15));

        Fake.NextReadFailure = new IntegrationException(IntegrationFailure.Transient, "Fake is throttling.", TimeSpan.FromMinutes(30));
        var throttled = await Sync(connection.Id);
        Assert.Equal("Failed", throttled.Status);
        var afterThrottle = (await Overview(_client)).Connections.Single();
        Assert.Equal("Connected", afterThrottle.Status);
        Assert.Equal(1, afterThrottle.ConsecutiveFailures);
        Assert.True(afterThrottle.NextSyncAt > DateTimeOffset.UtcNow.AddMinutes(29));
        Assert.Empty(await Entries($"Standup {_token}"));

        Fake.AuthFailures = 1;
        var refreshed = await Sync(connection.Id);
        Assert.Equal("Succeeded", refreshed.Status);
        Assert.Equal(1, Fake.Refreshes);
        Assert.Single(await Entries($"Standup {_token}"));
        Assert.Equal(0, (await Overview(_client)).Connections.Single().ConsecutiveFailures);

        Fake.AuthFailures = 2;
        var revoked = await Sync(connection.Id);
        Assert.Equal("Failed", revoked.Status);
        var needsReconnect = (await Overview(_client)).Connections.Single();
        Assert.Equal("NeedsReconnect", needsReconnect.Status);
        Assert.Null(needsReconnect.NextSyncAt);
    }

    [Fact]
    public async Task ExpiredCursor_TriggersOneFullReadWithoutDuplicates()
    {
        var connection = await Connect();
        Fake.Add($"Review {_token}", DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(2).AddHours(1));
        await Sync(connection.Id);

        Fake.ExpireCursors = true;
        var run = await Sync(connection.Id);

        Assert.Equal("Succeeded", run.Status);
        Assert.Equal(0, run.Imported);
        Assert.Single(await Entries($"Review {_token}"));
    }

    [Fact]
    public async Task Disconnect_RemovesImportedCopiesAndKeepsLocalEvents()
    {
        var connection = await Connect();
        await Settings(connection.Id, "TwoWay");
        var day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6));
        Fake.Add($"Imported {_token}", Utc(day, 9), Utc(day, 10));
        await CreateLocal($"Mine {_token}", Utc(day, 11), Utc(day, 12));
        await Sync(connection.Id);
        Assert.Single(await Entries($"Imported {_token}"));

        var response = await _client.DeleteAsync($"/api/integrations/connections/{connection.Id}");
        response.EnsureSuccessStatusCode();

        Assert.Equal(1, (await response.Content.ReadFromJsonAsync<Envelope<DisconnectBody>>())!.Data!.RemovedEvents);
        Assert.Empty(await Entries($"Imported {_token}"));
        Assert.Single(await Entries($"Mine {_token}"));
        Assert.Empty((await Overview(_client)).Connections);
    }

    [Fact]
    public async Task ChoosingAReadOnlyCalendarRequiresImportOnly()
    {
        var connection = await Connect();

        var rejected = await _client.PutAsJsonAsync($"/api/integrations/connections/{connection.Id}", new { direction = "TwoWay", syncEnabled = true, calendarId = FakeCalendarProvider.HolidayCalendarId });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var accepted = await Settings(connection.Id, "ImportOnly", FakeCalendarProvider.HolidayCalendarId);
        Assert.Equal("Holidays", accepted.CalendarName);
    }

    [Fact]
    public async Task RevokedAccessWhileListingCalendars_AsksToReconnect()
    {
        var connection = await Connect();
        Fake.RevokeCalendars = true;

        var response = await _client.GetAsync($"/api/integrations/connections/{connection.Id}/calendars");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("INTEGRATION_RECONNECT", await response.Content.ReadAsStringAsync());
        var after = (await Overview(_client)).Connections.Single();
        Assert.Equal("NeedsReconnect", after.Status);
        Assert.Null(after.NextSyncAt);
    }

    private async Task<ConnectionBody> Connect()
    {
        var client = await SignIn(TestUser, redirects: false);
        var start = await client.PostAsJsonAsync($"/api/integrations/{Provider}/connect", new { timeZone = Zone });
        start.EnsureSuccessStatusCode();
        var state = StateOf((await start.Content.ReadFromJsonAsync<Envelope<ConnectBody>>())!.Data!.AuthorizationUrl);

        var location = await RedirectOf(client, $"/api/integrations/oauth/callback?code={FakeCalendarProvider.GoodCode}&state={Uri.EscapeDataString(state)}");
        Assert.Equal($"/settings?integration=connected&provider={Provider}", location);
        return Assert.Single((await Overview(_client)).Connections);
    }

    private async Task<ConnectionBody> Settings(Guid connectionId, string direction, string? calendarId = null)
    {
        var response = await _client.PutAsJsonAsync($"/api/integrations/connections/{connectionId}", new { direction, syncEnabled = true, calendarId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Envelope<ConnectionBody>>())!.Data!;
    }

    private async Task<RunBody> Sync(Guid connectionId)
    {
        var response = await _client.PostAsync($"/api/integrations/connections/{connectionId}/sync", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Envelope<RunBody>>())!.Data!;
    }

    private async Task<Guid> CreateLocal(string title, DateTimeOffset start, DateTimeOffset end)
    {
        var response = await _client.PostAsJsonAsync("/api/calendar/events", EventBody(title, start, end));
        response.EnsureSuccessStatusCode();
        var id = (await response.Content.ReadFromJsonAsync<Envelope<IdBody>>())!.Data!.Id;
        _localEvents.Add(id);
        return id;
    }

    private async Task<EntryBody[]> Entries(string title)
    {
        var from = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("o"));
        var to = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(30).ToString("o"));
        var entries = (await _client.GetFromJsonAsync<Envelope<EntryBody[]>>($"/api/calendar?from={from}&to={to}"))!.Data!;
        return entries.Where(entry => entry.Origin == "event" && entry.Title == title).ToArray();
    }

    private static object EventBody(string title, DateTimeOffset start, DateTimeOffset end) => new
    {
        title,
        kind = "Personal",
        startAt = start,
        endAt = end,
        isAllDay = false,
        recurrence = "None"
    };

    private static DateTimeOffset Utc(DateOnly day, int hour) => new(day.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);

    private static string StateOf(string authorizationUrl) => HttpUtility.ParseQueryString(new Uri(authorizationUrl).Query)["state"]!;

    private static async Task<string> RedirectOf(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location!.OriginalString;
    }

    private static async Task<OverviewBody> Overview(HttpClient client) =>
        (await client.GetFromJsonAsync<Envelope<OverviewBody>>("/api/integrations"))!.Data!;

    private static async Task DisconnectAll(HttpClient client)
    {
        foreach (var connection in (await Overview(client)).Connections)
        {
            (await client.DeleteAsync($"/api/integrations/connections/{connection.Id}")).EnsureSuccessStatusCode();
        }
    }

    private async Task<HttpClient> SignIn(string email, bool redirects = true)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = redirects });
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Worknexa_Dev123!" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<Envelope<LoginBody>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body?.Data?.AccessToken);
        return client;
    }

    private sealed class Envelope<T>
    {
        public T? Data { get; set; }
    }

    private sealed class LoginBody
    {
        public string? AccessToken { get; set; }
    }

    private sealed class IdBody
    {
        public Guid Id { get; set; }
    }

    private sealed class ConnectBody
    {
        public string AuthorizationUrl { get; set; } = "";
    }

    private sealed class OverviewBody
    {
        public ProviderBody[] Providers { get; set; } = [];

        public ConnectionBody[] Connections { get; set; } = [];
    }

    private sealed class ProviderBody
    {
        public string Key { get; set; } = "";

        public string[] Categories { get; set; } = [];

        public bool Available { get; set; }

        public bool Configured { get; set; }

        public bool CanManage { get; set; }
    }

    private sealed class ConnectionBody
    {
        public Guid Id { get; set; }

        public string Status { get; set; } = "";

        public string Direction { get; set; } = "";

        public string? AccountName { get; set; }

        public string? CalendarName { get; set; }

        public string? TimeZone { get; set; }

        public DateTimeOffset? NextSyncAt { get; set; }

        public int ConsecutiveFailures { get; set; }
    }

    private sealed class RunBody
    {
        public Guid Id { get; set; }

        public string Status { get; set; } = "";

        public int Imported { get; set; }

        public int Updated { get; set; }

        public int Removed { get; set; }

        public FailureBody[] Failures { get; set; } = [];
    }

    private sealed class FailureBody
    {
        public string Operation { get; set; } = "";

        public string? Title { get; set; }

        public string Message { get; set; } = "";
    }

    private sealed class DisconnectBody
    {
        public int RemovedEvents { get; set; }
    }

    private sealed class EntryBody
    {
        public string Origin { get; set; } = "";

        public Guid OriginId { get; set; }

        public string Title { get; set; } = "";

        public DateTimeOffset StartAt { get; set; }

        public bool IsAllDay { get; set; }

        public string? Location { get; set; }

        public bool Editable { get; set; }
    }
}

public sealed class IntegrationApplicationFactory : AuthApplicationFactory
{
    public FakeCalendarProvider Calendar { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Integrations:BackgroundSync", "false");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IIntegrationProvider>();
            services.AddSingleton<IIntegrationProvider>(Calendar);
        });
    }
}

/// <summary>An in-memory calendar with a change log, so delta reads, echoes, and failures can be scripted.</summary>
public sealed class FakeCalendarProvider : ICalendarProvider, IOAuthIntegration
{
    public const string GoodCode = "good-code";
    public const string AccessToken = "fake-access-token";
    public const string RefreshToken = "fake-refresh-token";
    public const string PrimaryCalendarId = "cal-primary";
    public const string HolidayCalendarId = "cal-holidays";

    private readonly object _gate = new();
    private readonly Dictionary<string, FakeEvent> _events = [];
    private long _sequence;

    public string? RejectTitle { get; set; }

    public IntegrationException? NextReadFailure { get; set; }

    public int AuthFailures { get; set; }

    public bool ExpireCursors { get; set; }

    public bool RevokeCalendars { get; set; }

    public int Refreshes { get; private set; }

    public string Key => IntegrationCatalog.OutlookCalendar;

    public IntegrationCategory Category => IntegrationCategory.Calendar;

    public bool IsConfigured => true;

    public void Reset()
    {
        lock (_gate)
        {
            _events.Clear();
            RejectTitle = null;
            NextReadFailure = null;
            AuthFailures = 0;
            ExpireCursors = false;
            RevokeCalendars = false;
            Refreshes = 0;
        }
    }

    public string Add(string title, DateTimeOffset start, DateTimeOffset end, bool allDay = false, string? location = null)
    {
        lock (_gate)
        {
            var item = new FakeEvent { Id = $"evt-{Guid.NewGuid():N}", Calendar = PrimaryCalendarId, Title = title, Start = start, End = end, IsAllDay = allDay, Location = location };
            Touch(item);
            _events[item.Id] = item;
            return item.Id;
        }
    }

    public void Edit(string id, string title)
    {
        lock (_gate)
        {
            _events[id].Title = title;
            Touch(_events[id]);
        }
    }

    public void Remove(string id)
    {
        lock (_gate)
        {
            _events[id].Removed = true;
            Touch(_events[id]);
        }
    }

    public FakeEvent Get(string id)
    {
        lock (_gate)
        {
            return _events[id];
        }
    }

    public IReadOnlyList<FakeEvent> Named(string title)
    {
        lock (_gate)
        {
            return _events.Values.Where(item => item.Title == title && !item.Removed).ToArray();
        }
    }

    public string AuthorizationUrl(string state, string codeChallenge) =>
        $"https://login.fake.test/authorize?state={Uri.EscapeDataString(state)}&code_challenge={codeChallenge}";

    public Task<OAuthGrant> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
    {
        if (code != GoodCode || string.IsNullOrEmpty(codeVerifier))
        {
            throw new IntegrationException(IntegrationFailure.Rejected, "Bad code.");
        }

        return Task.FromResult(new OAuthGrant(AccessToken, RefreshToken, DateTimeOffset.UtcNow.AddHours(1), "acct-1", "person@fake.test"));
    }

    public Task<OAuthGrant> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        Refreshes++;
        return Task.FromResult(new OAuthGrant(AccessToken, RefreshToken, DateTimeOffset.UtcNow.AddHours(1), null, null));
    }

    public Task<IReadOnlyList<ExternalCalendar>> ListCalendarsAsync(IntegrationSession session, CancellationToken cancellationToken)
    {
        if (RevokeCalendars)
        {
            throw new IntegrationException(IntegrationFailure.Auth, "Token revoked.");
        }

        return Task.FromResult<IReadOnlyList<ExternalCalendar>>([new(PrimaryCalendarId, "Calendar", true, true), new(HolidayCalendarId, "Holidays", false, false)]);
    }

    public Task<CalendarChangePage> ReadChangesAsync(IntegrationSession session, string calendarId, string? cursor, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (AuthFailures > 0)
            {
                AuthFailures--;
                throw new IntegrationException(IntegrationFailure.Auth, "Token revoked.");
            }

            if (NextReadFailure is { } failure)
            {
                NextReadFailure = null;
                throw failure;
            }

            if (cursor is not null && ExpireCursors)
            {
                ExpireCursors = false;
                throw new IntegrationException(IntegrationFailure.CursorExpired, "Cursor expired.");
            }

            var since = cursor is null ? -1 : long.Parse(cursor);
            var changes = _events.Values
                .Where(item => item.Calendar == calendarId && (cursor is null ? !item.Removed : item.Sequence > since))
                .Where(item => item.Removed || (item.End > from && item.Start < to))
                .Select(item => new ExternalCalendarEvent(item.Id, item.Version.ToString(), item.Title, null, item.Start, item.End, item.IsAllDay, item.Location, null, item.UpdatedAt, item.Removed))
                .ToArray();
            return Task.FromResult(new CalendarChangePage(changes, _sequence.ToString(), false));
        }
    }

    public Task<ExternalItemRef> CreateEventAsync(IntegrationSession session, string calendarId, CalendarEventWriteModel model, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (model.Title == RejectTitle)
            {
                throw new IntegrationException(IntegrationFailure.Rejected, "The fake calendar refused this event.");
            }

            var existing = _events.Values.FirstOrDefault(item => item.TransactionId == model.IdempotencyKey && !item.Removed);
            if (existing is not null)
            {
                return Task.FromResult(new ExternalItemRef(existing.Id, existing.Version.ToString()));
            }

            var item = new FakeEvent { Id = $"evt-{Guid.NewGuid():N}", Calendar = calendarId, TransactionId = model.IdempotencyKey };
            Apply(item, model);
            _events[item.Id] = item;
            return Task.FromResult(new ExternalItemRef(item.Id, item.Version.ToString()));
        }
    }

    public Task<ExternalItemRef> UpdateEventAsync(IntegrationSession session, string calendarId, string eventId, CalendarEventWriteModel model, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_events.TryGetValue(eventId, out var item) || item.Removed)
            {
                throw new IntegrationException(IntegrationFailure.NotFound, "Gone.");
            }

            Apply(item, model);
            return Task.FromResult(new ExternalItemRef(item.Id, item.Version.ToString()));
        }
    }

    public Task DeleteEventAsync(IntegrationSession session, string calendarId, string eventId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_events.TryGetValue(eventId, out var item) && !item.Removed)
            {
                item.Removed = true;
                Touch(item);
            }

            return Task.CompletedTask;
        }
    }

    private void Apply(FakeEvent item, CalendarEventWriteModel model)
    {
        item.Title = model.Title;
        item.Start = model.Start;
        item.End = model.End;
        item.IsAllDay = model.IsAllDay;
        item.Location = model.Location;
        Touch(item);
    }

    private void Touch(FakeEvent item)
    {
        item.Sequence = ++_sequence;
        item.Version++;
        item.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public sealed class FakeEvent
    {
        public string Id { get; set; } = "";

        public string Calendar { get; set; } = "";

        public string Title { get; set; } = "";

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset End { get; set; }

        public bool IsAllDay { get; set; }

        public string? Location { get; set; }

        public bool Removed { get; set; }

        public string? TransactionId { get; set; }

        public long Sequence { get; set; }

        public int Version { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}

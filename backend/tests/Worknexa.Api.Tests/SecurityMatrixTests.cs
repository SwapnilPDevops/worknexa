using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Worknexa.Api.Tests;

/// <summary>
/// Cross-cutting API checks: every workflow endpoint requires a session, manage-only endpoints reject the
/// Team Member role, and invalid input is rejected with 400 before anything is written.
/// </summary>
public sealed class SecurityMatrixTests : IClassFixture<AuthApplicationFactory>
{
    private static readonly string Id = Guid.NewGuid().ToString();
    private readonly AuthApplicationFactory _factory;

    public SecurityMatrixTests(AuthApplicationFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string, string> ProtectedEndpoints => new()
    {
        { "GET", "/api/tasks" },
        { "POST", "/api/tasks" },
        { "POST", $"/api/tasks/{Id}/complete" },
        { "PUT", $"/api/tasks/{Id}/assignee" },
        { "GET", "/api/todos" },
        { "GET", "/api/projects" },
        { "POST", "/api/projects" },
        { "GET", "/api/meetings?date=2026-10-06&offsetMinutes=0" },
        { "POST", "/api/meetings" },
        { "GET", "/api/standups?date=2026-10-06" },
        { "POST", "/api/standups" },
        { "GET", "/api/meeting-notes" },
        { "PUT", $"/api/meeting-notes/{Id}" },
        { "POST", $"/api/meeting-notes/{Id}/actions/{Id}/task" },
        { "GET", "/api/reminders" },
        { "POST", "/api/reminders" },
        { "GET", "/api/teams" },
        { "POST", $"/api/teams/{Id}/members" },
        { "GET", "/api/leaves" },
        { "POST", "/api/leaves" },
        { "GET", "/api/learning/topics" },
        { "POST", "/api/learning/topics" },
        { "GET", "/api/notes" },
        { "POST", "/api/notes" },
        { "GET", "/api/search?q=plan" },
        { "GET", "/api/notifications" },
        { "GET", "/api/integrations" },
        { "GET", "/api/assistant/status" },
        { "GET", "/api/directory" },
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Endpoints_RequireASession(string method, string path)
    {
        var response = await _factory.CreateClient().SendAsync(Request(method, path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string, string> ManageOnlyEndpoints => new()
    {
        { "POST", "/api/projects" },
        { "PUT", $"/api/projects/{Id}" },
        { "DELETE", $"/api/projects/{Id}" },
        { "POST", $"/api/projects/{Id}/members" },
        { "POST", "/api/meetings" },
        { "PUT", $"/api/meetings/{Id}" },
        { "DELETE", $"/api/meetings/{Id}" },
        { "POST", "/api/calendar/events" },
        { "POST", "/api/teams" },
        { "PUT", $"/api/teams/{Id}" },
        { "POST", $"/api/teams/{Id}/members" },
        { "DELETE", $"/api/teams/{Id}/members/{Id}" },
        { "PUT", $"/api/members/{Id}" },
        { "GET", "/api/directory" },
    };

    [Theory]
    [MemberData(nameof(ManageOnlyEndpoints))]
    public async Task ManageEndpoints_RejectTheTeamMemberRole(string method, string path)
    {
        var (client, _) = await SignIn("member@worknexa.local");

        var response = await client.SendAsync(Request(method, path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal("FORBIDDEN", error?.Code);
    }

    [Fact]
    public async Task Leave_TeamMemberCannotRequestLeaveForSomeoneElse()
    {
        var (_, adminId) = await SignIn("admin@worknexa.local");
        var (member, _) = await SignIn("member@worknexa.local");

        var response = await member.PostAsJsonAsync("/api/leaves", new
        {
            employeeId = adminId,
            leaveTypeId = Guid.NewGuid(),
            startDate = "2026-12-01",
            endDate = "2026-12-02",
            reason = "Not mine to request"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public static TheoryData<string, string, string, string> InvalidWrites => new()
    {
        { "POST", "/api/tasks", """{"title":"","status":"Todo","priority":"High"}""", "Title" },
        { "POST", "/api/tasks", """{"title":"Bad status","status":"Exploding","priority":"High"}""", "status" },
        { "POST", "/api/tasks", """{"title":"Backwards","status":"Todo","priority":"High","startAt":"2026-10-10T00:00:00Z","dueAt":"2026-10-01T00:00:00Z"}""", "startAt" },
        { "POST", "/api/projects", """{"name":"","code":"","status":"Active","priority":"High"}""", "Name" },
        { "POST", "/api/projects", """{"name":"Link test","code":"LNK","status":"Active","priority":"High","repositoryUrl":"javascript:alert(document.cookie)"}""", "RepositoryUrl" },
        { "POST", "/api/meetings", """{"title":"","meetingType":"","durationMinutes":0}""", "Title" },
        { "POST", "/api/meetings", """{"title":"Link test","meetingType":"General","durationMinutes":30,"startAt":"2026-10-07T09:00:00Z","meetingLink":"javascript:alert(1)"}""", "MeetingLink" },
        { "POST", "/api/reminders", """{"title":"","recurrence":"None","priority":"Medium"}""", "Title" },
        { "POST", "/api/standups", """{"status":""}""", "Status" },
        { "PUT", $"/api/meeting-notes/{Id}", """{"actions":[{"title":""}]}""", "Actions[0].Title" },
        { "POST", $"/api/teams/{Id}/members", $$"""{"userId":"{{Guid.Empty}}","role":""}""", "UserId" },
        { "PUT", $"/api/members/{Id}", """{"name":"Pat","email":"pat@worknexa.local","status":"Active","photoUrl":"data:text/html,<script>alert(1)</script>"}""", "PhotoUrl" },
        { "POST", "/api/leaves", $$"""{"employeeId":"{{Guid.Empty}}","leaveTypeId":"{{Guid.Empty}}","startDate":"2026-12-01","endDate":"2026-12-02"}""", "EmployeeId" },
        { "POST", "/api/learning/topics", """{"name":"","status":"Planned","priority":"Medium"}""", "Name" },
        { "POST", "/api/learning/topics", """{"name":"Progress","status":"Planned","priority":"Medium","progress":150}""", "Progress" },
        { "POST", "/api/notes", """{"title":""}""", "Title" },
        { "POST", "/api/calendar/events", """{"title":"Backwards","kind":"Event","recurrence":"None","startAt":"2026-10-07T10:00:00Z","endAt":"2026-10-07T09:00:00Z"}""", "EndAt" },
    };

    [Theory]
    [MemberData(nameof(InvalidWrites))]
    public async Task InvalidWrites_ReturnFieldErrors(string method, string path, string json, string field)
    {
        var (client, _) = await SignIn("admin@worknexa.local");

        var response = await client.SendAsync(Request(method, path, json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal("VALIDATION_ERROR", error?.Code);
        Assert.Contains(error?.Errors ?? [], item => string.Equals(item.Field, field, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("""{"title": "Unclosed""")]
    [InlineData("""{"title": 42, "status": "Todo", "priority": "High"}""")]
    [InlineData("not json at all")]
    public async Task MalformedBodies_AreBadRequestsAndLeakNothing(string json)
    {
        var (client, _) = await SignIn("admin@worknexa.local");

        var response = await client.SendAsync(Request("POST", "/api/tasks", json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DROP TABLE worknexa.\"Tasks\"; --")]
    [InlineData("%' UNION SELECT \"PasswordHash\" FROM worknexa.\"Users\" --")]
    public async Task Search_TreatsSqlAsPlainText(string query)
    {
        var (client, _) = await SignIn("member@worknexa.local");

        var search = await client.GetAsync($"/api/search?q={Uri.EscapeDataString(query)}");
        var tasks = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        Assert.DoesNotContain("AQAAAA", await search.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, tasks.StatusCode);
    }

    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        var response = await _factory.CreateClient().GetAsync("/api/health");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Errors_CarryTheCorrelationIdAsTraceId()
    {
        var (client, _) = await SignIn("admin@worknexa.local");
        var request = Request("POST", "/api/tasks", """{"title":""}""");
        request.Headers.Add("X-Correlation-ID", "trace-me-123");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal("trace-me-123", error?.TraceId);
        Assert.Equal("trace-me-123", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    private static HttpRequestMessage Request(string method, string path, string json = "{}")
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is not ("GET" or "DELETE"))
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private async Task<(HttpClient Client, Guid UserId)> SignIn(string email)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Worknexa_Dev123!" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<Envelope<LoginBody>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
        return (client, body.Data.User!.Id);
    }

    private sealed class Envelope<T>
    {
        public T? Data { get; set; }
    }

    private sealed class LoginBody
    {
        public string? AccessToken { get; set; }

        public UserBody? User { get; set; }
    }

    private sealed class UserBody
    {
        public Guid Id { get; set; }
    }

    private sealed class ErrorBody
    {
        public string? Code { get; set; }

        public string? TraceId { get; set; }

        public FieldError[]? Errors { get; set; }
    }

    private sealed class FieldError
    {
        public string? Field { get; set; }
    }
}

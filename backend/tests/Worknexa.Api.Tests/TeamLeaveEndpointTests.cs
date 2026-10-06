using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Worknexa.Api.Tests;

public sealed class TeamLeaveEndpointTests : IClassFixture<AuthApplicationFactory>
{
    private readonly AuthApplicationFactory _factory;

    public TeamLeaveEndpointTests(AuthApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Teams_RequireASignedInUser()
    {
        var response = await _factory.CreateClient().GetAsync("/api/teams");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Team_ProfileLeaveAndWorkloadReuseExistingTasks()
    {
        var admin = await AuthenticatedClient();
        var created = await admin.PostAsJsonAsync("/api/teams", new { name = "Platform", description = "Product engineering" });
        created.EnsureSuccessStatusCode();
        var team = await created.Content.ReadFromJsonAsync<Envelope<TeamBody>>();
        Assert.NotNull(team?.Data);
        Assert.Contains(team.Data.Members, member => member.Role == "Owner");

        var memberId = Guid.Parse("8f0b1c2d-3e4f-4a5b-8c6d-7e8f9a0b1c2f");
        var added = await admin.PostAsJsonAsync($"/api/teams/{team.Data.Id}/members", new { userId = memberId, role = "Member" });
        added.EnsureSuccessStatusCode();

        var profile = await admin.PutAsJsonAsync($"/api/members/{memberId}", new
        {
            name = "Worknexa Member",
            email = "member@worknexa.local",
            phone = "+1 555 0100",
            designation = "Engineer",
            department = "Product",
            joinedOn = "2026-01-06",
            status = "Active",
            notes = "Ships the workspace.",
            skills = new[] { "Planning" },
            technologies = new[] { "PostgreSQL" }
        });
        profile.EnsureSuccessStatusCode();
        var member = await profile.Content.ReadFromJsonAsync<Envelope<MemberBody>>();
        Assert.Equal("Engineer", member?.Data?.Designation);
        Assert.Contains("Planning", member?.Data?.Skills ?? []);

        var memberClient = await AuthenticatedClient("member@worknexa.local");
        var leave = await memberClient.PostAsJsonAsync("/api/leaves", new
        {
            employeeId = memberId,
            leaveTypeId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
            startDate = "2026-10-08",
            endDate = "2026-10-10",
            reason = "Trip"
        });
        leave.EnsureSuccessStatusCode();
        var requested = await leave.Content.ReadFromJsonAsync<Envelope<LeaveBody>>();
        Assert.Equal(3, requested?.Data?.Days);
        Assert.Equal("Pending", requested?.Data?.Status);

        var denied = await memberClient.PostAsJsonAsync($"/api/leaves/{requested!.Data!.Id}/decision", new { status = "Approved" });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var manager = await AuthenticatedClient("manager@worknexa.local");
        var approved = await manager.PostAsJsonAsync($"/api/leaves/{requested.Data.Id}/decision", new { status = "Approved" });
        approved.EnsureSuccessStatusCode();
        var decision = await approved.Content.ReadFromJsonAsync<Envelope<LeaveBody>>();
        Assert.Equal("Approved", decision?.Data?.Status);
        Assert.Equal("Worknexa Manager", decision?.Data?.ApprovedByName);

        var warning = await admin.GetAsync($"/api/leaves/availability?userId={memberId}&on=2026-10-09");
        warning.EnsureSuccessStatusCode();
        var availability = await warning.Content.ReadFromJsonAsync<Envelope<WarningBody>>();
        Assert.Contains("Annual", availability?.Data?.Warning);

        var task = await admin.PostAsJsonAsync("/api/tasks", new
        {
            title = "Cover the release",
            status = "Blocked",
            priority = "High",
            teamId = team.Data.Id,
            assigneeId = memberId,
            dueAt = "2026-10-05T09:00:00Z"
        });
        task.EnsureSuccessStatusCode();

        var workload = await admin.GetAsync($"/api/teams/workload?teamId={team.Data.Id}");
        workload.EnsureSuccessStatusCode();
        var rows = await workload.Content.ReadFromJsonAsync<Envelope<WorkloadBody[]>>();
        var row = rows?.Data?.Single(item => item.UserId == memberId);
        Assert.NotNull(row);
        Assert.True(row.Assigned >= 1);
        Assert.True(row.Blocked >= 1);
    }

    private async Task<HttpClient> AuthenticatedClient(string email = "admin@worknexa.local")
    {
        var client = _factory.CreateClient();
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

    private sealed class TeamBody
    {
        public Guid Id { get; set; }

        public TeamMemberBody[] Members { get; set; } = [];
    }

    private sealed class TeamMemberBody
    {
        public string? Role { get; set; }
    }

    private sealed class MemberBody
    {
        public string? Designation { get; set; }

        public string[] Skills { get; set; } = [];
    }

    private sealed class LeaveBody
    {
        public Guid Id { get; set; }

        public int Days { get; set; }

        public string? Status { get; set; }

        public string? ApprovedByName { get; set; }
    }

    private sealed class WarningBody
    {
        public string? Warning { get; set; }
    }

    private sealed class WorkloadBody
    {
        public Guid UserId { get; set; }

        public int Assigned { get; set; }

        public int Blocked { get; set; }
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Worknexa.Application.Dashboard;

namespace Worknexa.Api.Tests;

public sealed class DashboardEndpointTests : IClassFixture<DashboardApplicationFactory>
{
    private readonly DashboardApplicationFactory _factory;

    public DashboardEndpointTests(DashboardApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MyDay_RequiresASignedInUser()
    {
        var response = await _factory.CreateClient().GetAsync("/api/dashboard/my-day");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MyDay_ReturnsTheComposedDayForTheSignedInUser()
    {
        var client = await AuthenticatedClient("admin@worknexa.local");

        var response = await client.GetAsync("/api/dashboard/my-day?date=2026-10-06&offsetMinutes=330");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Envelope<MyDayBody>>();
        Assert.Equal("You are clear for today.", body?.Data?.Summary);
        Assert.Equal(0, body?.Data?.Counts?.Tasks);
        Assert.NotNull(body?.Data?.Tasks?.Today);
        Assert.Empty(body.Data.Tasks.Today);
    }

    [Fact]
    public async Task QuickCapture_RejectsAMeetingWhenTheRoleCannotManageMeetings()
    {
        var client = await AuthenticatedClient("member@worknexa.local");

        var response = await client.PostAsJsonAsync("/api/dashboard/quick-capture", new
        {
            kind = "meeting",
            title = "Planning",
            notes = "",
            date = "2026-10-06",
            offsetMinutes = 0
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpClient> AuthenticatedClient(string email)
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

    private sealed class MyDayBody
    {
        public string? Summary { get; set; }

        public CountBody? Counts { get; set; }

        public TaskGroupsBody? Tasks { get; set; }
    }

    private sealed class CountBody
    {
        public int Tasks { get; set; }
    }

    private sealed class TaskGroupsBody
    {
        public object[] Today { get; set; } = [];
    }
}

public sealed class DashboardApplicationFactory : AuthApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMyDayQuery>();
            services.AddSingleton<IMyDayQuery, EmptyMyDayQuery>();
        });
    }

    private sealed class EmptyMyDayQuery : IMyDayQuery
    {
        public Task<MyDaySnapshot> LoadAsync(Guid userId, MyDayWindow window, MyDayScope scope, CancellationToken cancellationToken)
        {
            return Task.FromResult(new MyDaySnapshot());
        }
    }
}

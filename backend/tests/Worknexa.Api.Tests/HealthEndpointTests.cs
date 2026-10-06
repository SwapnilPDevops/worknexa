using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Worknexa.Api.Middleware;

namespace Worknexa.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthBody>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
        Assert.True(response.Headers.Contains(CorrelationIdMiddleware.HeaderName));
    }

    [Fact]
    public async Task GetHealth_EchoesCorrelationId()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "request-123");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("request-123", response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    private sealed class HealthBody
    {
        public string? Status { get; set; }
    }
}

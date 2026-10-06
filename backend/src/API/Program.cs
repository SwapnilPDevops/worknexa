using Serilog;
using Worknexa.Api;
using Worknexa.Application;
using Worknexa.Infrastructure;

// Container health probe: the runtime image has no shell or curl, so the app checks itself.
if (args is ["--healthcheck", ..])
{
    return await HealthProbe.RunAsync(args.Length > 1 ? args[1] : "/api/health");
}

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext());

builder.Services.AddWorknexaApi(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseWorknexaApi();
await app.RunAsync();
return 0;

public partial class Program;

internal static class HealthProbe
{
    public static async Task<int> RunAsync(string path)
    {
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")?.Split(';', ',')[0] ?? "8080";
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
        try
        {
            using var response = await client.GetAsync($"http://127.0.0.1:{port}{path}");
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return 1;
        }
    }
}

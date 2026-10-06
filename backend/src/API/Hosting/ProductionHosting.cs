using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Worknexa.Api.Hosting;

public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>Turn on when the API runs behind a reverse proxy that sets X-Forwarded-For and X-Forwarded-Proto.</summary>
    public bool Enabled { get; init; }

    /// <summary>CIDR ranges of the proxies allowed to set forwarded headers, e.g. the container network.</summary>
    public string[] KnownNetworks { get; init; } = [];

    /// <summary>Individual proxy addresses allowed to set forwarded headers.</summary>
    public string[] KnownProxies { get; init; } = [];
}

public static class ProductionHosting
{
    public static string Version { get; } =
        typeof(ProductionHosting).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "unknown";

    public static IServiceCollection AddWorknexaHosting(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var forwarded = configuration.GetSection(ForwardedHeadersSettings.SectionName).Get<ForwardedHeadersSettings>() ?? new ForwardedHeadersSettings();
        if (forwarded.Enabled)
        {
            if (forwarded.KnownNetworks.Length == 0 && forwarded.KnownProxies.Length == 0)
            {
                throw new InvalidOperationException("ForwardedHeaders:Enabled needs ForwardedHeaders:KnownNetworks or ForwardedHeaders:KnownProxies so clients cannot spoof their address.");
            }

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
                foreach (var network in forwarded.KnownNetworks)
                {
                    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
                }

                foreach (var proxy in forwarded.KnownProxies)
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }
            });
        }

        services.AddWorknexaTelemetry(configuration, environment);
        return services;
    }

    /// <summary>
    /// Traces and metrics are exported only when an OTLP endpoint is configured (OTEL_EXPORTER_OTLP_ENDPOINT),
    /// so no collector is needed to run Worknexa. Logs always go to stdout.
    /// </summary>
    private static void AddWorknexaTelemetry(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            return;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(configuration["OTEL_SERVICE_NAME"] ?? "worknexa-api", serviceVersion: Version)
                .AddAttributes([new KeyValuePair<string, object>(
                    "deployment.environment.name",
                    configuration["DEPLOYMENT_ENVIRONMENT"] ?? environment.EnvironmentName)]))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/api/health");
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation()
                .AddSource("Npgsql")
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Npgsql")
                .AddMeter("Microsoft.AspNetCore.RateLimiting")
                .AddOtlpExporter());
    }

    public static bool ApiDocsEnabled(this IConfiguration configuration) =>
        configuration.GetValue<bool>("ApiDocs:Enabled");
}

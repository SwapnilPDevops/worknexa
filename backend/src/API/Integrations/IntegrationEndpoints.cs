using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Worknexa.Api.Contracts;
using Worknexa.Api.Dashboard;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Integrations;

namespace Worknexa.Api.Integrations;

public static class IntegrationEndpoints
{
    public static IServiceCollection AddWorknexaIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("Worknexa");
        var keyDirectory = configuration[$"{IntegrationSettings.SectionName}:KeyDirectory"];
        if (!string.IsNullOrWhiteSpace(keyDirectory))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
        }

        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.AddHostedService<IntegrationSyncWorker>();
        return services;
    }

    public static RouteGroupBuilder MapWorknexaIntegrations(this RouteGroupBuilder api)
    {
        var integrations = api.MapGroup("/integrations").WithTags("Integrations").RequireAuthorization();

        integrations.MapGet("/", async (IIntegrationService service, ICurrentUser currentUser, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await DashboardEndpoints.PermissionsAsync(httpContext, userId, cancellationToken);
            return ApiResults.Success(await service.OverviewAsync(userId, permissions, cancellationToken));
        });

        integrations.MapPost("/{provider}/connect", async (string provider, ConnectRequest request, IIntegrationService service, ICurrentUser currentUser, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await DashboardEndpoints.PermissionsAsync(httpContext, userId, cancellationToken);
            return ApiResults.Success(await service.StartConnectAsync(userId, permissions, provider, request, DateTimeOffset.UtcNow, cancellationToken));
        });

        // The provider redirects the browser here, so this is a plain GET that ends in a redirect back to the app.
        integrations.MapGet("/oauth/callback", async (string? code, string? state, string? error, IIntegrationService service, ICurrentUser currentUser, IOptions<IntegrationSettings> options, ILoggerFactory loggers, CancellationToken cancellationToken) =>
        {
            var returnUrl = options.Value.ReturnUrl;
            var separator = returnUrl.Contains('?') ? '&' : '?';
            try
            {
                var provider = await service.CompleteConnectAsync(currentUser.UserId, code, state, error, DateTimeOffset.UtcNow, cancellationToken);
                return Results.Redirect($"{returnUrl}{separator}integration=connected&provider={Uri.EscapeDataString(provider)}");
            }
            catch (IntegrationConnectException exception)
            {
                return Results.Redirect($"{returnUrl}{separator}integration=error&reason={Uri.EscapeDataString(exception.Message)}");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                loggers.CreateLogger(typeof(IntegrationEndpoints)).LogError(exception, "Completing an integration connection failed");
                return Results.Redirect($"{returnUrl}{separator}integration=error&reason=failed");
            }
        }).AllowAnonymous();

        integrations.MapGet("/connections/{connectionId:guid}/calendars", async (Guid connectionId, IIntegrationService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.CalendarsAsync(RequireUser(currentUser), connectionId, DateTimeOffset.UtcNow, cancellationToken)));

        integrations.MapPut("/connections/{connectionId:guid}", async (Guid connectionId, ConnectionSettingsWrite request, IIntegrationService service, ICurrentUser currentUser, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await DashboardEndpoints.PermissionsAsync(httpContext, userId, cancellationToken);
            return ApiResults.Success(await service.UpdateAsync(userId, permissions, connectionId, request, DateTimeOffset.UtcNow, cancellationToken));
        });

        integrations.MapPost("/connections/{connectionId:guid}/sync", async (Guid connectionId, IIntegrationService service, ICurrentUser currentUser, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await DashboardEndpoints.PermissionsAsync(httpContext, userId, cancellationToken);
            return ApiResults.Success(await service.SyncAsync(userId, permissions, connectionId, DateTimeOffset.UtcNow, cancellationToken));
        });

        integrations.MapGet("/connections/{connectionId:guid}/runs", async (Guid connectionId, IIntegrationService service, ICurrentUser currentUser, CancellationToken cancellationToken) =>
            ApiResults.Success(await service.RunsAsync(RequireUser(currentUser), connectionId, cancellationToken)));

        integrations.MapDelete("/connections/{connectionId:guid}", async (Guid connectionId, IIntegrationService service, ICurrentUser currentUser, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var userId = RequireUser(currentUser);
            var permissions = await DashboardEndpoints.PermissionsAsync(httpContext, userId, cancellationToken);
            return ApiResults.Success(await service.DisconnectAsync(userId, permissions, connectionId, DateTimeOffset.UtcNow, cancellationToken));
        });

        return api;
    }

    private static Guid RequireUser(ICurrentUser currentUser)
    {
        return currentUser.UserId ?? throw new UnauthorizedAccessException();
    }
}

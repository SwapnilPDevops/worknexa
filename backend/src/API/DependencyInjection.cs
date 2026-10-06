using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Worknexa.Api.Assistant;
using Worknexa.Api.Auth;
using Worknexa.Api.Calendar;
using Worknexa.Api.Integrations;
using Worknexa.Api.Knowledge;
using Worknexa.Api.Learning;
using Worknexa.Api.Leaves;
using Worknexa.Api.Organization;
using Worknexa.Api.Projects;
using Worknexa.Api.Search;
using Worknexa.Api.Security;
using Worknexa.Api.Meetings;
using Worknexa.Api.Dashboard;
using Worknexa.Api.Work;
using Worknexa.Api.Configuration;
using Worknexa.Api.Contracts;
using Worknexa.Api.Errors;
using Worknexa.Api.Filters;
using Worknexa.Api.Health;
using Worknexa.Api.Hosting;
using Worknexa.Api.Identity;
using Worknexa.Api.Middleware;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Auth;

namespace Worknexa.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddWorknexaApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var cors = configuration.GetSection(FrontendCorsSettings.SectionName).Get<FrontendCorsSettings>()
            ?? new FrontendCorsSettings();
        if (cors.AllowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Cors:AllowedOrigins is not configured.");
        }

        if (cors.AllowedOrigins.Any(origin => origin.Contains('*', StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must list exact origins; wildcards cannot be combined with credentials.");
        }

        if (!environment.IsDevelopment()
            && !string.IsNullOrWhiteSpace(configuration["Integrations:Outlook:ClientId"])
            && string.IsNullOrWhiteSpace(configuration["Integrations:KeyDirectory"]))
        {
            throw new InvalidOperationException("Set Integrations__KeyDirectory to a persistent volume; without it, connected-account tokens cannot be decrypted after a restart.");
        }

        var auth = configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>() ?? new AuthSettings();
        if (!environment.IsDevelopment() && (auth.SigningKey.Length < 32 || auth.SigningKey.StartsWith("dev-only-", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Set Auth__SigningKey to a production secret before starting the API.");
        }

        services.AddOptions<AuthSettings>()
            .BindConfiguration(AuthSettings.SectionName)
            .Validate(settings => settings.SigningKey.Length >= 32, "Auth:SigningKey must be at least 32 characters.")
            .Validate(settings => settings.TokenHours is >= 1 and <= 24, "Auth:TokenHours must be between 1 and 24.")
            .ValidateOnStart();

        services.AddSingleton<JwtTokenIssuer>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = string.IsNullOrWhiteSpace(auth.Issuer) ? "worknexa" : auth.Issuer,
                    ValidateAudience = true,
                    ValidAudience = string.IsNullOrWhiteSpace(auth.Audience) ? "worknexa" : auth.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.SigningKey.Length >= 32 ? auth.SigningKey : "dev-only-worknexa-signing-key-32b")),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token)
                            && context.Request.Cookies.TryGetValue(AuthCookie.Name, out var cookie))
                        {
                            context.Token = cookie;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var userIdValue = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? context.Principal?.FindFirst("sub")?.Value;
                        var marker = context.Principal?.FindFirst("pwd")?.Value;
                        if (!Guid.TryParse(userIdValue, out var userId) || marker is null)
                        {
                            context.Fail("Invalid session");
                            return;
                        }

                        var accounts = context.HttpContext.RequestServices.GetRequiredService<IAccountStore>();
                        var account = await accounts.FindByIdAsync(userId, context.HttpContext.RequestAborted);
                        if (account is not { IsActive: true, PasswordHash: not null }
                            || marker != PasswordMarkers.FromHash(account.PasswordHash))
                        {
                            context.Fail("Session is no longer valid");
                            return;
                        }

                        context.HttpContext.Items[DashboardEndpoints.AccountItemKey] = account;
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new ApiError
                        {
                            Code = "UNAUTHORIZED",
                            Message = "Authentication is required"
                        });
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new ApiError
                        {
                            Code = "FORBIDDEN",
                            Message = "You do not have permission to do that."
                        });
                    }
                };
            });
        services.AddAuthorization(options => options.AddPermissionPolicies());
        services.AddWorknexaRateLimits();
        services.AddWorknexaIntegrations(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "Worknexa API",
                Version = ProductionHosting.Version,
                Description = "Responses wrap data as { \"data\": ... }; errors are { code, message, errors[], traceId }. See docs/api/README.md."
            });
        });
        services.AddOpenApi();
        services.AddWorknexaHosting(configuration, environment);
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
                policy.WithOrigins(cors.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        return services;
    }

    public static WebApplication UseWorknexaApi(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>($"{ForwardedHeadersSettings.SectionName}:Enabled"))
        {
            app.UseForwardedHeaders();
        }

        app.UseMiddleware<SecurityHeadersMiddleware>();
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseCors("Frontend");
        app.UseMiddleware<CsrfHeaderMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseExceptionHandler();

        if (app.Configuration.ApiDocsEnabled())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Worknexa API");
            });
            app.MapOpenApi();
        }

        var api = app.MapGroup("/api");
        api.AddEndpointFilter<ValidationEndpointFilter>();
        api.MapWorknexaAuth();
        api.MapWorknexaDashboard();
        api.MapWorknexaWork();
        api.MapWorknexaCalendar();
        api.MapWorknexaMeetings();
        api.MapWorknexaMeetingNotes();
        api.MapWorknexaNotes();
        api.MapWorknexaProjects();
        api.MapWorknexaTeams();
        api.MapWorknexaLeaves();
        api.MapWorknexaLearning();
        api.MapWorknexaSearch();
        api.MapWorknexaAssistant();
        api.MapWorknexaIntegrations();
        api.MapGet("/health", async (HealthCheckService healthChecks, CancellationToken cancellationToken) =>
            {
                var report = await healthChecks.CheckHealthAsync(
                    registration => registration.Tags.Contains("live"),
                    cancellationToken);
                return HealthResults.FromReport(report);
            })
            .WithName("Health")
            .WithTags("Health");

        api.MapGet("/health/ready", async (HealthCheckService healthChecks, CancellationToken cancellationToken) =>
            {
                var report = await healthChecks.CheckHealthAsync(
                    registration => registration.Tags.Contains("ready"),
                    cancellationToken);
                return HealthResults.FromReport(report);
            })
            .WithName("Ready")
            .WithTags("Health");

        return app;
    }
}

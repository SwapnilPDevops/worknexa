using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Worknexa.Api.Contracts;
using Worknexa.Application.Abstractions;

namespace Worknexa.Api.Security;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimits";

    /// <summary>Sign-in attempts per client address per minute.</summary>
    public int LoginPerMinute { get; init; } = 10;

    /// <summary>Password changes per signed-in person per minute.</summary>
    public int PasswordChangePerMinute { get; init; } = 5;

    /// <summary>Each chat turn calls a paid model, so every person gets a small per-minute budget.</summary>
    public int AssistantChatPerMinute { get; init; } = 20;
}

public static class RateLimitPolicies
{
    public const string AssistantChat = "assistant-chat";
    public const string Login = "auth-login";
    public const string PasswordChange = "auth-password";

    public static IServiceCollection AddWorknexaRateLimits(this IServiceCollection services)
    {
        services.AddOptions<RateLimitSettings>()
            .BindConfiguration(RateLimitSettings.SectionName)
            .Validate(settings => settings.LoginPerMinute > 0 && settings.PasswordChangePerMinute > 0 && settings.AssistantChatPerMinute > 0, "RateLimits values must be positive.")
            .ValidateOnStart();

        return services.AddRateLimiter(options =>
        {
            options.AddPolicy(Login, context => PerMinute(ClientAddress(context), Settings(context).LoginPerMinute));
            options.AddPolicy(PasswordChange, context => PerMinute(UserOrAddress(context), Settings(context).PasswordChangePerMinute));
            options.AddPolicy(AssistantChat, context => PerMinute(UserOrAddress(context), Settings(context).AssistantChatPerMinute));
            options.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                var policy = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
                await response.WriteAsJsonAsync(new ApiError
                {
                    Code = "TOO_MANY_REQUESTS",
                    Message = policy switch
                    {
                        Login => "Too many sign-in attempts. Wait a minute and try again.",
                        PasswordChange => "Too many password attempts. Wait a minute and try again.",
                        _ => "You are sending messages too quickly. Wait a moment and try again."
                    }
                }, cancellationToken);
            };
        });
    }

    private static RateLimitSettings Settings(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;

    private static RateLimitPartition<string> PerMinute(string key, int permits) =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{key}:{permits}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });

    private static string ClientAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string UserOrAddress(HttpContext context) =>
        context.RequestServices.GetRequiredService<ICurrentUser>().UserId?.ToString() ?? ClientAddress(context);
}

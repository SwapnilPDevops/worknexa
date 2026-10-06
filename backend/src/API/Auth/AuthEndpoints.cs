using Worknexa.Api.Contracts;
using Worknexa.Api.Security;
using Worknexa.Application.Abstractions;
using Worknexa.Application.Auth;
using Worknexa.Domain.Identity;

namespace Worknexa.Api.Auth;

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, UserSession User);

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapWorknexaAuth(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/login", async (LoginRequest request, IAuthService authService, JwtTokenIssuer tokens, HttpContext httpContext) =>
        {
            var account = await authService.LoginAsync(request, httpContext.RequestAborted);
            return Issue(account, tokens, httpContext);
        }).RequireRateLimiting(RateLimitPolicies.Login);

        auth.MapPost("/logout", (HttpContext httpContext) =>
        {
            AuthCookie.Clear(httpContext);
            return Results.NoContent();
        });

        auth.MapGet("/me", async (IAuthService authService, CancellationToken cancellationToken) =>
            ApiResults.Success(await authService.GetCurrentAsync(cancellationToken)))
            .RequireAuthorization();

        auth.MapPut("/profile", async (UpdateProfileRequest request, IAuthService authService, CancellationToken cancellationToken) =>
            ApiResults.Success(await authService.UpdateProfileAsync(request, cancellationToken)))
            .RequireAuthorization();

        auth.MapPost("/password", async (ChangePasswordRequest request, IAuthService authService, JwtTokenIssuer tokens, HttpContext httpContext) =>
        {
            var account = await authService.ChangePasswordAsync(request, httpContext.RequestAborted);
            return Issue(account, tokens, httpContext);
        }).RequireAuthorization().RequireRateLimiting(RateLimitPolicies.PasswordChange);

        api.MapGet("/directory", () => ApiResults.Success(Array.Empty<string>()))
            .RequireAuthorization(PermissionCatalog.UsersManage)
            .WithTags("Authorization")
            .WithName("Directory");

        return api;
    }

    private static IResult Issue(AccountRecord account, JwtTokenIssuer tokens, HttpContext httpContext)
    {
        var token = tokens.Create(account);
        AuthCookie.Set(httpContext, token.Value, token.ExpiresAt);
        return ApiResults.Success(new LoginResponse(token.Value, token.ExpiresAt, account.ToSession()));
    }
}

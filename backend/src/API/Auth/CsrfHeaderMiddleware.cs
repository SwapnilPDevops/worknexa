using Worknexa.Api.Contracts;

namespace Worknexa.Api.Auth;

public sealed class CsrfHeaderMiddleware
{
    public const string HeaderName = "X-Worknexa-Request";

    private readonly RequestDelegate _next;

    public CsrfHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method)
            || HttpMethods.IsTrace(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var authorization = context.Request.Headers.Authorization.ToString();
        var hasBearer = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
        var hasSessionCookie = context.Request.Cookies.ContainsKey(AuthCookie.Name);
        if (hasSessionCookie && !hasBearer && !context.Request.Headers.ContainsKey(HeaderName))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiError
            {
                Code = "FORBIDDEN",
                Message = "Missing request verification header."
            });
            return;
        }

        await _next(context);
    }
}

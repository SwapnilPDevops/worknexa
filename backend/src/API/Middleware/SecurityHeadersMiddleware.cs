namespace Worknexa.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            // The API serves JSON and file downloads only; Swagger UI needs its own scripts and styles.
            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            }

            // API responses are per-user; browsers and shared proxies must not keep them.
            if (context.Request.Path.StartsWithSegments("/api") && !headers.ContainsKey("Cache-Control"))
            {
                headers.CacheControl = "no-store";
            }

            return Task.CompletedTask;
        });

        return _next(context);
    }
}

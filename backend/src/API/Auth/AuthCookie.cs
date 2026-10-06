namespace Worknexa.Api.Auth;

public static class AuthCookie
{
    public const string Name = "worknexa.session";

    public static void Set(HttpContext httpContext, string token, DateTimeOffset expiresAt)
    {
        httpContext.Response.Cookies.Append(Name, token, Options(httpContext, expiresAt));
    }

    public static void Clear(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(Name, Options(httpContext, null));
    }

    private static CookieOptions Options(HttpContext httpContext, DateTimeOffset? expiresAt)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            // Outside Development the site is served over TLS (often terminated at a proxy), so never send the session in clear text.
            Secure = httpContext.Request.IsHttps
                || !httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expiresAt
        };
    }
}

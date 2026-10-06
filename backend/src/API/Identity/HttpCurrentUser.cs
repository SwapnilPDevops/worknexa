using System.Security.Claims;
using Worknexa.Application.Abstractions;

namespace Worknexa.Api.Identity;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public string? DisplayName => Principal?.FindFirstValue(ClaimTypes.Name);

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;
}

using System.Security.Claims;
using Korendzh.Infrastructure.Auditing;
using Microsoft.AspNetCore.Http;

namespace Korendzh.Web.Auth;

public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserId
    {
        get
        {
            var idStr = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out var id) ? id : null;
        }
    }

    public string? UserName => _accessor.HttpContext?.User?.Identity?.Name;

    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

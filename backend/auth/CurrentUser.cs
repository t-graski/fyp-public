using System.Security.Claims;

namespace backend.auth;

public sealed class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var user = http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var idStr =
                user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                user.FindFirstValue("sub");

            return Guid.TryParse(idStr, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => UserId is not null;

    public long PermissionBits
    {
        get
        {
            var user = http.HttpContext?.User;
            var raw = user?.FindFirst("perm")?.Value;
            return long.TryParse(raw, out var bits) ? bits : 0L;
        }
    }
}
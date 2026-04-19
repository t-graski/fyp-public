using System.Security.Claims;

namespace backend.auth;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public Guid GetUserIdOrThrow()
        {
            var idStr =
                user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                user.FindFirstValue("sub");

            if (Guid.TryParse(idStr, out var id))
            {
                return id;
            }

            throw new InvalidOperationException("JWT does not contain a valid user id claim.");
        }

        public long GetPermBits()
        {
            var raw = user.FindFirst("perm")?.Value;
            return long.TryParse(raw, out var bits) ? bits : 0L;
        }
    }
}
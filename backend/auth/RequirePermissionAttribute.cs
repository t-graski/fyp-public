using Microsoft.AspNetCore.Authorization;

namespace backend.auth;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(Permission permission)
    {
        Policy = PermissionPolicy.Build(permission);
    }
}
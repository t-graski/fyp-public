using backend.models.@base;

namespace backend.auth;

public static class RolePermissions
{
    public static Permission ForRole(SystemRole role) => role switch
    {
        SystemRole.Student => Permission.None,
        SystemRole.Staff => Permission.CatalogRead | Permission.EnrollmentRead,
        SystemRole.Admin => Permission.SuperAdmin,
        _ => Permission.None
    };
}
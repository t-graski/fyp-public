using backend.models.@base;

namespace backend.auth;

public static class RoleHierarchy
{
    public static int Rank(SystemRole role) => role switch
    {
        SystemRole.Student => 10,
        SystemRole.Staff => 20,
        SystemRole.Admin => 999,
        _ => 0
    };
}
namespace backend.auth;

public class PermissionPolicy
{
    public static string Build(Permission p) => $"perm:{(long)p}";
}
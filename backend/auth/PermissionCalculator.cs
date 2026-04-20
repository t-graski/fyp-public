using backend.dtos;
using backend.models;

namespace backend.auth;

public static class PermissionCalculator
{
    /// <summary>
    /// Compute effective permissions from user's roles
    /// </summary>
    public static long ComputeEffective(IEnumerable<Role> roles)
    {
        long permissions = 0;
        foreach (var role in roles)
        {
            permissions |= role.Permissions;
        }

        if (((Permission)permissions).HasFlag(Permission.SuperAdmin))
        {
            return (long)Permission.SuperAdmin;
        }

        return permissions;
    }

    /// <summary>
    /// Get all available permissions as metadata
    /// </summary>
    public static IReadOnlyList<PermissionMetadataDto> GetPermissionMetadata()
    {
        var permissions = new List<PermissionMetadataDto>();
        var type = typeof(Permission);

        foreach (Permission value in Enum.GetValues(type))
        {
            if (value == Permission.None) continue;

            var name = Enum.GetName(type, value)!;
            var longValue = (long)value;

            // Calculate the bit position
            var bit = (int)Math.Log2(longValue);

            var description = GetPermissionDescription(value);

            permissions.Add(new PermissionMetadataDto(
                name,
                bit,
                longValue,
                description
            ));
        }

        return permissions.OrderBy(p => p.Bit).ToList();
    }

    private static string GetPermissionDescription(Permission permission) => permission switch
    {
        Permission.CatalogRead => "View courses and modules",
        Permission.CatalogWrite => "Create and edit courses and modules",
        Permission.CatalogDelete => "Delete courses and modules",
        Permission.EnrollmentRead => "View enrollments",
        Permission.EnrollmentWrite => "Create and edit enrollments",
        Permission.EnrollmentDelete => "Delete enrollments",
        Permission.AuditRead => "View audit logs",
        Permission.UserRead => "View users",
        Permission.UserWrite => "Create and edit users",
        Permission.UserDelete => "Delete users",
        Permission.UserManageRoles => "Assign and remove user roles",
        Permission.RoleRead => "View roles",
        Permission.RoleWrite => "Create and edit roles",
        Permission.RoleDelete => "Delete roles",
        Permission.AttendanceRead => "View attendance records",
        Permission.AttendanceWrite => "Manage attendance records",
        Permission.SuperAdmin => "Full system access (all permissions)",
        _ => "Unknown permission"
    };
}
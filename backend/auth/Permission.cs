namespace backend.auth;

[Flags]
public enum Permission : long
{
    None = 0,

    CatalogRead = 1L << 10,
    CatalogWrite = 1L << 11,
    CatalogDelete = 1L << 12,

    EnrollmentRead = 1L << 13,
    EnrollmentWrite = 1L << 14,
    EnrollmentDelete = 1L << 16,

    AuditRead = 1L << 17,

    UserRead = 1L << 18,
    UserWrite = 1L << 19,
    UserDelete = 1L << 20,
    UserManageRoles = 1L << 21,

    RoleRead = 1L << 22,
    RoleWrite = 1L << 23,
    RoleDelete = 1L << 24,

    AttendanceRead = 1L << 25,
    AttendanceWrite = 1L << 26,

    SuperAdmin = 1L << 31,
}
namespace backend.dtos;

public record AdminUserListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    long Permissions,
    IReadOnlyList<RoleDto> Roles,
    StudentMiniDto? Student,
    StaffMiniDto? Staff
);

public record StudentMiniDto(Guid Id, string StudentNumber);

public record StaffMiniDto(Guid Id, string StaffNumber);

public record AdminUserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    long Permissions,
    IReadOnlyList<RoleDto> Roles,
    StudentMiniDto? Student,
    StaffMiniDto? Staff,
    AdminStudentRecordDto? StudentRecord,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc
);

public record AdminCreateUserDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    bool IsActive,
    Guid? RoleId
);

public record AdminUpdateUserDto(
    string FirstName,
    string LastName
);

public record SetUserActiveDto(bool IsActive);
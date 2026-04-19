using backend.models.@base;

namespace backend.dtos;

public record UserSummaryDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    long Permissions,
    DateTimeOffset? LastLoginAt
);

public record UserDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    long Permissions,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IEnumerable<RoleDto> Roles,
    
    StudentMiniDto? Student,
    StudentRecordDto? StudentRecord
);

public record CreateUserDto(string Email, string Password, string FirstName, string LastName);

public record SetUserStatusDto(bool IsActive);

public record AssignRoleDto(Guid RoleId);
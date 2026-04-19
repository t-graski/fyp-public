namespace backend.dtos;

public record RoleDto(
    Guid Id,
    string Name,
    string Key,
    int Rank,
    bool IsSystem,
    long Permissions
);

public record CreateRoleDto(
    string Name,
    long Permissions
);

public record UpdateRoleDto(
    string Name,
    long Permissions
);

public record PermissionMetadataDto(
    string Key,
    int Bit,
    long Value,
    string Description
);
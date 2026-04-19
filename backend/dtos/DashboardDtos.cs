namespace backend.dtos;

public record StaffModuleCardDto(
    Guid ModuleId,
    string ModuleCode,
    string Title,
    string StaffRole
);

public record StaffDashboardDto(IReadOnlyList<StaffModuleCardDto> Modules);
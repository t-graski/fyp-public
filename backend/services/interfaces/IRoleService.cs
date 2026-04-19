using backend.dtos;

namespace backend.services.interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> ListAsync();
    Task<RoleDto> GetAsync(Guid roleId);
    Task<RoleDto> CreateAsync(CreateRoleDto dto);
    Task<RoleDto> UpdateAsync(Guid roleId, UpdateRoleDto dto);
    Task DeleteAsync(Guid roleId);
    Task<IReadOnlyList<PermissionMetadataDto>> GetPermissionsMetadataAsync();
}

using backend.dtos;

namespace backend.services.interfaces;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserListItemDto>> ListAsync(string? q, int limit = 50, int offset = 0);
    Task<AdminUserDetailDto> GetAsync(Guid userId);

    Task<AdminUserDetailDto> CreateAsync(AdminCreateUserDto dto);
    Task<AdminUserDetailDto> UpdateAsync(Guid userId, AdminUpdateUserDto dto);
    Task SetActiveAsync(Guid userId, bool isActive);

    Task DeleteAsync(Guid userId);
    Task RecomputePermissionsAsync(Guid userId);

    Task<AdminStudentRecordDto> UpsertStudentRecordAsync(Guid userId, AdminUpsertStudentRecordDto dto);
    Task DeleteStudentRecordAsync(Guid userId);
}
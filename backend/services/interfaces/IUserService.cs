using backend.dtos;
using backend.models.@base;

namespace backend.services.interfaces;

public interface IUserService
{
    Task<AuthResultDto> RegisterAsync(RegisterDto dto);
    Task<AuthResultDto> LoginAsync(LoginDto dto);
    Task<UserDetailDto> GetMeAsync(Guid meId);

    Task<PagedDto<UserSummaryDto>> GetUsersAsync(int page, int pageSize);
    Task<UserDetailDto> GetByIdAsync(Guid userId);

    Task<UserDetailDto> CreateAsync(CreateUserDto dto);
    Task SetStatusAsync(Guid userId, bool isActive);
    Task SetPermissionsAsync(Guid userId, long permissions);

    Task ToggleRoleAsync(Guid userId, AssignRoleDto role);
}
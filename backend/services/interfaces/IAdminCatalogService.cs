using backend.dtos;

namespace backend.services.interfaces;

public interface IAdminCatalogService
{
    Task<IReadOnlyList<AdminCourseDto>> ListCoursesAsync(string? q);
    Task<AdminCourseDetailDto> GetCourseAsync(Guid id);
    Task<AdminCourseDetailDto> CreateCourseAsync(CreateCourseDto dto);
    Task<AdminCourseDetailDto> UpdateCourseAsync(Guid id, UpdateCourseDto dto);
    Task DeleteCourseAsync(Guid id);

    Task<IReadOnlyList<AdminModuleDto>> ListModulesByCourseAsync(Guid courseId);
    Task<AdminModuleDto> GetModuleAsync(Guid id);
    Task<AdminModuleDto> CreateModuleAsync(Guid courseId, CreateModuleDto dto);
    Task<AdminModuleDto> UpdateModuleAsync(Guid id, UpdateModuleDto dto);
    Task DeleteModuleAsync(Guid id);
}
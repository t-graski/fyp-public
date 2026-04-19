using backend.dtos;

namespace backend.services.interfaces;

public interface IEnrollmentQueryService
{
    Task<IReadOnlyList<CourseEnrollmentRowDto>> GetStudentsByCourseAsync(Guid courseId);
    Task<IReadOnlyList<ModuleEnrollmentRowDto>> GetStudentsByModuleAsync(Guid moduleId);
    Task<StudentEnrollmentHistoryDto> GetStudentEnrollmentHistoryAsync(Guid studentId);
    Task<StudentGradesDto> GetMyGradesAsync(Guid userId);
}
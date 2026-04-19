using backend.dtos;
using backend.models;
using backend.models.enums;

namespace backend.services.interfaces;

public interface IEnrollmentService
{
    Task<CourseEnrollmentDto> EnrollStudentInCourseAsync(Guid studentId, EnrollInCourseDto dto);
    Task<CourseEnrollmentDto> SetCourseEnrollmentStatusAsync(Guid studentId, CourseEnrollmentStatus status);

    Task<ModuleCardDto> EnrollStudentInModuleAsync(Guid studentId, Guid moduleId, EnrollInModuleDto dto);
    Task<ModuleCardDto> SetModuleEnrollmentStatusAsync(Guid enrollmentId, ModuleEnrollmentStatus status);

    Task<StudentDashboardDto> GetStudentDashboardByUserIdAsync(Guid userId);
    Task<StaffDashboardDto> GetStaffDashboardByUserIdAsync(Guid userId);
    Task<StudentDashboardDto> GetStudentDashboardByStudentIdAsync(Guid studentId);
    Task<StaffDashboardDto> GetStaffDashboardByStaffIdAsync(Guid staffId);
}
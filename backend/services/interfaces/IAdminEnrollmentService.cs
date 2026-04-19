using backend.dtos;
using backend.models.enums;

namespace backend.services.interfaces;

public interface IAdminEnrollmentService
{
    Task EnrollStudentInCourseAsync(Guid studentId, EnrollInCourseDto dto);
    Task SetStudentCourseStatusAsync(Guid studentId, CourseEnrollmentStatus status);

    Task EnrollStudentInModuleAsync(Guid studentId, Guid moduleId, EnrollInModuleDto dto);
    Task EnrollStaffInModuleAsync(Guid staff, Guid moduleId, EnrollInModuleDto dto);
    Task SetModuleEnrollmentStatusAsync(Guid enrollmentId, ModuleEnrollmentStatus status);

    Task DeleteModuleEnrollmentAsync(Guid enrollmentId); 
}
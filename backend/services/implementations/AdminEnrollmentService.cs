using backend.data;
using backend.dtos;
using backend.errors;
using backend.models;
using backend.models.enums;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class AdminEnrollmentService(AppDbContext db) : IAdminEnrollmentService
{
    public async Task EnrollStudentInCourseAsync(Guid studentId, EnrollInCourseDto dto)
    {
        var studentExists = await db.Students.AnyAsync(s => s.Id == studentId && !s.IsDeleted);
        
        if (!studentExists)
        {
            throw new AppException(404, "STUDENT_NOT_FOUND", "Student does not exist.");
        }

        var courseExists = await db.Courses.AnyAsync(c => c.Id == dto.CourseId && !c.IsDeleted);
        
        if (!courseExists)
        {
            throw new AppException(404, "COURSE_NOT_FOUND", "Course does not exist.");
        }

        var activeExists = await db.StudentCourseEnrollments.AnyAsync(e =>
            e.StudentId == studentId && !e.IsDeleted && !e.Course.IsDeleted && e.Status == CourseEnrollmentStatus.Active);

        if (activeExists)
        {
            throw new AppException(409, "ACTIVE_COURSE_EXISTS", "Student already has an active course enrollment.");
        }

        db.StudentCourseEnrollments.Add(new StudentCourseEnrollment
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            AcademicYear = dto.AcademicYear,
            YearOfStudy = dto.YearOfStudy,
            Semester = dto.Semester,
            Status = CourseEnrollmentStatus.Active,
            StartDateUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }

    public async Task SetStudentCourseStatusAsync(Guid studentId, CourseEnrollmentStatus status)
    {
        var enrollment = await db.StudentCourseEnrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e =>
                e.StudentId == studentId &&
                !e.IsDeleted &&
                e.Status == CourseEnrollmentStatus.Active);

        if (enrollment is null)
        {
            throw new AppException(404, "ACTIVE_COURSE_ENROLLMENT_NOT_FOUND", "No active course enrollment found.");
        }

        enrollment.Status = status;
        
        if (status != CourseEnrollmentStatus.Active)
        {
            enrollment.EndDateUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public async Task EnrollStudentInModuleAsync(Guid studentId, Guid moduleId, EnrollInModuleDto dto)
    {
        var studentExists = await db.Students.AnyAsync(s => s.Id == studentId && !s.IsDeleted);

        if (!studentExists)
        {
            throw new AppException(404, "STUDENT_NOT_FOUND", "Student does not exist.");
        }

        var module = await db.Modules.FirstOrDefaultAsync(m => m.Id == moduleId && !m.IsDeleted);

        if (module is null)
        {
            throw new AppException(404, "MODULE_NOT_FOUND", "Module does not exist.");
        }

        // policy: require active course + module belong to that course
        var activeCourseId = await db.StudentCourseEnrollments
            .Where(e => e.StudentId == studentId && !e.IsDeleted && !e.Course.IsDeleted && e.Status == CourseEnrollmentStatus.Active)
            .Select(e => (Guid?)e.CourseId)
            .FirstOrDefaultAsync();

        if (activeCourseId is null)
        {
            throw new AppException(409, "NO_ACTIVE_COURSE", "Student must have an active course enrollment.");
        }

        if (module.CourseId != activeCourseId.Value)
        {
            throw new AppException(409, "MODULE_NOT_IN_COURSE",
                "Module does not belong to the student's enrolled course.");
        }

        var duplicate = await db.StudentModuleEnrollments.AnyAsync(e =>
            e.StudentId == studentId &&
            e.ModuleId == moduleId &&
            e.AcademicYear == dto.AcademicYear &&
            e.Semester == dto.Semester &&
            !e.IsDeleted);

        if (duplicate)
        {
            throw new AppException(409, "DUPLICATE_MODULE_ENROLLMENT", "Duplicate module enrollment for the same term.");
        }

        db.StudentModuleEnrollments.Add(new StudentModuleEnrollment
        {
            StudentId = studentId,
            ModuleId = moduleId,
            AcademicYear = dto.AcademicYear,
            YearOfStudy = dto.YearOfStudy,
            Semester = dto.Semester,
            Status = ModuleEnrollmentStatus.Enrolled,
            EnrolledAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }
    
    public async Task EnrollStaffInModuleAsync(Guid staffId, Guid moduleId, EnrollInModuleDto dto)
    {
        var staffExists = await db.Staff.AnyAsync(s => s.Id == staffId && !s.IsDeleted);

        if (!staffExists)
        {
            throw new AppException(404, "STAFF_NOT_FOUND", "Staff does not exist.");
        }

        var module = await db.Modules.FirstOrDefaultAsync(m => m.Id == moduleId && !m.IsDeleted);

        if (module is null)
        {
            throw new AppException(404, "MODULE_NOT_FOUND", "Module does not exist.");
        }

        var duplicate = await db.ModuleStaff.AnyAsync(e =>
            e.StaffId == staffId &&
            e.ModuleId == moduleId &&
            !e.IsDeleted);

        if (duplicate)
        {
            throw new AppException(409, "DUPLICATE_MODULE_ENROLLMENT", "Duplicate module enrollment.");
        }

        db.ModuleStaff.Add(new ModuleStaff
        {
            StaffId = staffId,
            ModuleId = moduleId,
            Role = "Staff"
        });

        await db.SaveChangesAsync();
    }

    public async Task SetModuleEnrollmentStatusAsync(Guid enrollmentId, ModuleEnrollmentStatus status)
    {
        var enrollment = await db.StudentModuleEnrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId && !e.IsDeleted);
        
        if (enrollment is null)
        {
            throw new AppException(404, "MODULE_ENROLLMENT_NOT_FOUND", "Module enrollment does not exist.");
        }

        enrollment.Status = status;

        if (status is ModuleEnrollmentStatus.Completed or ModuleEnrollmentStatus.Withdrawn or ModuleEnrollmentStatus.Failed)
        {
            enrollment.CompletedAtUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteModuleEnrollmentAsync(Guid enrollmentId)
    {
        var enrollment = await db.StudentModuleEnrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId && !e.IsDeleted);

        if (enrollment is null)
        {
            return;
        }

        enrollment.IsDeleted = true;
        await db.SaveChangesAsync();
    }
}
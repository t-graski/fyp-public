using backend.data;
using backend.dtos;
using backend.errors;
using backend.models;
using backend.models.enums;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class EnrollmentService(AppDbContext db) : IEnrollmentService
{
    public async Task<CourseEnrollmentDto> EnrollStudentInCourseAsync(Guid studentId, EnrollInCourseDto dto)
    {
        var studentExists = await db.Students.AnyAsync(s => s.Id == studentId);
        
        if (!studentExists)
        {
            throw new AppException(404, "STUDENT_NOT_FOUND", "Student does not exist.");
        }

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId);
        
        if (course is null)
        {
            throw new AppException(404, "COURSE_NOT_FOUND", "Course does not exist.");
        }

        var active = await db.StudentCourseEnrollments
            .Where(x => x.StudentId == studentId && !x.IsDeleted && !x.Course.IsDeleted &&
                        x.Status == CourseEnrollmentStatus.Active)
            .FirstOrDefaultAsync();

        if (active is not null)
        {
            throw new AppException(409, "COURSE_ENROLLMENT_EXISTS", "Student already has an active course enrollment.");
        }

        var duplicate = await db.StudentCourseEnrollments.AnyAsync(x =>
            x.StudentId == studentId &&
            x.CourseId == dto.CourseId &&
            x.AcademicYear == dto.AcademicYear &&
            x.Semester == dto.Semester &&
            !x.IsDeleted);

        if (duplicate)
        {
            throw new AppException(409, "DUPLICATE_ENROLLMENT", "Duplicate course enrollment for the same term.");
        }

        var enrollment = new StudentCourseEnrollment
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            AcademicYear = dto.AcademicYear,
            YearOfStudy = dto.YearOfStudy,
            Semester = dto.Semester,
            Status = CourseEnrollmentStatus.Active,
            StartDateUtc = DateTimeOffset.UtcNow
        };

        db.StudentCourseEnrollments.Add(enrollment);
        await db.SaveChangesAsync();

        return await GetActiveCourseEnrollmentDto(studentId)
               ?? throw new AppException(500, "ENROLLMENT_CREATE_FAILED", "Failed to create course enrollment.");
    }

    public async Task<CourseEnrollmentDto> SetCourseEnrollmentStatusAsync(Guid studentId, CourseEnrollmentStatus status)
    {
        var enrollment = await db.StudentCourseEnrollments
            .Include(x => x.Course)
            .Where(x => x.StudentId == studentId && !x.IsDeleted && x.Status == CourseEnrollmentStatus.Active)
            .FirstOrDefaultAsync();

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

        return new CourseEnrollmentDto(
            enrollment.CourseId,
            enrollment.Course.CourseCode,
            enrollment.Course.Title,
            enrollment.AcademicYear,
            enrollment.YearOfStudy,
            enrollment.Semester,
            enrollment.Status
        );
    }

    public async Task<ModuleCardDto> EnrollStudentInModuleAsync(Guid studentId, Guid moduleId, EnrollInModuleDto dto)
    {
        var studentExists = await db.Students.AnyAsync(s => s.Id == studentId);
        
        if (!studentExists)
        {
            throw new AppException(404, "STUDENT_NOT_FOUND", "Student does not exist.");
        }

        var module = await db.Modules
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == moduleId);

        if (module is null)
        {
            throw new AppException(404, "MODULE_NOT_FOUND", "Module does not exist.");
        }

        var activeCourse = await db.StudentCourseEnrollments
            .Where(x => x.StudentId == studentId && !x.IsDeleted && !x.Course.IsDeleted &&
                        x.Status == CourseEnrollmentStatus.Active)
            .Select(x => new { x.CourseId, x.AcademicYear, x.YearOfStudy, x.Semester })
            .FirstOrDefaultAsync();

        if (activeCourse is null)
        {
            throw new AppException(409, "NO_ACTIVE_COURSE",
                "Student must have an active course enrollment before enrolling in modules.");
        }

        if (module.CourseId != activeCourse.CourseId)
        {
            throw new AppException(409, "MODULE_NOT_IN_COURSE",
                "Module does not belong to the student's enrolled course.");
        }

        var exists = await db.StudentModuleEnrollments.AnyAsync(x =>
            x.StudentId == studentId &&
            x.ModuleId == moduleId &&
            x.AcademicYear == dto.AcademicYear &&
            x.Semester == dto.Semester &&
            !x.IsDeleted);

        if (exists)
        {
            throw new AppException(409, "DUPLICATE_MODULE_ENROLLMENT",
                "Student is already enrolled in this module for the term.");
        }

        var enrollment = new StudentModuleEnrollment
        {
            StudentId = studentId,
            ModuleId = moduleId,
            AcademicYear = dto.AcademicYear,
            YearOfStudy = dto.YearOfStudy,
            Semester = dto.Semester,
            Status = ModuleEnrollmentStatus.Enrolled,
            EnrolledAtUtc = DateTimeOffset.UtcNow
        };

        db.StudentModuleEnrollments.Add(enrollment);
        await db.SaveChangesAsync();

        return await GetModuleEnrollmentDto(enrollment.Id);
    }

    public async Task<ModuleCardDto> SetModuleEnrollmentStatusAsync(Guid enrollmentId, ModuleEnrollmentStatus status)
    {
        var enrollment = await db.StudentModuleEnrollments
            .Include(x => x.Module)
            .Where(x => x.Id == enrollmentId && !x.IsDeleted)
            .FirstOrDefaultAsync();

        if (enrollment is null)
        {
            throw new AppException(404, "MODULE_ENROLLMENT_NOT_FOUND", "Module enrollment does not exist.");
        }

        enrollment.Status = status;

        if (status is ModuleEnrollmentStatus.Completed or ModuleEnrollmentStatus.Withdrawn
            or ModuleEnrollmentStatus.Failed)
        {
            enrollment.CompletedAtUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();

        return new ModuleCardDto(
            enrollment.ModuleId,
            enrollment.Module.ModuleCode,
            enrollment.Module.Title,
            enrollment.AcademicYear,
            enrollment.YearOfStudy,
            enrollment.Semester,
            enrollment.Status
        );
    }

    public async Task<StudentDashboardDto> GetStudentDashboardByUserIdAsync(Guid userId)
    {
        var studentId = await db.Students
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

        if (studentId is null)
        {
            throw new AppException(404, "STUDENT_PROFILE_NOT_FOUND", "No student profile linked to this user.");
        }

        return await GetStudentDashboardByStudentIdAsync(studentId.Value);
    }

    public async Task<StaffDashboardDto> GetStaffDashboardByUserIdAsync(Guid userId)
    {
        var staffId = await db.Staff
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

        if (staffId is null)
        {
            throw new AppException(404, "STAFF_PROFILE_NOT_FOUND", "No staff profile linked to this user.");
        }

        return await GetStaffDashboardByStaffIdAsync(staffId.Value);
    }

    public async Task<StudentDashboardDto> GetStudentDashboardByStudentIdAsync(Guid studentId)
    {
        var enrolledCourse = await GetActiveCourseEnrollmentDto(studentId);

        var current = await db.StudentModuleEnrollments.AsNoTracking()
            .Where(x => x.StudentId == studentId && !x.IsDeleted && !x.Module.IsDeleted &&
                        x.Status == ModuleEnrollmentStatus.Enrolled)
            .OrderByDescending(x => x.AcademicYear)
            .ThenByDescending(x => x.Semester)
            .Select(x => new ModuleCardDto(
                x.ModuleId,
                x.Module.ModuleCode,
                x.Module.Title,
                x.AcademicYear,
                x.YearOfStudy,
                x.Semester,
                x.Status
            ))
            .ToListAsync();

        var past = await db.StudentModuleEnrollments.AsNoTracking()
            .Where(x => x.StudentId == studentId && !x.IsDeleted && !x.Module.IsDeleted &&
                        (x.Status == ModuleEnrollmentStatus.Completed || x.Status == ModuleEnrollmentStatus.Withdrawn ||
                         x.Status == ModuleEnrollmentStatus.Failed))
            .OrderByDescending(x => x.AcademicYear)
            .ThenByDescending(x => x.Semester)
            .Select(x => new ModuleCardDto(
                x.ModuleId,
                x.Module.ModuleCode,
                x.Module.Title,
                x.AcademicYear,
                x.YearOfStudy,
                x.Semester,
                x.Status
            ))
            .ToListAsync();

        return new StudentDashboardDto(enrolledCourse, current, past);
    }

    public async Task<StaffDashboardDto> GetStaffDashboardByStaffIdAsync(Guid staffId)
    {
        var modules = await db.ModuleStaff
            .AsNoTracking()
            .Where(ms => ms.StaffId == staffId && !ms.IsDeleted)
            .Include(ms => ms.Module)
            .Select(x => new StaffModuleCardDto(
                x.ModuleId,
                x.Module.ModuleCode,
                x.Module.Title,
                x.Role ?? string.Empty
            ))
            .ToListAsync();

        return new StaffDashboardDto(modules);
    }

    private async Task<CourseEnrollmentDto?> GetActiveCourseEnrollmentDto(Guid studentId)
    {
        return await db.StudentCourseEnrollments.AsNoTracking()
            .Where(x => x.StudentId == studentId && !x.IsDeleted && !x.Course.IsDeleted &&
                        x.Status == CourseEnrollmentStatus.Active)
            .OrderByDescending(x => x.AcademicYear)
            .ThenByDescending(x => x.Semester)
            .Select(x => new CourseEnrollmentDto(
                x.CourseId,
                x.Course.CourseCode,
                x.Course.Title,
                x.AcademicYear,
                x.YearOfStudy,
                x.Semester,
                x.Status
            ))
            .FirstOrDefaultAsync();
    }

    private async Task<ModuleCardDto> GetModuleEnrollmentDto(Guid enrollmentId)
    {
        var dto = await db.StudentModuleEnrollments.AsNoTracking()
            .Where(x => x.Id == enrollmentId && !x.IsDeleted && !x.Module.IsDeleted)
            .Select(x => new ModuleCardDto(
                x.ModuleId,
                x.Module.ModuleCode,
                x.Module.Title,
                x.AcademicYear,
                x.YearOfStudy,
                x.Semester,
                x.Status
            ))
            .FirstOrDefaultAsync();

        if (dto is null)
        {
            throw new AppException(404, "MODULE_ENROLLMENT_NOT_FOUND", "Module enrollment does not exist.");
        }

        return dto;
    }
}
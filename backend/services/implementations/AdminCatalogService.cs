using backend.data;
using backend.dtos;
using backend.errors;
using backend.models;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class AdminCatalogService(AppDbContext db) : IAdminCatalogService
{
    public async Task<IReadOnlyList<AdminCourseDto>> ListCoursesAsync(string? q)
    {
        var query = db.Courses.AsNoTracking().Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.CourseCode.ToLower().Contains(s) ||
                c.Title.ToLower().Contains(s));
        }

        return await query
            .OrderBy(c => c.CourseCode)
            .Select(c => new AdminCourseDto(
                c.Id,
                c.CourseCode,
                c.Title,
                c.Description,
                c.Award,
                c.DurationSemesters,
                c.IsActive
            ))
            .ToListAsync();
    }

    public async Task<AdminCourseDetailDto> GetCourseAsync(Guid id)
    {
        var dto = await db.Courses.AsNoTracking()
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => new AdminCourseDetailDto(
                c.Id,
                c.CourseCode,
                c.Title,
                c.Description,
                c.Award,
                c.DurationSemesters,
                c.IsActive,
                c.Modules.Where(m => !m.IsDeleted)
                    .OrderBy(m => m.ModuleCode)
                    .Select(m => new AdminModuleDto(
                        m.Id,
                        m.CourseId,
                        m.ModuleCode,
                        m.Title,
                        m.Description,
                        m.Credits,
                        m.Level,
                        m.AcademicYear,
                        m.SemesterOfStudy,
                        m.Term,
                        m.IsCore,
                        m.RunsFrom,
                        m.RunsTo,
                        m.ScheduledDay,
                        m.ScheduledStartLocal,
                        m.ScheduledEndLocal
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync();

        if (dto is null)
        {
            throw new AppException(404, "COURSE_NOT_FOUND", "Course does not exist.");
        }

        return dto;
    }

    public async Task<AdminCourseDetailDto> CreateCourseAsync(CreateCourseDto dto)
    {
        var code = dto.CourseCode.Trim();
        var exists = await db.Courses.AnyAsync(c => c.CourseCode == code && !c.IsDeleted);

        if (exists)
        {
            throw new AppException(409, "COURSE_CODE_EXISTS", "Course code already exists.");
        }

        var course = new Course
        {
            CourseCode = code,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Award = dto.Award?.Trim(),
            DurationSemesters = dto.DurationSemesters,
            IsActive = dto.IsActive
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync();

        return await GetCourseAsync(course.Id);
    }

    public async Task<AdminCourseDetailDto> UpdateCourseAsync(Guid id, UpdateCourseDto dto)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (course is null)
        {
            throw new AppException(404, "COURSE_NOT_FOUND", "Course does not exist.");
        }

        course.CourseCode = dto.CourseCode.Trim();
        course.Title = dto.Title.Trim();
        course.Description = dto.Description?.Trim();
        course.Award = dto.Award?.Trim();
        course.DurationSemesters = dto.DurationSemesters;
        course.IsActive = dto.IsActive;

        await db.SaveChangesAsync();

        return await GetCourseAsync(id);
    }

    public async Task DeleteCourseAsync(Guid id)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (course is null)
        {
            return;
        }

        db.Courses.Remove(course);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AdminModuleDto>> ListModulesByCourseAsync(Guid courseId)
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId && !c.IsDeleted);

        if (!courseExists)
        {
            throw new AppException(404, "COURSE_NOT_FOUND", "Course does not exist.");
        }

        return await db.Modules.AsNoTracking()
            .Where(m => m.CourseId == courseId && !m.IsDeleted)
            .OrderBy(m => m.ModuleCode)
            .Select(m => new AdminModuleDto(
                m.Id,
                m.CourseId,
                m.ModuleCode,
                m.Title,
                m.Description,
                m.Credits,
                m.Level,
                m.AcademicYear,
                m.SemesterOfStudy,
                m.Term,
                m.IsCore,
                m.RunsFrom,
                m.RunsTo,
                m.ScheduledDay,
                m.ScheduledStartLocal,
                m.ScheduledEndLocal
            ))
            .ToListAsync();
    }

    public async Task<AdminModuleDto> GetModuleAsync(Guid id)
    {
        var dto = await db.Modules.AsNoTracking()
            .Where(m => m.Id == id && !m.IsDeleted)
            .Select(m => new AdminModuleDto(
                m.Id,
                m.CourseId,
                m.ModuleCode,
                m.Title,
                m.Description,
                m.Credits,
                m.Level,
                m.AcademicYear,
                m.SemesterOfStudy,
                m.Term,
                m.IsCore,
                m.RunsFrom,
                m.RunsTo,
                m.ScheduledDay,
                m.ScheduledStartLocal,
                m.ScheduledEndLocal
            ))
            .FirstOrDefaultAsync();

        if (dto is null)
        {
            throw new AppException(404, "MODULE_NOT_FOUND", "Module does not exist.");
        }

        return dto;
    }

    public async Task<AdminModuleDto> CreateModuleAsync(Guid courseId, CreateModuleDto dto)
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId && !c.IsDeleted);

        if (!courseExists)
        {
            throw new AppException(404, "COURSE_NOT_FOUND", "Course does not exist.");
        }

        var code = dto.ModuleCode.Trim();
        var exists = await db.Modules.AnyAsync(m => m.CourseId == courseId && m.ModuleCode == code && !m.IsDeleted);

        if (exists)
        {
            throw new AppException(409, "MODULE_CODE_EXISTS", "Module code already exists for this course.");
        }

        var module = new Module
        {
            CourseId = courseId,
            ModuleCode = code,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Credits = dto.Credits,
            Level = dto.Level,
            AcademicYear = dto.AcademicYear,
            SemesterOfStudy = dto.SemesterOfStudy,
            Term = dto.Term?.Trim(),
            IsCore = dto.IsCore,
            RunsFrom = dto.RunsFrom,
            RunsTo = dto.RunsTo,
            ScheduledDay = dto.ScheduledDay,
            ScheduledStartLocal = dto.ScheduledStartLocal,
            ScheduledEndLocal = dto.ScheduledEndLocal
        };

        db.Modules.Add(module);
        await db.SaveChangesAsync();

        return await GetModuleAsync(module.Id);
    }

    public async Task<AdminModuleDto> UpdateModuleAsync(Guid id, UpdateModuleDto dto)
    {
        var module = await db.Modules.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (module is null)
        {
            throw new AppException(404, "MODULE_NOT_FOUND", "Module does not exist.");
        }

        module.ModuleCode = dto.ModuleCode.Trim();
        module.Title = dto.Title.Trim();
        module.Description = dto.Description?.Trim();
        module.Credits = dto.Credits;
        module.Level = dto.Level;
        module.AcademicYear = dto.AcademicYear;
        module.SemesterOfStudy = dto.SemesterOfStudy;
        module.Term = dto.Term?.Trim();
        module.IsCore = dto.IsCore;
        module.RunsFrom = dto.RunsFrom;
        module.RunsTo = dto.RunsTo;
        module.ScheduledDay = dto.ScheduledDay;
        module.ScheduledStartLocal = dto.ScheduledStartLocal;

        await db.SaveChangesAsync();

        return await GetModuleAsync(id);
    }

    public async Task DeleteModuleAsync(Guid id)
    {
        var module = await db.Modules.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (module is null)
        {
            return;
        }

        db.Modules.Remove(module);
        await db.SaveChangesAsync();
    }
}
using backend.data;
using backend.dtos;
using backend.errors;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class EnrollmentQueryService(AppDbContext db) : IEnrollmentQueryService
{
    public async Task<IReadOnlyList<CourseEnrollmentRowDto>> GetStudentsByCourseAsync(Guid courseId)
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == courseId && !c.IsDeleted);

        if (!courseExists)
        {
            throw new AppException(404, "COURSE_NOT_FOUND", "Course does not exist.");
        }

        return await db.StudentCourseEnrollments.AsNoTracking()
            .Where(e => e.CourseId == courseId && !e.IsDeleted)
            .OrderByDescending(e => e.AcademicYear)
            .ThenByDescending(e => e.Semester)
            .ThenBy(e => e.Student.StudentNumber)
            .Select(e => new CourseEnrollmentRowDto(
                new StudentListItemDto(
                    e.Student.Id,
                    e.Student.User.Id,
                    e.Student.User.Email,
                    e.Student.User.FirstName,
                    e.Student.User.LastName,
                    e.Student.StudentNumber
                ),
                e.Id,
                e.Status,
                e.AcademicYear,
                e.YearOfStudy,
                e.Semester,
                e.StartDateUtc,
                e.EndDateUtc
            ))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ModuleEnrollmentRowDto>> GetStudentsByModuleAsync(Guid moduleId)
    {
        var moduleExists = await db.Modules.AnyAsync(m => m.Id == moduleId && !m.IsDeleted);

        if (!moduleExists)
        {
            throw new AppException(404, "MODULE_NOT_FOUND", "Module does not exist.");
        }

        return await db.StudentModuleEnrollments.AsNoTracking()
            .Where(e => e.ModuleId == moduleId && !e.IsDeleted)
            .OrderByDescending(e => e.AcademicYear)
            .ThenByDescending(e => e.Semester)
            .ThenBy(e => e.Student.StudentNumber)
            .Select(e => new ModuleEnrollmentRowDto(
                new StudentListItemDto(
                    e.Student.Id,
                    e.Student.User.Id,
                    e.Student.User.Email,
                    e.Student.User.FirstName,
                    e.Student.User.LastName,
                    e.Student.StudentNumber
                ),
                e.Id,
                e.Status,
                e.AcademicYear,
                e.YearOfStudy,
                e.Semester,
                e.EnrolledAtUtc,
                e.CompletedAtUtc
            ))
            .ToListAsync();
    }

    public async Task<StudentEnrollmentHistoryDto> GetStudentEnrollmentHistoryAsync(Guid studentId)
    {
        var student = await db.Students.AsNoTracking()
            .Where(s => s.Id == studentId && !s.IsDeleted)
            .Select(s => new
            {
                s.Id,
                s.StudentNumber,
                User = new { s.User.Id, s.User.Email, s.User.FirstName, s.User.LastName }
            })
            .FirstOrDefaultAsync();

        if (student is null)
        {
            throw new AppException(404, "STUDENT_NOT_FOUND", "Student does not exist.");
        }

        var courses = await db.StudentCourseEnrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && !e.IsDeleted)
            .OrderByDescending(e => e.AcademicYear)
            .ThenByDescending(e => e.Semester)
            .Select(e => new StudentCourseHistoryRowDto(
                e.Id,
                e.CourseId,
                e.Course.CourseCode,
                e.Course.Title,
                e.Status,
                e.AcademicYear,
                e.YearOfStudy,
                e.Semester,
                e.StartDateUtc,
                e.EndDateUtc
            ))
            .ToListAsync();

        var modules = await db.StudentModuleEnrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && !e.IsDeleted)
            .OrderByDescending(e => e.AcademicYear)
            .ThenByDescending(e => e.Semester)
            .Select(e => new StudentModuleHistoryRowDto(
                e.Id,
                e.ModuleId,
                e.Module.ModuleCode,
                e.Module.Title,
                e.Status,
                e.AcademicYear,
                e.YearOfStudy,
                e.Semester,
                e.EnrolledAtUtc,
                e.CompletedAtUtc
            ))
            .ToListAsync();

        return new StudentEnrollmentHistoryDto(
            student.Id,
            student.User.Id,
            student.User.Email,
            student.User.FirstName,
            student.User.LastName,
            student.StudentNumber,
            courses,
            modules
        );
    }

    public async Task<StudentGradesDto> GetMyGradesAsync(Guid userId)
    {
        var studentId = await db.Students.AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

        if (studentId is null)
        {
            throw new AppException(404, "STUDENT_NOT_FOUND", "Student record not found.");
        }


        var moduleEnrollments = await db.StudentModuleEnrollments
            .AsNoTracking()
            .Where(me => me.StudentId == studentId && !me.IsDeleted)
            .Select(me => new
            {
                me.ModuleId,
                me.Module.ModuleCode,
                me.Module.Title,
                me.Module.AcademicYear,
                me.Module.Credits,
                me.Module.Level,
                me.Module.CourseId,
                me.Module.Course.CourseCode,
                CourseTitle = me.Module.Course.Title,
                me.Module.Course.Description,
                me.Module.Course.Award,
                Elements = me.Module.Elements
                    .Where(el => !el.IsDeleted && el.MarksPublished && el.AssessmentWeight != null)
                    .Select(el => new
                    {
                        el.Id,
                        el.AssessmentWeight,
                        el.MarksPublished
                    })
                    .ToList()
            })
            .ToListAsync();

        var moduleIds = moduleEnrollments.Select(me => me.ModuleId).ToList();
        var grades = await db.AssessmentGrades.AsNoTracking()
            .Where(g => g.StudentId == studentId
                        && !g.IsDeleted
                        && moduleIds.Contains(g.AssessmentElement.ModuleId))
            .Select(g => new
            {
                g.Id,
                g.AssessmentElementId,
                g.Grade,
                g.Feedback,
                g.GradedAtUtc
            })
            .ToListAsync();

        var gradesByElementId = grades.ToDictionary(g => g.AssessmentElementId);

        var courseGroups = moduleEnrollments
            .GroupBy(me => me.CourseId)
            .Select(cg =>
            {
                var first = cg.First();
                var modules = cg.Select(me =>
                {
                    var assessments = me.Elements
                        .Where(el => gradesByElementId.ContainsKey(el.Id))
                        .Select(el =>
                        {
                            var g = gradesByElementId[el.Id];
                            return new StudentAssessmentGradeDto(
                                el.Id,
                                el.AssessmentWeight!.Value,
                                el.MarksPublished,
                                g.Id,
                                g.Grade,
                                g.Feedback,
                                g.GradedAtUtc
                            );
                        })
                        .ToList();

                    return new StudentModuleGradesDto(
                        me.ModuleId,
                        me.ModuleCode,
                        me.Title,
                        me.AcademicYear ?? 0,
                        me.Credits,
                        me.Level,
                        me.Elements.Any(el => el.MarksPublished),
                        assessments
                    );
                }).ToList();

                return new StudentCourseGradesDto(
                    cg.Key,
                    first.CourseCode,
                    first.CourseTitle,
                    first.Description,
                    first.Award,
                    modules
                );
            })
            .ToList();

        return new StudentGradesDto(courseGroups);
    }
}
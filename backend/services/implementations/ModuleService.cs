using backend.auth;
using backend.data;
using backend.dtos;
using backend.errors;
using backend.helpers;
using backend.helpers.interfaces;
using backend.models;
using backend.models.enums;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class ModuleService(AppDbContext db, ICurrentUser currentUser, IDatabaseGuards guards) : IModuleService
{
    public async Task<ModuleDto> GetByIdAsync(Guid moduleId)
    {
        var userId = currentUser.UserId ?? throw new AppException(401, "UNAUTHORIZED", "Authentication required.");

        var studentId = await guards.TryGetStudentIdAsync(userId);
        var staffId = await guards.TryGetStaffIdAsync(userId);

        if (studentId is null && staffId is null)
        {
            throw new AppException(404, "STUDENT_OR_STAFF_PROFILE_NOT_FOUND",
                "No student or staff profile linked to this user.");
        }

        var canAccess =
            await db.StudentModuleEnrollments.AnyAsync(x =>
                x.ModuleId == moduleId && x.StudentId == studentId && !x.IsDeleted)
            || await db.ModuleStaff.AnyAsync(x => x.ModuleId == moduleId && x.StaffId == staffId && !x.IsDeleted);

        if (!canAccess)
        {
            throw new AppException(403, "FORBIDDEN", "You are not allowed to access this module.");
        }

        var module = await db.Modules
            .AsNoTracking()
            .Include(m => m.Course)
            .Include(m => m.Elements.Where(e => !e.IsDeleted))
            .Include(m => m.StudentEnrollments.Where(se => !se.IsDeleted))
            .ThenInclude(se => se.Student)
            .ThenInclude(se => se.User)
            .Include(m => m.TeachingStaff.Where(ts => !ts.IsDeleted))
            .ThenInclude(ts => ts.Staff)
            .ThenInclude(s => s.User)
            .Where(m => m.Id == moduleId && !m.IsDeleted)
            .FirstOrDefaultAsync();

        if (module is null)
        {
            throw new AppException(404, "MODULE_NOT_FOUND", "Module does not exist.");
        }

        var studentEnrollment = await db.StudentModuleEnrollments
            .AsNoTracking()
            .Where(x => x.ModuleId == moduleId && x.StudentId == studentId && !x.IsDeleted)
            .Select(x => new { x.AcademicYear, x.YearOfStudy, x.Semester })
            .FirstOrDefaultAsync();

        var academicYear = studentEnrollment?.AcademicYear ?? (module.AcademicYear ?? 0);
        var yearOfStudy = studentEnrollment?.YearOfStudy ?? (module.Level ?? 0);
        var semester = studentEnrollment?.Semester ?? (short)(module.SemesterOfStudy ?? 0);

        var students = module.StudentEnrollments
            .Where(se => !se.Student.User.IsDeleted)
            .Select(se => new ModuleMemberDto(
                se.Student.User.Id,
                $"{se.Student.User.FirstName} {se.Student.User.LastName}",
                se.StudentId.ToString(),
                se.Student.User.Email))
            .ToList();

        var staff = module.TeachingStaff
            .Select(ts => ts.Staff)
            .Select(s => s.User)
            .Where(u => !u.IsDeleted)
            .Select(u => new ModuleMemberDto(u.Id, $"{u.FirstName} {u.LastName}", string.Empty, u.Email))
            .ToList();

        var elements = module.Elements
            .OrderBy(e => e.SortOrder)
            .Select(e => new ModuleElementDto(
                e.Id,
                e.SortOrder,
                e.Type,
                e.IconKey,
                e.Options.RootElement.Clone(),
                e.AssessmentWeight,
                e.MarksPublished
            ))
            .ToList();

        return new ModuleDto(
            module.Id,
            module.CourseId,
            module.Course.CourseCode,
            module.Course.Title,
            module.ModuleCode,
            module.Title,
            module.Description,
            module.Credits,
            module.Level,
            academicYear,
            yearOfStudy,
            semester,
            module.IsCore,
            module.RunsFrom,
            module.RunsTo,
            module.ScheduledDay,
            module.ScheduledStartLocal,
            module.ScheduledEndLocal,
            students,
            staff,
            elements
        );
    }

    public async Task<ModuleElementDto> CreateElementAsync(Guid moduleId, CreateModuleElementDto dto)
    {
        await guards.EnsureTeachingStaffAsync(moduleId);

        ValidateElementPayload(dto.Type, dto.AssessmentWeight);

        var nextOrder = await db.ModuleElements
            .Where(e => e.ModuleId == moduleId && !e.IsDeleted)
            .Select(e => (int?)e.SortOrder)
            .MaxAsync();

        var element = new ModuleElement
        {
            ModuleId = moduleId,
            Type = dto.Type,
            SortOrder = (nextOrder ?? 0) + 1,
            IconKey = dto.IconKey,
            Options = JsonHelpers.ToDocument(dto.Options),
            AssessmentWeight = dto.Type == ModuleElementType.Assessment ? dto.AssessmentWeight : null,
            MarksPublished = false
        };

        if (dto.Type == ModuleElementType.Assessment)
        {
            await EnsureTotalAssessmentWeightWithinLimitAsync(moduleId, additionalWeight: dto.AssessmentWeight!.Value);
        }

        db.ModuleElements.Add(element);
        await db.SaveChangesAsync();

        return ToDto(element);
    }

    public async Task<ModuleElementDto> UpdateElementAsync(Guid moduleId, Guid elementId, UpdateModuleElementDto dto)
    {
        await guards.EnsureTeachingStaffAsync(moduleId);

        var element = await db.ModuleElements
            .FirstOrDefaultAsync(e => e.Id == elementId && e.ModuleId == moduleId && !e.IsDeleted);

        if (element is null)
        {
            throw new AppException(404, "ELEMENT_NOT_FOUND", "Module element does not exist.");
        }

        ValidateElementPayload(element.Type, dto.AssessmentWeight);

        element.IconKey = dto.IconKey;
        element.Options = JsonHelpers.ToDocument(dto.Options);

        if (element.Type == ModuleElementType.Assessment)
        {
            var oldWeight = element.AssessmentWeight ?? 0;
            var newWeight = dto.AssessmentWeight!.Value;

            await EnsureTotalAssessmentWeightWithinLimitAsync(moduleId, newWeight - oldWeight);

            element.AssessmentWeight = newWeight;
        }
        else
        {
            element.AssessmentWeight = null;
            element.MarksPublished = false;
        }

        await db.SaveChangesAsync();
        return ToDto(element);
    }

    public async Task DeleteElementAsync(Guid moduleId, Guid elementId)
    {
        await guards.EnsureTeachingStaffAsync(moduleId);

        var element = await db.ModuleElements
            .FirstOrDefaultAsync(e => e.Id == elementId && e.ModuleId == moduleId && !e.IsDeleted);

        if (element is null)
        {
            return;
        }

        element.IsDeleted = true;
        await db.SaveChangesAsync();
        await CompactSortOrderAsync(moduleId);
    }

    public async Task ReorderElementsAsync(Guid moduleId, ReorderModuleElementsDto dto)
    {
        await guards.EnsureTeachingStaffAsync(moduleId);

        if (dto.ElementIdsInOrder is null || dto.ElementIdsInOrder.Count == 0)
        {
            throw new AppException(400, "INVALID_ORDER", "ElementIdsInOrder cannot be empty.");
        }

        var elements = await db.ModuleElements
            .Where(e => e.ModuleId == moduleId && !e.IsDeleted)
            .ToListAsync();

        if (elements.Count == 0)
        {
            throw new AppException(404, " NO_ELEMENT", "Module has no elements to reorder.");
        }

        var existingIds = elements.Select(e => e.Id).ToHashSet();
        var providedIds = dto.ElementIdsInOrder.ToHashSet();

        if (!existingIds.SetEquals(providedIds))
        {
            throw new AppException(400, "ORDER_MISMATCH", "Order must include all existing element IDs exactly once.");
        }

        // temporary move everything to a unique sortOrder to avoid collisions
        const int offset = 1_000_000;
        foreach (var element in elements)
        {
            element.SortOrder += offset;
        }

        await db.SaveChangesAsync();

        for (var i = 0; i < dto.ElementIdsInOrder.Count; i++)
        {
            var id = dto.ElementIdsInOrder[i];
            var element = elements.First(e => e.Id == id);
            element.SortOrder = i + 1;
        }

        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AssessmentGradeDto>> ListAssessmentGradesAsync(Guid moduleId,
        Guid assessmentElementId)
    {
        var userId = currentUser.UserId ?? throw new AppException(401, "UNAUTHORIZED", "Authentication required.");

        var staffId = await db.Staff
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

        var isStaff = staffId.HasValue && await db.ModuleStaff
            .AnyAsync(ms => ms.ModuleId == moduleId && ms.StaffId == staffId && !ms.IsDeleted);

        if (isStaff)
        {
            await GetAssessmentElementOrThrowAsync(moduleId, assessmentElementId);

            return await db.AssessmentGrades
                .AsNoTracking()
                .Where(g => g.AssessmentElementId == assessmentElementId && !g.IsDeleted)
                .OrderBy(g => g.StudentId)
                .Select(g => new AssessmentGradeDto(
                    g.Id,
                    g.AssessmentElementId,
                    g.StudentId,
                    g.Grade,
                    g.Feedback,
                    g.GradedByStaffId,
                    g.GradedAtUtc
                ))
                .ToListAsync();
        }

        var studentId = await db.Students
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

        if (studentId is null)
        {
            throw new AppException(403, "FORBIDDEN", "No student profile linked to this user");
        }

        var isEnrolled = await db.StudentModuleEnrollments
            .AnyAsync(e => e.ModuleId == moduleId && e.StudentId == studentId && !e.IsDeleted);

        if (!isEnrolled)
        {
            throw new AppException(403, "FORBIDDEN", "You are not enrolled in this module.");
        }

        var element = await GetAssessmentElementOrThrowAsync(moduleId, assessmentElementId);

        if (!element.MarksPublished)
        {
            throw new AppException(403, "MARKS_NOT_PUBLISHED",
                "Marks for this assessment have not been published yet.");
        }

        return await db.AssessmentGrades
            .AsNoTracking()
            .Where(g => g.AssessmentElementId == assessmentElementId && g.StudentId == studentId && !g.IsDeleted)
            .Select(g => new AssessmentGradeDto(
                g.Id,
                g.AssessmentElementId,
                g.StudentId,
                g.Grade,
                g.Feedback,
                g.GradedByStaffId,
                g.GradedAtUtc
            ))
            .ToListAsync();
    }

    public async Task<AssessmentGradeDto> UpsertAssessmentGradeAsync(Guid moduleId, Guid assessmentElementId,
        Guid studentId,
        UpsertAssessmentGradeDto dto)
    {
        await guards.EnsureTeachingStaffAsync(moduleId);
        await GetAssessmentElementOrThrowAsync(moduleId, assessmentElementId);

        var enrolled = await db.StudentModuleEnrollments.AnyAsync(e =>
            e.ModuleId == moduleId && e.StudentId == studentId && !e.IsDeleted);

        if (!enrolled)
        {
            throw new AppException(400, "STUDENT_NOT_ENROLLED", "Student is not enrolled in this module.");
        }

        var staffId = await db.Staff
            .Where(s => s.UserId == currentUser.UserId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

        if (staffId is null)
        {
            throw new AppException(404, "STAFF_PROFILE_NOT_FOUND", "No staff profile linked to this user.");
        }

        if (dto.Grade is < 0 or > 100)
        {
            throw new AppException(400, "INVALID_GRADE", "Grade must be between 0 and 100");
        }

        var existing = await db.AssessmentGrades
            .FirstOrDefaultAsync(g =>
                g.AssessmentElementId == assessmentElementId && g.StudentId == studentId && !g.IsDeleted);

        if (existing is null)
        {
            var grade = new AssessmentGrade
            {
                AssessmentElementId = assessmentElementId,
                StudentId = studentId,
                Grade = dto.Grade,
                Feedback = dto.Feedback?.Trim(),
                GradedByStaffId = staffId.Value,
                GradedAtUtc = DateTime.UtcNow
            };

            db.AssessmentGrades.Add(grade);
            await db.SaveChangesAsync();

            return new AssessmentGradeDto(
                grade.Id,
                grade.AssessmentElementId,
                grade.StudentId,
                grade.Grade,
                grade.Feedback,
                grade.GradedByStaffId,
                grade.GradedAtUtc
            );
        }

        existing.Grade = dto.Grade;
        existing.Feedback = dto.Feedback?.Trim();
        existing.GradedByStaffId = staffId.Value;
        existing.GradedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new AssessmentGradeDto(
            existing.Id,
            existing.AssessmentElementId,
            existing.StudentId,
            existing.Grade,
            existing.Feedback,
            existing.GradedByStaffId,
            existing.GradedAtUtc
        );
    }

    public async Task DeleteAssessmentGradeAsync(Guid moduleId, Guid gradeId)
    {
        await guards.EnsureTeachingStaffAsync(moduleId);

        var grade = await db.AssessmentGrades
            .Include(g => g.AssessmentElement)
            .FirstOrDefaultAsync(g => g.Id == gradeId && !g.IsDeleted);

        if (grade is null)
        {
            return;
        }

        if (grade.AssessmentElement.ModuleId != moduleId)
        {
            throw new AppException(403, "FORBIDDEN", "Grade does not belong to this module.");
        }

        grade.IsDeleted = true;
        await db.SaveChangesAsync();
    }

    public async Task PublishMarksAsync(Guid moduleId, bool publish)
    {
        await guards.EnsureTeachingStaffAsync(moduleId);

        var assessments = await db.ModuleElements
            .Where(e => e.ModuleId == moduleId && !e.IsDeleted && e.Type == ModuleElementType.Assessment)
            .ToListAsync();

        foreach (var element in assessments)
        {
            element.MarksPublished = true;
        }

        await db.SaveChangesAsync();
    }

    private static void ValidateElementPayload(ModuleElementType type, double? weight)
    {
        if (type == ModuleElementType.Assessment)
        {
            if (weight is null)
            {
                throw new AppException(400, "WEIGHT_REQUIRED", "Assessment weight is required.");
            }

            if (weight is < 0 or > 100)
            {
                throw new AppException(400, "INVALID_WEIGHT", "Assessment weight must be between 0 and 100.");
            }
        }
    }

    private async Task EnsureTotalAssessmentWeightWithinLimitAsync(Guid moduleId, double additionalWeight)
    {
        if (additionalWeight <= 0)
        {
            return;
        }

        var currentTotal = await db.ModuleElements
            .Where(e => e.ModuleId == moduleId && !e.IsDeleted && e.Type == ModuleElementType.Assessment)
            .Select(e => e.AssessmentWeight)
            .SumAsync();

        if (currentTotal + additionalWeight > 100)
        {
            throw new AppException(400, "WEIGHT_TOTAL_EXCEEDED", "Total assessment weight must not exceed 100.");
        }
    }

    private async Task CompactSortOrderAsync(Guid moduleId)
    {
        var elements = await db.ModuleElements
            .Where(e => e.ModuleId == moduleId && !e.IsDeleted)
            .OrderBy(e => e.SortOrder)
            .ToListAsync();

        for (var i = 0; i < elements.Count; i++)
        {
            elements[i].SortOrder = i + 1;
        }

        await db.SaveChangesAsync();
    }

    private async Task<ModuleElement> GetAssessmentElementOrThrowAsync(Guid moduleId, Guid assessmentElementId)
    {
        var element = await db.ModuleElements.FirstOrDefaultAsync(e =>
            e.Id == assessmentElementId && e.ModuleId == moduleId && !e.IsDeleted);

        if (element is null)
        {
            throw new AppException(404, "ASSESSMENT_NOT_FOUND", "Assessment element does not exist.");
        }

        if (element.Type != ModuleElementType.Assessment)
        {
            throw new AppException(400, "NOT_AN_ASSESSMENT", "Element is not an assessment.");
        }

        return element;
    }

    private static ModuleElementDto ToDto(ModuleElement e)
        => new(
            e.Id,
            e.SortOrder,
            e.Type,
            e.IconKey,
            e.Options.RootElement.Clone(),
            e.AssessmentWeight,
            e.MarksPublished
        );
}
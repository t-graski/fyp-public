using System.Text.Json;
using backend.models.enums;

namespace backend.dtos;

public record ModuleMemberDto(
    Guid UserId,
    string FullName,
    string StudentId,
    string Email
);

public record ModuleElementDto(
    Guid Id,
    int SortOrder,
    ModuleElementType Type,
    string? IconKey,
    JsonElement Options,
    double? AssessmentWeight,
    bool MarksPublished
);

public record CreateModuleElementDto(
    ModuleElementType Type,
    int SortOrder,
    string? IconKey,
    JsonElement Options,
    double? AssessmentWeight
);

public record UpdateModuleElementDto(
    int SortOrder,
    string? IconKey,
    JsonElement Options,
    double? AssessmentWeight
);

public record ReorderModuleElementsDto(
    IReadOnlyList<Guid> ElementIdsInOrder
);

public record ModuleDto(
    Guid Id,
    Guid CourseId,
    string CourseCode,
    string CourseTitle,
    string ModuleCode,
    string Title,
    string? Description,
    int? Credits,
    int? Level,
    int AcademicYear,
    int YearOfStudy,
    short Semester,
    bool IsCore,
    DateOnly RunsFrom,
    DateOnly RunsTo,
    DayOfWeek ScheduledDay,
    TimeOnly ScheduledStartLocal,
    TimeOnly ScheduledEndLocal,
    IReadOnlyList<ModuleMemberDto> Students,
    IReadOnlyList<ModuleMemberDto> Staff,
    IReadOnlyList<ModuleElementDto> Elements
);

public record AssessmentGradeDto(
    Guid Id,
    Guid AssessmentElementId,
    Guid StudentId,
    double Grade,
    string? Feedback,
    Guid GradedByStaffId,
    DateTime GradedAtUtc
);

public record UpsertAssessmentGradeDto(
    double Grade,
    string? Feedback
);
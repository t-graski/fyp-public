namespace backend.dtos;

public record AdminCourseDto(
    Guid Id,
    string CourseCode,
    string Title,
    string? Description,
    string? Award,
    int? DurationSemesters,
    bool IsActive
);

public record AdminCourseDetailDto(
    Guid Id,
    string CourseCode,
    string Title,
    string? Description,
    string? Award,
    int? DurationSemesters,
    bool IsActive,
    IReadOnlyList<AdminModuleDto> Modules
);

public record AdminModuleDto(
    Guid Id,
    Guid CourseId,
    string ModuleCode,
    string Title,
    string? Description,
    int? Credits,
    int? Level,
    int? AcademicYear,
    int? SemesterOfStudy,
    string? Term,
    bool IsCore,
    DateOnly RunsFrom,
    DateOnly RunsTo,
    DayOfWeek ScheduledDay,
    TimeOnly ScheduledStartLocal,
    TimeOnly ScheduledEndLocal
);

public record CreateCourseDto(
    string CourseCode,
    string Title,
    string? Description,
    string? Award,
    int? DurationSemesters,
    bool IsActive
);

public record UpdateCourseDto(
    string CourseCode,
    string Title,
    string? Description,
    string? Award,
    int? DurationSemesters,
    bool IsActive
);

public record CreateModuleDto(
    string ModuleCode,
    string Title,
    string? Description,
    int? Credits,
    int? Level,
    int? AcademicYear,
    int? SemesterOfStudy,
    string? Term,
    bool IsCore,
    DateOnly RunsFrom,
    DateOnly RunsTo,
    DayOfWeek ScheduledDay,
    TimeOnly ScheduledStartLocal,
    TimeOnly ScheduledEndLocal
);

public record UpdateModuleDto(
    string ModuleCode,
    string Title,
    string? Description,
    int? Credits,
    int? Level,
    int? AcademicYear,
    int? SemesterOfStudy,
    string? Term,
    bool IsCore,
    DateOnly RunsFrom,
    DateOnly RunsTo,
    DayOfWeek ScheduledDay,
    TimeOnly ScheduledStartLocal,
    TimeOnly ScheduledEndLocal
);
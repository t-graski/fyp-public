using backend.models.enums;

namespace backend.dtos;

public record EnrollInCourseDto(
    Guid CourseId,
    int AcademicYear,
    int YearOfStudy,
    short Semester
);

public record SetCourseEnrollmentStatusDto(
    CourseEnrollmentStatus Status
);

public record EnrollInModuleDto(
    int AcademicYear,
    int YearOfStudy,
    short Semester
);

public record SetModuleEnrollmentStatusDto(
    ModuleEnrollmentStatus Status
);

public record CourseEnrollmentDto(
    Guid CourseId,
    string CourseCode,
    string Title,
    int AcademicYear,
    int YearOfStudy,
    short Semester,
    CourseEnrollmentStatus Status
);

public record ModuleCardDto(
    Guid ModuleId,
    string ModuleCode,
    string Title,
    int AcademicYear,
    int YearOfStudy,
    short Semester,
    ModuleEnrollmentStatus Status
);

public record StudentDashboardDto(
    CourseEnrollmentDto? EnrolledCourse,
    IReadOnlyList<ModuleCardDto> CurrentModules,
    IReadOnlyList<ModuleCardDto> PastModules
);
namespace backend.dtos;

public record MyAttendanceDayModuleDto(
    Guid ModuleId,
    string ModuleName,
    bool IsAttended,
    DateTimeOffset? CheckedInAtUtc,
    TimeOnly ScheduledStartLocal,
    TimeOnly ScheduledEndLocal
);

public record MyAttendanceDayDto(
    DateOnly Date,
    int AttendedModulesCount,
    IReadOnlyList<MyAttendanceDayModuleDto> Modules
);

public record MyModuleAttendanceSummaryDto(
    Guid ModuleId,
    string ModuleName,
    DayOfWeek ScheduledDay,
    DateOnly RunsFrom,
    DateOnly RunsTo,
    int ExpectedSessions,
    int AttendedSessions,
    double? AttendancePercent
);

public record MyAttendanceOverviewDto(
    DateOnly From,
    DateOnly To,
    int ExpectedSessionsTotal,
    int AttendedSessionsTotal,
    double? OverallPercent,
    IReadOnlyList<MyModuleAttendanceSummaryDto> PerModule
);

public record MyAttendanceResponseDto(
    MyAttendanceOverviewDto Overview,
    PagedDto<MyAttendanceDayDto> Days
);

public record AdminStudentAttendanceRowDto(
    Guid StudentId,
    Guid UserId,
    string StudentNumber,
    string FirstName,
    string LastName,
    int ExpectedSessionsTotal,
    int AttendedSessionsTotal,
    double? OverallPercent
);

public record StaffModuleStudentAttendanceRowDto(
    Guid StudentId,
    string StudentNumber,
    string FirstName,
    string LastName,
    int ExpectedSessions,
    int AttendedSessions,
    double? AttendancePercent
);

public record AttendanceSettingsDto(
    TimeOnly CheckInStartLocal,
    TimeOnly CheckInEndLocal,
    string TimeZoneId
);

public record UpdateAttendanceSettingsDto(
    TimeOnly CheckInStartLocal,
    TimeOnly CheckInEndLocal,
    string TimeZoneId
);
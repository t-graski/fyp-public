using backend.dtos;

namespace backend.services.interfaces;

public interface IAttendanceService
{
    Task MarkStudentAttendanceOnLoginAsync(Guid userId, DateTimeOffset nowUtc);

    Task<MyAttendanceResponseDto> GetMyAttendanceAsync(Guid meUserId, DateOnly from, DateOnly to, int page,
        int pageSize);

    Task<PagedDto<AdminStudentAttendanceRowDto>> GetStudentsAttendanceAsync(DateOnly from, DateOnly to, string? search,
        int page, int pageSize);

    Task<MyAttendanceResponseDto> GetStudentAttendanceAsync(Guid studentId, DateOnly from, DateOnly to, int page,
        int pageSize);

    Task<PagedDto<StaffModuleStudentAttendanceRowDto>> GetModuleAttendanceAsync(Guid moduleId, DateOnly from,
        DateOnly to, string? search, int page, int pageSize);

    Task<AttendanceSettingsDto> GetSettingsAsync();
    Task<AttendanceSettingsDto> UpdateSettingsAsync(UpdateAttendanceSettingsDto dto);
}
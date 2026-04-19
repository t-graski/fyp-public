using backend.dtos;
using backend.services.interfaces;

namespace Backend.IntegrationTests.Fakes;

public class FakeAttendanceService : IAttendanceService
{
    public Task MarkStudentAttendanceOnLoginAsync(Guid userId, DateTimeOffset nowUtc)
    {
        return Task.CompletedTask;
    }

    public Task<MyAttendanceResponseDto> GetMyAttendanceAsync(Guid meUserId, DateOnly from, DateOnly to, int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    public Task<PagedDto<AdminStudentAttendanceRowDto>> GetStudentsAttendanceAsync(DateOnly from, DateOnly to, string? search, int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    public Task<MyAttendanceResponseDto> GetStudentAttendanceAsync(Guid studentId, DateOnly from, DateOnly to, int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    public Task<PagedDto<StaffModuleStudentAttendanceRowDto>> GetModuleAttendanceAsync(Guid moduleId, DateOnly from, DateOnly to, string? search, int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    public Task<AttendanceSettingsDto> GetSettingsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<AttendanceSettingsDto> UpdateSettingsAsync(UpdateAttendanceSettingsDto dto)
    {
        throw new NotImplementedException();
    }
}
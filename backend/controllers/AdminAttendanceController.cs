using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/admin/attendance")]
[Authorize]
public class AdminAttendanceController(IAttendanceService attendance) : ControllerBase
{
    [HttpGet("students")]
    [ProducesResponseType(typeof(ApiResponse<PagedDto<AdminStudentAttendanceRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudents([FromQuery] DateOnly from, [FromQuery] DateOnly to,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await attendance.GetStudentsAttendanceAsync(from, to, search, page, pageSize);
        return Ok(ApiResponse<PagedDto<AdminStudentAttendanceRowDto>>.Ok(result));
    }

    [HttpGet("students/{studentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MyAttendanceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudent(Guid studentId, [FromQuery] DateOnly from, [FromQuery] DateOnly to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await attendance.GetStudentAttendanceAsync(studentId, from, to, page, pageSize);
        return Ok(ApiResponse<MyAttendanceResponseDto>.Ok(result));
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
        => Ok(ApiResponse<AttendanceSettingsDto>.Ok(await attendance.GetSettingsAsync()));

    [HttpPut("settings")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings(UpdateAttendanceSettingsDto dto)
        => Ok(ApiResponse<AttendanceSettingsDto>.Ok(await attendance.UpdateSettingsAsync(dto)));
}
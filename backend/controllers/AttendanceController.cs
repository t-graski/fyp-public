using backend.auth;
using backend.dtos;
using backend.errors;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController(IAttendanceService attendance, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<MyAttendanceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var meId = currentUser.UserId ?? throw new AppException(401, "UNAUTHORIZED", "Authentication required.");
        var result = await attendance.GetMyAttendanceAsync(meId, from, to, page, pageSize);
        return Ok(ApiResponse<MyAttendanceResponseDto>.Ok(result));
    }
}
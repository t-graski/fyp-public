using backend.auth;
using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[ProducesResponseType(typeof(ApiResponse<StudentDashboardDto>), StatusCodes.Status200OK)]
[Route("api/me")]
public class MeController(IEnrollmentService enrollments, IUserService users, IEnrollmentQueryService enrollmentQueries)
    : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<StudentDashboardDto>), StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var userId = User.GetUserIdOrThrow();
        var dto = await enrollments.GetStudentDashboardByUserIdAsync(userId);
        return Ok(ApiResponse<StudentDashboardDto>.Ok(dto));
    }

    [HttpGet("dashboard/staff")]
    [ProducesResponseType(typeof(ApiResponse<StaffDashboardDto>), StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> StaffDashboard()
    {
        var userId = User.GetUserIdOrThrow();
        var dto = await enrollments.GetStaffDashboardByUserIdAsync(userId);
        return Ok(ApiResponse<StaffDashboardDto>.Ok(dto));
    }

    [HttpGet("grades")]
    [ProducesResponseType(typeof(ApiResponse<StudentGradesDto>), StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> MyGrades()
    {
        var userId = User.GetUserIdOrThrow();
        var dto = await enrollmentQueries.GetMyGradesAsync(userId);
        return Ok(ApiResponse<StudentGradesDto>.Ok(dto));
    }
}
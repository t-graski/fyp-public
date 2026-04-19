using backend.auth;
using backend.dtos;
using backend.models;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController(IEnrollmentService enrollments) : ControllerBase
{
    [HttpPost("students/{studentId:guid}/course")]
    [ProducesResponseType(typeof(ApiResponse<CourseEnrollmentDto>), StatusCodes.Status201Created)]
    [Authorize]
    [RequirePermission(Permission.EnrollmentWrite)]
    public async Task<IActionResult> EnrollInCourse(Guid studentId, EnrollInCourseDto dto)
    {
        var result = await enrollments.EnrollStudentInCourseAsync(studentId, dto);
        return StatusCode(201, ApiResponse<CourseEnrollmentDto>.Ok(result, 201));
    }

    [HttpPatch("student/{studentId:guid}/course/status")]
    [ProducesResponseType(typeof(ApiResponse<CourseEnrollmentDto>), StatusCodes.Status201Created)]
    [Authorize]
    [RequirePermission(Permission.EnrollmentWrite)]
    public async Task<IActionResult> SetCourseStatus(Guid studentId, SetCourseEnrollmentStatusDto dto)
    {
        var result = await enrollments.SetCourseEnrollmentStatusAsync(studentId, dto.Status);
        return Ok(ApiResponse<CourseEnrollmentDto>.Ok(result));
    }

    [HttpPost("students/{studentId:guid}/modules/{moduleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ModuleCardDto>), StatusCodes.Status201Created)]
    [Authorize]
    [RequirePermission(Permission.EnrollmentWrite)]
    public async Task<IActionResult> EnrollInModule(Guid studentId, Guid moduleId, EnrollInModuleDto dto)
    {
        var result = await enrollments.EnrollStudentInModuleAsync(studentId, moduleId, dto);
        return StatusCode(201, ApiResponse<ModuleCardDto>.Ok(result, 201));
    }

    [HttpPatch("module/enrollments/{enrollmentId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<ModuleCardDto>), StatusCodes.Status200OK)]
    [Authorize]
    [RequirePermission(Permission.EnrollmentWrite)]
    public async Task<IActionResult> SetModuleStatus(Guid enrollmentId, SetModuleEnrollmentStatusDto dto)
    {
        var result = await enrollments.SetModuleEnrollmentStatusAsync(enrollmentId, dto.Status);
        return Ok(ApiResponse<ModuleCardDto>.Ok(result));
    }

    [HttpGet("students/{studentId:guid}/dashboard")]
    [ProducesResponseType(typeof(ApiResponse<StudentDashboardDto>), StatusCodes.Status200OK)]
    [Authorize]
    [RequirePermission(Permission.EnrollmentRead)]
    public async Task<IActionResult> StudentDashboard(Guid studentId)
    {
        var dto = await enrollments.GetStudentDashboardByStudentIdAsync(studentId);
        return Ok(ApiResponse<StudentDashboardDto>.Ok(dto));
    }
}
using backend.auth;
using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/admin/enrollments")]
public class EnrollmentQueriesController(IEnrollmentQueryService query) : ControllerBase
{
    [HttpGet("courses/{courseId:guid}/students")]
    [Authorize]
    [RequirePermission(Permission.EnrollmentRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CourseEnrollmentRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StudentsByCourse(Guid courseId)
    {
        var rows = await query.GetStudentsByCourseAsync(courseId);
        return Ok(ApiResponse<IReadOnlyList<CourseEnrollmentRowDto>>.Ok(rows));
    }

    [HttpGet("modules/{moduleId:guid}/students")]
    [Authorize]
    [RequirePermission(Permission.EnrollmentRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ModuleEnrollmentRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StudentsByModule(Guid moduleId)
    {
        var rows = await query.GetStudentsByModuleAsync(moduleId);
        return Ok(ApiResponse<IReadOnlyList<ModuleEnrollmentRowDto>>.Ok(rows));
    }

    [HttpGet("students/{studentId:guid}/history")]
    [Authorize]
    [RequirePermission(Permission.EnrollmentRead)]
    [ProducesResponseType(typeof(ApiResponse<StudentEnrollmentHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StudentHistory(Guid studentId)
    {
        var dto = await query.GetStudentEnrollmentHistoryAsync(studentId);
        return Ok(ApiResponse<StudentEnrollmentHistoryDto>.Ok(dto));
    }
}

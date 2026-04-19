using backend.auth;
using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/admin")]
public class AdminEnrollmentController(IAdminEnrollmentService enrollment) : ControllerBase
{
    [HttpPost("students/{studentId:guid}/course/enrollment")]
    [Authorize]
    [RequirePermission(Permission.EnrollmentWrite)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public async Task<IActionResult> EnrollCourse(Guid studentId, EnrollInCourseDto dto)
    {
        await enrollment.EnrollStudentInCourseAsync(studentId, dto);
        return StatusCode(201, ApiResponse<object>.Ok(new { }, 201));
    }

    [HttpPatch("students/{studentId:guid}/course/enrollment/status")]
    [Authorize]
    [RequirePermission(Permission.EnrollmentWrite)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetCourseStatus(Guid studentId, SetCourseEnrollmentStatusDto dto)
    {
        await enrollment.SetStudentCourseStatusAsync(studentId, dto.Status);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("students/{studentId:guid}/modules/{moduleId:guid}/enroll")]
    [Authorize]
    [RequirePermission(Permission.EnrollmentWrite)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public async Task<IActionResult> EnrollModule(Guid studentId, Guid moduleId, EnrollInModuleDto dto)
    {
        await enrollment.EnrollStudentInModuleAsync(studentId, moduleId, dto);
        return StatusCode(201, ApiResponse<object>.Ok(new { }, 201));
    }
    
    [HttpPost("staff/{staffId:guid}/modules/{moduleId:guid}/enroll")]
    [Authorize]
    [RequirePermission(Permission.EnrollmentWrite)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public async Task<IActionResult> EnrollStaffModule(Guid staffId, Guid moduleId, EnrollInModuleDto dto)
    {
        await enrollment.EnrollStaffInModuleAsync(staffId, moduleId, dto);
        return StatusCode(201, ApiResponse<object>.Ok(new { }, 201));
    }
    

    [HttpPatch("module/enrollments/{enrollmentId:guid}/status")]
    [Authorize]
    [RequirePermission(Permission.EnrollmentWrite)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetModuleStatus(Guid enrollmentId, SetModuleEnrollmentStatusDto dto)
    {
        await enrollment.SetModuleEnrollmentStatusAsync(enrollmentId, dto.Status);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpDelete("module/enrollments/{enrollmentId:guid}")]
    [Authorize]
    [RequirePermission(Permission.EnrollmentWrite)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteModuleEnrollment(Guid enrollmentId)
    {
        await enrollment.DeleteModuleEnrollmentAsync(enrollmentId);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
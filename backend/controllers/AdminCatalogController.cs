using backend.auth;
using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/admin")]
public class AdminCatalogController(IAdminCatalogService catalog) : ControllerBase
{
    [HttpGet("courses")]
    [Authorize]
    [RequirePermission(Permission.CatalogRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminCourseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCourses([FromQuery] string? q = null)
        => Ok(ApiResponse<IReadOnlyList<AdminCourseDto>>.Ok(await catalog.ListCoursesAsync(q)));

    [HttpGet("courses/{id:guid}")]
    [Authorize]
    [RequirePermission(Permission.CatalogRead)]
    [ProducesResponseType(typeof(ApiResponse<AdminCourseDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourse(Guid id)
        => Ok(ApiResponse<AdminCourseDetailDto>.Ok(await catalog.GetCourseAsync(id)));

    [HttpPost("courses")]
    [Authorize]
    [RequirePermission(Permission.CatalogWrite)]
    [ProducesResponseType(typeof(ApiResponse<AdminCourseDetailDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCourse(CreateCourseDto dto)
    {
        var created = await catalog.CreateCourseAsync(dto);
        return StatusCode(201, ApiResponse<AdminCourseDetailDto>.Ok(created, 201));
    }

    [HttpPut("courses/{id:guid}")]
    [Authorize]
    [RequirePermission(Permission.CatalogWrite)]
    [ProducesResponseType(typeof(ApiResponse<AdminCourseDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCourse(Guid id, UpdateCourseDto dto)
        => Ok(ApiResponse<AdminCourseDetailDto>.Ok(await catalog.UpdateCourseAsync(id, dto)));

    [HttpDelete("courses/{id:guid}")]
    [Authorize]
    [RequirePermission(Permission.CatalogDelete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCourse(Guid id)
    {
        await catalog.DeleteCourseAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpGet("courses/{courseId:guid}/modules")]
    [Authorize]
    [RequirePermission(Permission.CatalogRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminModuleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListModules(Guid courseId)
        => Ok(ApiResponse<IReadOnlyList<AdminModuleDto>>.Ok(await catalog.ListModulesByCourseAsync(courseId)));

    [HttpGet("modules/{id:guid}")]
    [Authorize]
    [RequirePermission(Permission.CatalogRead)]
    [ProducesResponseType(typeof(ApiResponse<AdminModuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModule(Guid id)
        => Ok(ApiResponse<AdminModuleDto>.Ok(await catalog.GetModuleAsync(id)));

    [HttpPost("courses/{courseId:guid}/modules")]
    [Authorize]
    [RequirePermission(Permission.CatalogWrite)]
    [ProducesResponseType(typeof(ApiResponse<AdminModuleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateModule(Guid courseId, CreateModuleDto dto)
    {
        var created = await catalog.CreateModuleAsync(courseId, dto);
        return StatusCode(201, ApiResponse<AdminModuleDto>.Ok(created, 201));
    }

    [HttpPut("modules/{id:guid}")]
    [Authorize]
    [RequirePermission(Permission.CatalogWrite)]
    [ProducesResponseType(typeof(ApiResponse<AdminModuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateModule(Guid id, UpdateModuleDto dto)
        => Ok(ApiResponse<AdminModuleDto>.Ok(await catalog.UpdateModuleAsync(id, dto)));

    [HttpDelete("modules/{id:guid}")]
    [Authorize]
    [RequirePermission(Permission.CatalogDelete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteModule(Guid id)
    {
        await catalog.DeleteModuleAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
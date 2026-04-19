using backend.auth;
using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUserController(IAdminUserService users) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminUserListItemDto>>), StatusCodes.Status200OK)]
    [Authorize]
    [RequirePermission(Permission.UserRead)]
    public async Task<IActionResult> List([FromQuery] string? q = null, [FromQuery] int limit = 50,
        [FromQuery] int offset = 0) =>
        Ok(ApiResponse<IReadOnlyList<AdminUserListItemDto>>.Ok(await users.ListAsync(q, limit, offset)));

    [HttpGet("{userId:guid}")]
    [Authorize]
    [RequirePermission(Permission.UserRead)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid userId)
        => Ok(ApiResponse<AdminUserDetailDto>.Ok(await users.GetAsync(userId)));

    [HttpPost]
    [Authorize]
    [RequirePermission(Permission.UserWrite)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(AdminCreateUserDto dto)
    {
        var created = await users.CreateAsync(dto);
        return StatusCode(201, ApiResponse<AdminUserDetailDto>.Ok(created, 201));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [RequirePermission(Permission.UserWrite)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, AdminUpdateUserDto dto)
        => Ok(ApiResponse<AdminUserDetailDto>.Ok(await users.UpdateAsync(id, dto)));

    [HttpPatch("{id:guid}/active")]
    [Authorize]
    [RequirePermission(Permission.UserWrite)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetActive(Guid id, SetUserActiveDto dto)
    {
        await users.SetActiveAsync(id, dto.IsActive);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpDelete("{userId:guid}")]
    [Authorize]
    [RequirePermission((Permission.UserWrite))]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid userId)
    {
        await users.DeleteAsync(userId);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("{id:guid}/permissions/recompute")]
    [Authorize]
    [RequirePermission(Permission.UserWrite)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecomputePerms(Guid id)
    {
        await users.RecomputePermissionsAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPut("users/{userId:guid}/student-record")]
    [Authorize]
    [RequirePermission(Permission.UserWrite)]
    public async Task<IActionResult> UpsertStudentRecord(Guid userId, AdminUpsertStudentRecordDto dto)
        => Ok(ApiResponse<AdminStudentRecordDto>.Ok(await users.UpsertStudentRecordAsync(userId, dto)));

    [HttpDelete("users/{userId:guid}/student-record")]
    [Authorize]
    [RequirePermission(Permission.UserWrite)]
    public async Task<IActionResult> DeleteStudentRecord(Guid userId)
    {
        await users.DeleteStudentRecordAsync(userId);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
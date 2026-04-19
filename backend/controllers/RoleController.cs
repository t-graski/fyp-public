using backend.auth;
using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RoleController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(Permission.RoleRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var roles = await roleService.ListAsync();
        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(roles));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.RoleRead)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var role = await roleService.GetAsync(id);
        return Ok(ApiResponse<RoleDto>.Ok(role));
    }

    [HttpPost]
    [RequirePermission(Permission.RoleWrite)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
    {
        var role = await roleService.CreateAsync(dto);
        return Ok(ApiResponse<RoleDto>.Ok(role));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permission.RoleWrite)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleDto dto)
    {
        var role = await roleService.UpdateAsync(id, dto);
        return Ok(ApiResponse<RoleDto>.Ok(role));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.RoleDelete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await roleService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
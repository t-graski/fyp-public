using backend.auth;
using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionMetadataDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await roleService.GetPermissionsMetadataAsync();
        return Ok(ApiResponse<IReadOnlyList<PermissionMetadataDto>>.Ok(permissions));
    }
}
using backend.auth;
using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/admin/audit")]
public class AdminAuditController(IAuditService audit) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [RequirePermission(Permission.AuditRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuditEventDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? actorUserId,
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] string? action,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset toUtc,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var rows = await audit.SearchAsync(actorUserId, entityType, entityId, action, fromUtc, toUtc, limit, offset);
        return Ok(ApiResponse<IReadOnlyList<AuditEventDto>>.Ok(rows));
    }

    [HttpGet("logins")]
    [Authorize]
    [RequirePermission(Permission.AuditRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LoginEventDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchLogin([FromQuery] Guid? actorUserId, [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        var rows = await audit.SearchLoginAsync(actorUserId, limit, offset);
        return Ok(ApiResponse<IReadOnlyList<LoginEventDto>>.Ok(rows));
    }
}
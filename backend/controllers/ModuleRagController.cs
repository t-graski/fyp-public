using backend.auth;
using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/modules/{moduleId:guid}/rag")]
public class ModuleRagController(IModuleRagService rag) : ControllerBase
{
    [HttpPost("index")]
    [Authorize]
    [RequirePermission(Permission.None)]
    public async Task<IActionResult> Index(Guid moduleId, RagIndexRequest dto, CancellationToken ct)
    {
        await rag.IndexAsync(moduleId, dto.Rebuild, ct);
        return Ok(ApiResponse<object>.Ok(new { indexed = true }));
    }

    [HttpPost("chat")]
    [Authorize]
    [RequirePermission(Permission.SuperAdmin)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Chat(Guid moduleId, RagChatRequest dto, CancellationToken ct)
    {
        var res = await rag.ChatAsync(moduleId, dto.Message, dto.TopK, ct);
        return Ok(ApiResponse<RagChatResponse>.Ok(res));
    }
}
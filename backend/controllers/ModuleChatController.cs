using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/modules/{moduleId:guid}/chat/threads")]
public class ModuleChatController(IModuleChatService chat) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CreateThreadResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateThread(Guid moduleId, CancellationToken ct)
    {
        var id = await chat.CreateThreadAsync(moduleId, ct);
        return Ok(ApiResponse<CreateThreadResponse>.Ok(new CreateThreadResponse(id)));
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<ThreadSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThreads(Guid moduleId, CancellationToken ct)
    {
        var threads = await chat.GetThreadsASync(moduleId, ct);
        return Ok(ApiResponse<List<ThreadSummaryDto>>.Ok(threads));
    }

    [HttpGet("{threadId:guid}/messages")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<ChatMessageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(Guid moduleId, Guid threadId, [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 200);
        var messages = await chat.GetMessagesAsync(moduleId, threadId, take, ct);
        return Ok(ApiResponse<List<ChatMessageDto>>.Ok(messages));
    }

    [HttpPost("{threadId:guid}/messages")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SendMessageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Send(Guid moduleId, Guid threadId, SendMessageRequest dto, CancellationToken ct)
    {
        var res = await chat.SendAsync(moduleId, threadId, dto.Message, ct);
        return Ok(ApiResponse<SendMessageResponse>.Ok(res));
    }
}
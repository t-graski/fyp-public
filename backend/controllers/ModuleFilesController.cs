using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/modules/{moduleId:guid}/files")]
public class ModuleFilesController(IModuleFileService files) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(2_000_000)]
    [ProducesResponseType(typeof(ApiResponse<ModuleFileDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload(Guid moduleId, IFormFile file, CancellationToken ct)
    {
        var created = await files.UploadAsync(moduleId, file, ct);
        return StatusCode(201, ApiResponse<ModuleFileDto>.Ok(created, 201));
    }

    [HttpGet("{fileId:guid}/download")]
    [Authorize]
    public async Task<IActionResult> Download(Guid moduleId, Guid fileId, CancellationToken ct)
    {
        var (meta, stream) = await files.DownloadAsync(moduleId, fileId, ct);
        return File(stream, meta.ContentType, fileDownloadName: meta.OriginalFileName);
    }

    [HttpGet("{fileId:guid}/exists")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ModuleFileExistsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Exists(Guid moduleId, Guid fileId, CancellationToken ct)
        => Ok(ApiResponse<ModuleFileExistsDto>.Ok(await files.ExistsAsync(moduleId, fileId, ct)));
}
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/internal/boostrap")]
public class BootstrapController(IBootstrapService bootstrap) : ControllerBase
{
    [HttpPost("admin")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> BootstrapAdmin()
    {
        await bootstrap.Boostrap();
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpPost("propagate")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> PropagateData()
    {
        await bootstrap.Propagate();
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}
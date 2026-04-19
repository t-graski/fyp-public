using backend.dtos;
using backend.responses;
using backend.services.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IUserService users) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register(RegisterDto dto)
        => StatusCode(201, ApiResponse<AuthResultDto>.Ok(await users.RegisterAsync(dto), 201));

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginDto dto)
        => Ok(ApiResponse<AuthResultDto>.Ok(await users.LoginAsync(dto)));
}
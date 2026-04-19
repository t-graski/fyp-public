using backend.auth;
using backend.dtos;
using backend.dtos.bulk;
using backend.responses;
using backend.services.interfaces.bulk;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.controllers.bulk;

[ApiController]
[Route("api/admin/bulk")]
public class AdminBulkUserController(IAdminBulkUserService bulk) : ControllerBase
{
    [HttpPost("users:create")]
    [Authorize]
    [RequirePermission(Permission.UserWrite)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateUsers(BulkCreateUsersRequest dto)
        => Ok(ApiResponse<BulkResult<AdminUserDetailDto>>.Ok(await bulk.CreateAsync(dto)));
}
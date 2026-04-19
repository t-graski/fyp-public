using backend.dtos;
using backend.dtos.bulk;

namespace backend.services.interfaces.bulk;

public interface IAdminBulkUserService
{
    Task<BulkResult<AdminUserDetailDto>> CreateAsync(BulkCreateUsersRequest dto);
}
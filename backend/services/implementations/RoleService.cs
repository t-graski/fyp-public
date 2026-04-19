using backend.auth;
using backend.data;
using backend.dtos;
using backend.errors;
using backend.models;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class RoleService(AppDbContext db) : IRoleService
{
    public async Task<IReadOnlyList<RoleDto>> ListAsync()
    {
        return await db.Roles
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Key,
                r.Rank,
                r.IsSystem,
                r.Permissions
            ))
            .ToListAsync();
    }

    public async Task<RoleDto> GetAsync(Guid roleId)
    {
        var role = await db.Roles
            .AsNoTracking()
            .Where(r => r.Id == roleId && !r.IsDeleted)
            .FirstOrDefaultAsync();

        if (role is null)
        {
            throw new AppException(404, "ROLE_NOT_FOUND", "Role does not exist.");
        }

        return new RoleDto(role.Id, role.Name, role.Key, role.Rank, role.IsSystem, role.Permissions);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto dto)
    {
        var role = new Role
        {
            Key = Guid.NewGuid().ToString(),
            Name = dto.Name.Trim(),
            Permissions = dto.Permissions,
            Rank = 50,
            IsSystem = false
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync();

        return new RoleDto(role.Id, role.Name, role.Key, role.Rank, role.IsSystem, role.Permissions);
    }

    public async Task<RoleDto> UpdateAsync(Guid roleId, UpdateRoleDto dto)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted);

        if (role is null)
        {
            throw new AppException(404, "ROLE_NOT_FOUND", "Role does not exist.");
        }

        if (role.IsSystem)
        {
            throw new AppException(400, "CANNOT_MODIFY_SYSTEM_ROLE", "System roles cannot be modified.");
        }

        role.Name = dto.Name.Trim();
        role.Permissions = dto.Permissions;

        await db.SaveChangesAsync();
        await RecomputeUsersPermissionsAsync(roleId);

        return new RoleDto(role.Id, role.Name, role.Key, role.Rank, role.IsSystem, role.Permissions);
    }

    public async Task DeleteAsync(Guid roleId)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted);

        if (role is null)
        {
            throw new AppException(404, "ROLE_NOT_FOUND", "Role does not exist.");
        }

        if (role.IsSystem)
        {
            throw new AppException(400, "CANNOT_DELETE_SYSTEM_ROLE", "System roles cannot be deleted.");
        }

        role.IsDeleted = true;
        await db.SaveChangesAsync();
    }

    public Task<IReadOnlyList<PermissionMetadataDto>> GetPermissionsMetadataAsync()
    {
        return Task.FromResult(PermissionCalculator.GetPermissionMetadata());
    }

    private async Task RecomputeUsersPermissionsAsync(Guid roleId)
    {
        var userIds = await db.UserRoles
            .Where(ur => ur.RoleId == roleId && !ur.IsDeleted)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync();

        foreach (var userId in userIds)
        {
            var user = await db.Users
                .Include(u => u.Roles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is not null)
            {
                var roles = user.Roles
                    .Where(ur => ur is { IsDeleted: false, Role.IsDeleted: false })
                    .Select(ur => ur.Role)
                    .ToList();

                user.Permissions = PermissionCalculator.ComputeEffective(roles);
            }
        }

        await db.SaveChangesAsync();
    }
}
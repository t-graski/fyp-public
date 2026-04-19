using backend.auth;
using backend.data;
using backend.dtos;
using backend.errors;
using backend.models;
using backend.models.@base;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class UserService(AppDbContext db, ITokenService tokens, ICurrentUser currentUser, IAttendanceService attendance)
    : IUserService
{
    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var exists = await db.Users.AnyAsync(u => u.Email == email);

        if (exists)
        {
            throw new AppException(409, "EMAIL_EXISTS", "Email already exists.");
        }

        var roleKey = dto.Role switch
        {
            SystemRole.Student => "student",
            SystemRole.Staff => "staff",
            SystemRole.Admin => "admin",
            _ => "student"
        };

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Key == roleKey && !r.IsDeleted);

        if (role is null)
        {
            throw new AppException(500, "ROLE_NOT_FOUND", $"System role '{roleKey}' not found. Please run bootstrap.");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = new User
        {
            Email = email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            PasswordHash = hash,
            IsActive = true,
            Permissions = role.Permissions
        };

        db.Users.Add(user);

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        };

        db.UserRoles.Add(userRole);

        switch (dto.Role)
        {
            case SystemRole.Student:
                var student = new Student
                {
                    UserId = user.Id,
                    StudentNumber = await GenerateStudentNumber()
                };
                db.Students.Add(student);
                break;
            case SystemRole.Staff:
            case SystemRole.Admin:
                var staff = new Staff
                {
                    UserId = user.Id,
                    StaffNumber = await GenerateStaffNumber(),
                    Department = "Unassigned"
                };
                db.Staff.Add(staff);
                break;
            default:
                throw new AppException(500, "UNKNOWN_ROLE", $"Role {dto.Role} is not known.");
        }

        await db.SaveChangesAsync();
        var token = tokens.CreateAccessToken(user);
        return new AuthResultDto(user.Id, user.Email, user.FirstName, user.LastName, user.Permissions, token);
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            throw new AppException(401, "INVALID_CREDENTIALS", "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new AppException(403, "USER_DISABLED", "User is disabled.");
        }

        if (user.LockOutUntilUtc.HasValue && user.LockOutUntilUtc.Value > DateTimeOffset.UtcNow)
        {
            throw new AppException(403, "LOCKED_OUT", "User is temporarily locked out.");
        }

        var ok = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!ok)
        {
            user.FailedLoginCount += 1;
            if (user.FailedLoginCount >= 5)
            {
                user.LockOutUntilUtc = DateTimeOffset.UtcNow.AddMinutes(10);
                user.FailedLoginCount = 0;
            }

            await db.SaveChangesAsync();
            throw new AppException(401, "INVALID_CREDENTIALS", "Invalid email or password");
        }

        var nowUtc = DateTimeOffset.UtcNow;

        user.LastLoginAtUtc = nowUtc;
        user.FailedLoginCount = 0;
        user.LockOutUntilUtc = null;

        await attendance.MarkStudentAttendanceOnLoginAsync(user.Id, nowUtc);

        await db.SaveChangesAsync();

        var token = tokens.CreateAccessToken(user);
        return new AuthResultDto(user.Id, user.Email, user.FirstName, user.LastName, user.Permissions, token);
    }

    public async Task<UserDetailDto> GetMeAsync(Guid meId)
        => await GetByIdAsync(meId);

    public async Task<PagedDto<UserSummaryDto>> GetUsersAsync(int page, int pageSize)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;
        var query = db.Users.AsNoTracking();
        var total = await query.LongCountAsync();
        var items = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserSummaryDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsActive,
                u.Permissions,
                u.LastLoginAtUtc
            ))
            .ToListAsync();

        return new PagedDto<UserSummaryDto>(items, page, pageSize, total);
    }

    public async Task<UserDetailDto> GetByIdAsync(Guid userId)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Include(u => u.Roles.Where(r => !r.IsDeleted))
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Student)
            .ThenInclude(s => s.StudentRecord)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            throw new AppException(404, "USER_NOT_FOUND", "User does not exist.");
        }

        var roles = user.Roles
            .Where(r => r is { IsDeleted: false, Role.IsDeleted: false })
            .Select(r => new RoleDto(
                r.Role.Id,
                r.Role.Name,
                r.Role.Key,
                r.Role.Rank,
                r.Role.IsSystem,
                r.Role.Permissions
            ))
            .ToList();

        StudentMiniDto? studentMini = null;
        StudentRecordDto? studentRecord = null;

        if (user.Student is not null && !user.Student.IsDeleted)
        {
            studentMini = new StudentMiniDto(user.Student.Id, user.Student.StudentNumber);

            if (user.Student.StudentRecord is not null && !user.Student.StudentRecord.IsDeleted)
            {
                var r = user.Student.StudentRecord;
                studentRecord = new StudentRecordDto(
                    r.Id,
                    r.PersonalEmail,
                    r.HomeAddress,
                    r.PhoneNumber,
                    r.EntryQualifications?.ToList() ?? [],
                    r.Gender
                );
            }
        }

        return new UserDetailDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.IsActive,
            user.Permissions,
            user.CreatedAtUtc,
            user.UpdatedAtUtc,
            roles,
            studentMini,
            studentRecord
        );
    }

    public Task<UserDetailDto> CreateAsync(CreateUserDto dto)
    {
        throw new NotImplementedException();
    }

    public async Task SetStatusAsync(Guid userId, bool isActive)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new AppException(404, "USER_NOT_FOUND", "User does not exist.");
        }

        user.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    public async Task SetPermissionsAsync(Guid userId, long permissions)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new AppException(404, "USER_NOT_FOUND", "User does not exist.");
        }

        user.Permissions = permissions;
        await db.SaveChangesAsync();
    }

    public async Task ToggleRoleAsync(Guid userId, AssignRoleDto dto)
    {
        var actorId = currentUser.UserId ?? throw new AppException(401, "UNAUTHORIZED", "Authentication required.");

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == dto.RoleId && !r.IsDeleted);

        if (role is null)
        {
            throw new AppException(404, "ROLE_NOT_FOUND", $"Role '{dto.RoleId}' not found.");
        }

        var actor = await db.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == actorId);

        if (actor is null)
        {
            throw new AppException(401, "ACTOR_NOT_FOUND", "Authenticated user not found.");
        }

        var target = await db.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (target is null)
        {
            throw new AppException(404, "USER_NOT_FOUND", "Target user does not exist.");
        }

        var actorHighest = GetHighestActiveRole(actor);
        var targetHighest = GetHighestActiveRole(target);
        var actorHighestRank = actorHighest?.Rank ?? 0;
        var targetHighestRank = targetHighest?.Rank ?? 0;
        var roleRank = role.Rank;
        var actorIsAdmin = actorHighest?.Key == "admin";
        var active = target.Roles.FirstOrDefault(r => r.RoleId == role.Id && !r.IsDeleted);
        var isRemoving = active is not null;

        // Rule #1: cannot modify someone higher than self
        if (targetHighestRank > actorHighestRank)
        {
            throw new AppException(403, "ROLE_TARGET_HIGHER",
                "You cannot modify a user with a higher role than yours.");
        }

        // Rule #2: cannot modify a role higher than own highest role
        if (roleRank > actorHighestRank)
        {
            throw new AppException(
                403,
                isRemoving ? "ROLE_REMOVE_HIGHER_THAN_SELF" : "ROLE_ASSIGN_HIGHER_THAN_SELF",
                isRemoving ? "You cannot remove a role higher than your own." : "You cannot assign a role higher than your own.");
        }

        if (!isRemoving)
        {
            // Rule #3: self can't give self a higher role
            if (actorId == userId && roleRank > actorHighestRank)
            {
                throw new AppException(403, "ROLE_SELF_PROMOTION", "You cannot assign yourself a higher role.");
            }

            // Rule #4: only admins can give same-level role
            if (roleRank == actorHighestRank && !actorIsAdmin)
            {
                throw new AppException(403, "ROLE_SAME_LEVEL_ADMIN_ONLY",
                    "Only admins can assign a role at their own level.");
            }

            // Rule #5: only admins can assign admin
            if (role.Key == "admin" && !actorIsAdmin)
            {
                throw new AppException(403, "ROLE_ADMIN_REQUIRED", "Only admins can assign the admin role.");
            }

            var deleted = target.Roles.FirstOrDefault(r => r.RoleId == role.Id && r.IsDeleted);
            if (deleted is not null)
            {
                deleted.IsDeleted = false;
                deleted.DeletedAtUtc = null;
                deleted.DeletedByUserId = null;
            }
            else
            {
                target.Roles.Add(new UserRole { UserId = userId, RoleId = role.Id });
            }
        }
        else
        {
            // Rule #3: self can't remove own highest role
            if (actorId == userId && targetHighest?.Id == role.Id)
            {
                throw new AppException(403, "ROLE_SELF_LOCKOUT", "You cannot remove your own highest role.");
            }

            // Rule #4: only admins can remove admin
            if (role.Key == "admin" && !actorIsAdmin)
            {
                throw new AppException(403, "ROLE_ADMIN_REQUIRED", "Only admins can remove the admin role.");
            }

            // Rule #5: prevent removing the final active admin
            if (role.Key == "admin")
            {
                var adminCount = await db.UserRoles
                    .Where(r => !r.IsDeleted && r.RoleId == role.Id)
                    .Select(r => r.UserId)
                    .Distinct()
                    .CountAsync();
                var targetIsAdmin = target.Roles.Any(r => r is { IsDeleted: false, Role.Key: "admin" });

                if (targetIsAdmin && adminCount <= 1)
                {
                    throw new AppException(403, "ROLE_LAST_ADMIN", "You cannot remove the last admin.");
                }
            }

            active!.IsDeleted = true;
            active.DeletedAtUtc = DateTimeOffset.UtcNow;
            active.DeletedByUserId = actorId;
        }

        target.Permissions = (long)ComputePermissionsFromRoles(target);
        await db.SaveChangesAsync();
    }

    private static Role? GetHighestActiveRole(User user)
    {
        var roles = user.Roles
            .Where(r => r is { IsDeleted: false, Role.IsDeleted: false })
            .Select(r => r.Role)
            .ToList();

        Role? best = null;
        var bestRank = 0;

        foreach (var role in roles.Where(role => role.Rank > bestRank))
        {
            bestRank = role.Rank;
            best = role;
        }

        return best;
    }

    public static Permission ComputePermissionsFromRoles(User user)
    {
        var roles = user.Roles
            .Where(x => x is { IsDeleted: false, Role.IsDeleted: false })
            .Select(x => x.Role)
            .ToList();
        var effectivePerms = PermissionCalculator.ComputeEffective(roles);

        return (Permission)effectivePerms;
    }

    private async Task<string> GenerateStudentNumber()
    {
        var lastStudent = await db.Students
            .OrderByDescending(s => s.StudentNumber)
            .FirstOrDefaultAsync();

        if (lastStudent == null)
        {
            return "w1000001";
        }

        var lastNumber = int.Parse(lastStudent.StudentNumber[1..]);
        return $"w{lastNumber + 1}";
    }

    private async Task<string> GenerateStaffNumber()
    {
        var lastStaff = await db.Staff
            .OrderByDescending(s => s.StaffNumber)
            .FirstOrDefaultAsync();

        if (lastStaff == null)
        {
            return "s1001";
        }

        var lastNumber = int.Parse(lastStaff.StaffNumber[1..]);
        return $"s{lastNumber + 1}";
    }
}
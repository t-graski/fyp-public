using backend.auth;
using backend.data;
using backend.errors;
using backend.helpers.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.helpers;

public class DatabaseGuards(AppDbContext db, ICurrentUser currentUser) : IDatabaseGuards
{
    public Task<Guid> GetUserIdOrThrowAsync()
    {
        var userId = currentUser.UserId;

        if (userId is null)
        {
            throw new AppException(401, "UNAUTHORIZED", "Authentication required.");
        }

        return Task.FromResult(userId.Value);
    }

    public Task<Guid?> TryGetStudentIdAsync(Guid userId)
        => db.Students
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

    public Task<Guid?> TryGetStaffIdAsync(Guid userId)
        => db.Staff
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

    public async Task<(Guid? StudentId, Guid? StaffId)> GetStudentOrStaffProfileAsync(Guid userId)
    {
        var studentId = await TryGetStudentIdAsync(userId);
        var staffId = await TryGetStaffIdAsync(userId);

        if (studentId is null && staffId is null)
        {
            throw new AppException(404, "STUDENT_OR_STAFF_PROFILE_NOT_FOUND",
                "No student or staff profile link to this user");
        }

        return (studentId, staffId);
    }

    public async Task EnsureCanAccessModuleAsync(Guid moduleId)
    {
        var userId = await GetUserIdOrThrowAsync();
        var (studentId, staffId) = await GetStudentOrStaffProfileAsync(userId);

        var canAccess =
            (studentId is not null && await db.StudentModuleEnrollments.AnyAsync(x =>
                x.ModuleId == moduleId && x.StudentId == studentId && !x.IsDeleted))
            || (staffId is not null && await db.ModuleStaff.AnyAsync(x =>
                x.ModuleId == moduleId && x.StaffId == staffId && !x.IsDeleted));

        if (!canAccess)
        {
            throw new AppException(403, "FORBIDDEN", "You are not allowed to access this module");
        }
    }

    public async Task EnsureTeachingStaffAsync(Guid moduleId)
    {
        _ = await EnsureTeachingStaffAndGetStaffIdAsync(moduleId);
    }

    public async Task<Guid> EnsureTeachingStaffAndGetStaffIdAsync(Guid moduleId)
    {
        var userId = await GetUserIdOrThrowAsync();
        var staffId = await TryGetStaffIdAsync(userId);

        if (staffId is null)
        {
            throw new AppException(404, "STAFF_PROFILE_NOT_FOUND", "No staff profile linked to the user");
        }

        var isTeachingStaff = await db.ModuleStaff.AnyAsync(ms =>
            ms.ModuleId == moduleId && ms.StaffId == staffId && !ms.IsDeleted);

        if (!isTeachingStaff)
        {
            throw new AppException(403, "FORBIDDEN", "Only staff can modify module content.");
        }

        return staffId.Value;
    }
}
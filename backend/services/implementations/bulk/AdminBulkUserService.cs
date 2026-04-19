using System.Data;
using backend.data;
using backend.dtos;
using backend.dtos.bulk;
using backend.errors;
using backend.models;
using backend.services.interfaces.bulk;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations.bulk;

public class AdminBulkUserService(AppDbContext db) : IAdminBulkUserService
{
    public async Task<BulkResult<AdminUserDetailDto>> CreateAsync(BulkCreateUsersRequest dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
        {
            return new BulkResult<AdminUserDetailDto>(0, 0, 0, []);
        }

        if (dto.Items.Count > 2000)
        {
            throw new AppException(400, "TOO_MANY_ITEMS", "too many items in one bulk request.");
        }

        var batchSize = Math.Clamp(dto.BatchSize, 50, 500);

        var normalized = dto.Items.Select(i =>
        {
            var email = (i.Data.Email ?? "").Trim().ToLowerInvariant();
            return (i.Key, Dto: i.Data, NormalizedEmail: email);
        }).ToList();

        var precheck = new Dictionary<string, string>();

        foreach (var item in normalized)
        {
            if (string.IsNullOrWhiteSpace(item.NormalizedEmail))
            {
                precheck[item.Key] = "EMAIL_REQUIRED";
            }
            else if (string.IsNullOrWhiteSpace(item.Dto.Password))
            {
                precheck[item.Key] = "PASSWORD_REQUIRED";
            }
            else if (string.IsNullOrWhiteSpace(item.Dto.FirstName))
            {
                precheck[item.Key] = "FIRSTNAME_REQUIRED";
            }
            else if (string.IsNullOrWhiteSpace(item.Dto.LastName))
            {
                precheck[item.Key] = "LASTNAME_REQUIRED";
            }
        }

        var dupEmails = normalized
            .Where(x => !string.IsNullOrWhiteSpace(x.NormalizedEmail))
            .GroupBy(x => x.NormalizedEmail)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(x => x.Key))
            .ToHashSet();

        foreach (var dup in dupEmails)
        {
            precheck[dup] = "DUPLICATE_EMAIL_IN_REQUEST";
        }

        var candidates = normalized.Where(x => !precheck.ContainsKey(x.Key)).ToList();
        var candidateEmails = candidates.Select(x => x.NormalizedEmail).Distinct().ToList();

        var existingEmail = await db.Users
            .AsNoTracking()
            .Where(u => candidateEmails.Contains(u.Email) && !u.IsDeleted)
            .Select(u => u.Email)
            .ToHashSetAsync();

        foreach (var item in candidates)
        {
            if (existingEmail.Contains(item.NormalizedEmail))
            {
                precheck[item.Key] = "EMAIL_EXISTS";
            }
        }

        candidates = candidates.Where(x => !precheck.ContainsKey(x.Key)).ToList();

        var roleIds = candidates
            .Where(x => x.Dto.RoleId.HasValue)
            .Select(x => x.Dto.RoleId!.Value)
            .Distinct()
            .ToList();

        var rolesById = await db.Roles
            .Where(r => roleIds.Contains(r.Id) && !r.IsDeleted)
            .ToDictionaryAsync(r => r.Id);

        foreach (var item in candidates)
        {
            if (item.Dto.RoleId.HasValue && !rolesById.ContainsKey(item.Dto.RoleId.Value))
            {
                precheck[item.Key] = "ROLE_NOT_FOUND";
            }
        }

        candidates = candidates.Where(x => !precheck.ContainsKey(x.Key)).ToList();

        var precheckResults = precheck.ToDictionary(
            kvp => kvp.Key,
            kvp => new BulkItemResult<AdminUserDetailDto>(kvp.Key, false, kvp.Value, MessageFor(kvp.Value), null)
        );

        if (candidates.Count == 0)
        {
            var all = normalized.Select(x =>
                precheckResults.TryGetValue(x.Key, out var r)
                    ? r
                    : new BulkItemResult<AdminUserDetailDto>(x.Key, false, "UNKNOWN", "Unknown error.")).ToList();

            // TODO
        }

        if (dto.Atomic)
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var createdByKey = new Dictionary<string, Guid>();
            var failures = new List<BulkItemResult<AdminUserDetailDto>>();

            var ok = await CreateBatchedInternal(candidates, rolesById, createdByKey, failures, dto.DryRun);

            if (failures.Count > 0)
            {
                await tx.RollbackAsync();

                var atomicFailed = candidates.Select(x =>
                    new BulkItemResult<AdminUserDetailDto>(x.Key, false, "BULK_ATOMIC_FAILED",
                        "Atomic bulk create failed, nothing was created.")).ToList();

                var all = normalized.Select(x =>
                        precheckResults.TryGetValue(x.Key, out var r) ? r : atomicFailed.First(a => a.Key == x.Key))
                    .ToList();

                return Summarize(all);
            }

            if (!dto.DryRun)
            {
                await tx.CommitAsync();
            }

            var createdIds = createdByKey.Values.ToList();
            var details = await LoadAdminUserDetails(createdIds);

            var detailsById = details.ToDictionary(d => d.Id);
            var success = candidates.Select(x =>
                {
                    var id = createdByKey[x.Key];
                    return new BulkItemResult<AdminUserDetailDto>(x.Key, true, null, null, detailsById[id]);
                }
            ).ToList();

            var allResults = normalized.Select(x =>
                precheckResults.TryGetValue(x.Key, out var r) ? r : success.First(s => s.Key == x.Key)
            ).ToList();

            return Summarize(allResults);
        }
        else
        {
            var allResultsMap = new Dictionary<string, BulkItemResult<AdminUserDetailDto>>();

            foreach (var pr in precheckResults.Values)
            {
                allResultsMap[pr.Key] = pr;
            }

            var createdIdsByKey = new Dictionary<string, Guid>();
            for (var i = 0; i < candidates.Count; i += batchSize)
            {
                var batch = candidates.Skip(i).Take(batchSize).ToList();

                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var failures = new List<BulkItemResult<AdminUserDetailDto>>();

                await CreateBatchedInternal(batch, rolesById, createdIdsByKey, failures, dto.DryRun);

                if (failures.Count > 0 && dto.ContinueOnError)
                {
                    await tx.RollbackAsync();
                    foreach (var f in failures)
                    {
                        allResultsMap[f.Key] = f;
                    }

                    foreach (var rest in candidates.Skip(i + batch.Count))
                    {
                        allResultsMap[rest.Key] = new BulkItemResult<AdminUserDetailDto>(rest.Key, false,
                            "NOT_PROCESSED", "Stopped due to error and ContinueOnError=false.");
                    }

                    break;
                }

                if (!dto.DryRun)
                {
                    await tx.CommitAsync();
                }
                else
                {
                    await tx.RollbackAsync();
                }

                foreach (var f in failures)
                {
                    allResultsMap[f.Key] = f;
                }

                foreach (var s in batch.Where(x => !allResultsMap.ContainsKey(x.Key)))
                {
                    allResultsMap[s.Key] = new BulkItemResult<AdminUserDetailDto>(s.Key, true, null, null, null);
                }

                db.ChangeTracker.Clear();
            }

            if (!dto.DryRun && createdIdsByKey.Count > 0)
            {
                var createdIds = createdIdsByKey.Values.Distinct().ToList();
                var details = await LoadAdminUserDetails(createdIds);
                var byId = details.ToDictionary(d => d.Id);

                foreach (var kvp in createdIdsByKey)
                {
                    var key = kvp.Key;
                    var id = kvp.Value;
                    if (!byId.TryGetValue(id, out var detail)) continue;
                    var existing = allResultsMap[key];
                    allResultsMap[key] = existing with { Data = detail };
                }
            }

            var ordered = normalized.Select(x => allResultsMap[x.Key]).ToList();
            return Summarize(ordered);
        }
    }

    private async Task<bool> CreateBatchedInternal(
        List<(string Key, AdminCreateUserDto Dto, string NormalizedEmail)> items,
        Dictionary<Guid, Role> rolesById,
        Dictionary<string, Guid> createdByKey,
        List<BulkItemResult<AdminUserDetailDto>> failures,
        bool dryRun)
    {
        // Use serialize transaction to avoid collisions
        var studentCount = items.Count(x => x.Dto.RoleId.HasValue && rolesById[x.Dto.RoleId.Value].Key == "student");
        var staffCount = items.Count(x =>
            x.Dto.RoleId.HasValue && (rolesById[x.Dto.RoleId.Value].Key is "staff" or "admin"));

        var (nextStudent, nextStaff) = await GetNextNumbers(studentCount, staffCount);

        var prevAutoDetect = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            foreach (var item in items)
            {
                try
                {
                    var dto = item.Dto;

                    var user = new User
                    {
                        Email = item.NormalizedEmail,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                        FirstName = dto.FirstName.Trim(),
                        LastName = dto.LastName.Trim(),
                        IsActive = dto.IsActive,
                        Permissions = 0
                    };

                    db.Users.Add(user);

                    if (dto.RoleId.HasValue)
                    {
                        var role = rolesById[dto.RoleId.Value];

                        db.UserRoles.Add(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = role.Id
                        });

                        user.Permissions = role.Permissions;

                        if (role.Key == "student")
                        {
                            db.Students.Add(new Student
                            {
                                UserId = user.Id,
                                StudentNumber = $"w{nextStudent++}"
                            });
                        }
                        else if (role.Key is "staff" or "admin")
                        {
                            db.Staff.Add(new Staff
                            {
                                UserId = user.Id,
                                StaffNumber = $"s{nextStaff++}",
                                Department = "Unassigned"
                            });
                        }
                    }

                    createdByKey[item.Key] = user.Id;
                }
                catch
                {
                    failures.Add(new BulkItemResult<AdminUserDetailDto>(item.Key, false, "CREATE_FAILED",
                        "Failed to create user."));
                }
            }

            if (!dryRun)
            {
                await db.SaveChangesAsync();
            }

            return failures.Count == 0;
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = prevAutoDetect;
        }
    }

    private async Task<List<AdminUserDetailDto>> LoadAdminUserDetails(List<Guid> userIds)
    {
        return await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
            .Select(u => new AdminUserDetailDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.IsActive,
                u.Permissions,
                u.Roles.Where(r => !r.IsDeleted).Select(r => new RoleDto(
                    r.Role.Id,
                    r.Role.Name,
                    r.Role.Key,
                    r.Role.Rank,
                    r.Role.IsSystem,
                    r.Role.Permissions
                )).ToList(),
                u.Student != null && !u.Student.IsDeleted
                    ? new StudentMiniDto(u.Student.Id, u.Student.StudentNumber)
                    : null,
                u.Staff != null && !u.Staff.IsDeleted ? new StaffMiniDto(u.Staff.Id, u.Staff.StaffNumber) : null,
                u.Student != null && !u.Student.IsDeleted && u.Student.StudentRecord != null &&
                !u.Student.StudentRecord.IsDeleted
                    ? new AdminStudentRecordDto(
                        u.Student.StudentRecord.Id,
                        u.Student.StudentRecord.PersonalEmail,
                        u.Student.StudentRecord.HomeAddress,
                        u.Student.StudentRecord.PhoneNumber,
                        u.Student.StudentRecord.EntryQualifications.ToList(),
                        u.Student.StudentRecord.Gender
                    )
                    : null,
                u.CreatedAtUtc,
                u.LastLoginAtUtc
            ))
            .ToListAsync();
    }

    private async Task<(int nextStudent, int nextStaff)> GetNextNumbers(int allocateStudents, int allocateStaff)
    {
        var nextStudent = 1000001;
        var nextStaff = 1001;

        if (allocateStudents > 0)
        {
            var last = await db.Students
                .OrderByDescending(s => s.StudentNumber)
                .Select(s => s.StudentNumber)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(last) && last.Length > 1 && int.TryParse(last[1..], out var n))
            {
                nextStudent = n + 1;
            }
        }

        if (allocateStaff > 0)
        {
            var last = await db.Staff
                .OrderByDescending(s => s.StaffNumber)
                .Select(s => s.StaffNumber)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(last) && last.Length > 1 && int.TryParse(last[1..], out var n))
            {
                nextStaff = n + 1;
            }
        }

        return (nextStudent, nextStaff);
    }

    private static BulkResult<AdminUserDetailDto> Summarize(List<BulkItemResult<AdminUserDetailDto>> items)
    {
        var succeeded = items.Count(x => x.Success);
        var failed = items.Count - succeeded;
        return new BulkResult<AdminUserDetailDto>(items.Count, succeeded, failed, items);
    }

    private static string MessageFor(string code) => code switch
    {
        "EMAIL_REQUIRED" => "Email is required.",
        "PASSWORD_REQUIRED" => "Password is required.",
        "FIRSTNAME_REQUIRED" => "First name is required.",
        "LASTNAME_REQUIRED" => "Last name is required.",
        "DUPLICATE_EMAIL_IN_REQUEST" => "Duplicate email in request.",
        "EMAIL_EXISTS" => "Email already exists.",
        "ROLE_NOT_FOUND" => "Role not found.",
        _ => "Validation error."
    };
}
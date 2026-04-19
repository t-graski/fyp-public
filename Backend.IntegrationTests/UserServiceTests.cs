using backend.auth;
using backend.data;
using backend.dtos;
using backend.errors;
using Backend.IntegrationTests.Fakes;
using Backend.IntegrationTests.Fixtures;
using backend.models;
using backend.models.@base;
using backend.services.implementations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.IntegrationTests;

public class UserServiceTests(PostgresDbFixture fx) : IClassFixture<PostgresDbFixture>
{
    private static UserService CreateSut(AppDbContext db, FakeCurrentUser? currentUser = null)
        => new UserService(db, new FakeTokenService(), currentUser ?? new FakeCurrentUser { IsAuthenticated = false },
            new FakeAttendanceService());

    private static string Hash(string plain) => BCrypt.Net.BCrypt.HashPassword(plain);

    private static async Task<User> SeedUserAsync(AppDbContext db, string email, string password, bool isActive = true,
        int failed = 0, DateTimeOffset? lockoutUntilUtc = null, long? permissions = null,
        CancellationToken ct = default)
    {
        var user = new User
        {
            Email = email.Trim().ToLowerInvariant(),
            FirstName = "Test",
            LastName = "User",
            DateOfBirth = new DateOnly(200, 1, 1),
            PasswordHash = Hash(password),
            IsActive = isActive,
            FailedLoginCount = failed,
            LockOutUntilUtc = lockoutUntilUtc,
            Permissions = permissions ?? 0
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    private static async Task<Role> SeedRoleAsync(AppDbContext db, string key, string name, Permission permissions,
        int rank = 999, bool isDeleted = false, CancellationToken ct = default)
    {
        var role = new Role
        {
            Key = key,
            Name = name,
            Permissions = (long)permissions,
            Rank = rank,
            IsSystem = false,
            IsDeleted = isDeleted
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return role;
    }

    private static async Task<UserRole> SeedUserRoleAsync(
        AppDbContext db,
        Guid userId,
        Role role,
        bool isDeleted = false,
        CancellationToken ct = default)
    {
        var userRole = new UserRole
        {
            UserId = userId,
            Role = role,
            IsDeleted = isDeleted
        };

        db.UserRoles.Add(userRole);
        await db.SaveChangesAsync(ct);
        return userRole;
    }

    [Fact]
    public async Task RegisterAsync_normalizes_email_trim_and_lower()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var dto = new RegisterDto("   TeSt@Example.Com   ", "Test123!?", "Tobias", "Graski",
            new DateOnly(2000, 1, 1), SystemRole.Student);

        var res = await sut.RegisterAsync(dto);

        res.Email.Should().Be("test@example.com");

        var user = await db.Users.SingleAsync(u => u.Id == res.UserId, TestContext.Current.CancellationToken);
        user.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task RegisterAsync_when_email_exists_throws_409()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        await SeedUserAsync(db, "test.example.com", "Test123!?", ct: TestContext.Current.CancellationToken);

        var dto = new RegisterDto("test.example.com", "Test123!?", "Tobias", "Graski",
            new DateOnly(2000, 1, 1), SystemRole.Student);

        var act = async () => await sut.RegisterAsync(dto);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(409);
        ex.ErrorCode.Should().Be("EMAIL_EXISTS");
    }


    [Fact]
    public async Task RegisterAsync_when_role_admin_creates_staff_record()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        const string email = "admin@example.com";
        var dto = new RegisterDto(email, "Test123!?", "Admin", "User", new DateOnly(2000, 1, 1), SystemRole.Admin);

        await sut.RegisterAsync(dto);

        var user = await db.Users.SingleAsync(u => u.Email == email, TestContext.Current.CancellationToken);

        var role = await db.UserRoles.SingleAsync(r => r.UserId == user.Id, TestContext.Current.CancellationToken);
        role.Role.Key.Should().Be("admin");
        role.Role.Name.Should().Be("Administrator");


        var staff = await db.Staff.SingleAsync(s => s.UserId == user.Id, TestContext.Current.CancellationToken);
        staff.StaffNumber.Should().NotBeNullOrWhiteSpace();
        staff.Department.Should().Be("Unassigned");

        (await db.Students.AnyAsync(s => s.UserId == user.Id, TestContext.Current.CancellationToken))
            .Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_sets_permissions_from_role()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var dto = new RegisterDto("perm@example.com", "Test123!?", "Perm", "Test",
            new DateOnly(2000, 1, 1), SystemRole.Student);

        var res = await sut.RegisterAsync(dto);

        var user = await db.Users.SingleAsync(u => u.Id == res.UserId, TestContext.Current.CancellationToken);
        user.Permissions.Should().Be((long)RolePermissions.ForRole(SystemRole.Student));
        res.Permissions.Should().Be((long)RolePermissions.ForRole(SystemRole.Student));
    }

    [Fact]
    public async Task LoginAsync_when_user_not_found_throws_401()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var act = async () => await sut.LoginAsync(new LoginDto("missing@example.com", "Test123!?"));

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(401);
        ex.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task LoginAsync_when_user_disabled_throws_403()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        await SeedUserAsync(db, "disabled@example.com", "Test123!?", isActive: false,
            ct: TestContext.Current.CancellationToken);

        var act = async () => await sut.LoginAsync(new LoginDto("disabled@example.com", "Test123!?"));

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(403);
        ex.ErrorCode.Should().Be("USER_DISABLED");
    }

    [Fact]
    public async Task LoginAsync_when_locked_out_throws_403()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        await SeedUserAsync(
            db,
            "locked@example.com",
            "Test123!?",
            lockoutUntilUtc: DateTimeOffset.UtcNow.AddMinutes(5),
            ct: TestContext.Current.CancellationToken);

        var act = async () => await sut.LoginAsync(new LoginDto("locked@example.com", "Test123!?"));

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(403);
        ex.ErrorCode.Should().Be("LOCKED_OUT");
    }

    [Fact]
    public async Task LoginAsync_wrong_password_increments_failed_count_and_throws_401()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        await SeedUserAsync(db, "wp@example.com", "Correct123!?", ct: TestContext.Current.CancellationToken);

        var act = async () => await sut.LoginAsync(new LoginDto("wp@example.com", "Wrong123!?"));

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(401);
        ex.ErrorCode.Should().Be("INVALID_CREDENTIALS");

        var user = await db.Users.SingleAsync(u => u.Email == "wp@example.com", TestContext.Current.CancellationToken);
        user.FailedLoginCount.Should().Be(1);
        user.LockOutUntilUtc.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_wrong_password_on_5th_attempt_locks_out_for_10_minutes_and_resets_failed_count()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        await SeedUserAsync(db, "lock5@example.com", "Correct123!?", failed: 4,
            ct: TestContext.Current.CancellationToken);

        var before = DateTimeOffset.UtcNow;

        var act = async () => await sut.LoginAsync(new LoginDto("lock5@example.com", "Wrong123!?"));

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(401);

        var user = await db.Users.SingleAsync(u => u.Email == "lock5@example.com",
            TestContext.Current.CancellationToken);

        user.FailedLoginCount.Should().Be(0, "it resets after reaching >= 5");
        user.LockOutUntilUtc.Should().NotBeNull();

        // allow a bit of wiggle for execution time
        user.LockOutUntilUtc!.Value.Should().BeAfter(before.AddMinutes(9));
        user.LockOutUntilUtc!.Value.Should().BeBefore(before.AddMinutes(11));
    }

    [Fact]
    public async Task LoginAsync_success_resets_failed_count_and_clears_lockout_and_sets_last_login()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        await SeedUserAsync(
            db,
            "ok@example.com",
            "Correct123!?",
            failed: 3,
            lockoutUntilUtc: null,
            ct: TestContext.Current.CancellationToken);

        // also test that existing lockout gets cleared on success
        var user = await db.Users.SingleAsync(u => u.Email == "ok@example.com", TestContext.Current.CancellationToken);
        user.LockOutUntilUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var before = DateTimeOffset.UtcNow;
        var res = await sut.LoginAsync(new LoginDto("ok@example.com", "Correct123!?"));

        res.Email.Should().Be("ok@example.com");
        res.AccessToken.Should().StartWith("TEST_TOKEN_FOR");

        var updated =
            await db.Users.SingleAsync(u => u.Email == "ok@example.com", TestContext.Current.CancellationToken);
        updated.FailedLoginCount.Should().Be(0);
        updated.LockOutUntilUtc.Should().BeNull();
        updated.LastLoginAtUtc.Should().NotBeNull();
        updated.LastLoginAtUtc!.Value.Should().BeAfter(before.AddSeconds(-1));
    }

    [Fact]
    public async Task LoginAsync_email_is_normalized_trim_lower()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        await SeedUserAsync(db, "norm@example.com", "Correct123!?", ct: TestContext.Current.CancellationToken);

        var res = await sut.LoginAsync(new LoginDto("  NoRm@Example.Com ", "Correct123!?"));

        res.Email.Should().Be("norm@example.com");
    }

    [Fact]
    public async Task GetMeAsync_when_user_not_found_throws_404()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var act = async () => await sut.GetMeAsync(Guid.Empty);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(404);
        ex.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task GetByIdAsync_excludes_roles_where_user_role_link_is_deleted()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var user = await SeedUserAsync(db, "user@example.com", "Test123!?", ct: TestContext.Current.CancellationToken);
        var role1 = await SeedRoleAsync(db, "role1", "role1", Permission.AuditRead, 1,
            ct: TestContext.Current.CancellationToken);
        var role2 = await SeedRoleAsync(db, "role2", "role2", Permission.AuditRead, 2,
            ct: TestContext.Current.CancellationToken);

        await SeedUserRoleAsync(db, user.Id, role1, ct: TestContext.Current.CancellationToken);
        await SeedUserRoleAsync(db, user.Id, role2, true, ct: TestContext.Current.CancellationToken);

        var res = await sut.GetByIdAsync(user.Id);
        res.Roles.Count().Should().Be(1);
        res.Roles.First().Key.Should().Be("role1");
    }

    [Fact]
    public async Task GetByIdAsync_excludes_roles_where_role_is_deleted()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var user = await SeedUserAsync(db, "user@example.com", "Test123!?", ct: TestContext.Current.CancellationToken);
        var role1 = await SeedRoleAsync(db, "role1", "role1", Permission.AuditRead, 1,
            ct: TestContext.Current.CancellationToken);
        var role2 = await SeedRoleAsync(db, "role2", "role2", Permission.AuditRead, 2,
            true, ct: TestContext.Current.CancellationToken);

        await SeedUserRoleAsync(db, user.Id, role1, ct: TestContext.Current.CancellationToken);
        await SeedUserRoleAsync(db, user.Id, role2, ct: TestContext.Current.CancellationToken);

        var res = await sut.GetByIdAsync(user.Id);
        res.Roles.Count().Should().Be(1);
        res.Roles.First().Key.Should().Be("role1");
    }

    [Fact]
    public async Task SetStatusAsync_WhenUserExists_UpdatesIsActive()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var seed = await SeedUserAsync(db, "user@example.com", "Test123!?", ct: TestContext.Current.CancellationToken);
        await sut.SetStatusAsync(seed.Id, false);

        var user = await db.Users.SingleAsync(u => u.Id == seed.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        user.IsActive.Should().Be(false);

        await sut.SetStatusAsync(seed.Id, true);
        user = await db.Users.SingleAsync(u => u.Id == seed.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        user.IsActive.Should().Be(true);
    }

    [Fact]
    public async Task SetStatusAsync_WhenUserNotFound_ThrowsUserNotFound()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var act = async () => await sut.SetStatusAsync(Guid.Empty, false);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(404);
        ex.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task SetPermissionsAsync_WhenUserExists_UpdatesPermissions()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var seed = await SeedUserAsync(db, "user@example.com", "Test123!?", ct: TestContext.Current.CancellationToken);

        const Permission perms = Permission.AttendanceRead | Permission.AttendanceRead | Permission.AttendanceRead;
        await sut.SetPermissionsAsync(seed.Id, (long)perms);

        var user = await db.Users.SingleAsync(u => u.Id == seed.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        user.Permissions.Should().Be((long)perms);
    }

    [Fact]
    public async Task SetPermissionsAsync_WhenUserNotFound_ThrowsUserNotFound()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var act = async () => await sut.SetPermissionsAsync(Guid.Empty, 0L);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(404);
        ex.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task AssignRoleAsync_WhenSoftDeletedRoleLinkExists_ReactivatesExistingLink()
    {
        await using var db = await fx.CreateDbContextAsync();

        var adminRole = await db.Roles.SingleAsync(r => r.Key == "admin", TestContext.Current.CancellationToken);
        var staffRole = await db.Roles.SingleAsync(r => r.Key == "staff", TestContext.Current.CancellationToken);

        var actor = await SeedUserAsync(db, "actor2.admin@example.com", "Test123!?",
            permissions: adminRole.Permissions, ct: TestContext.Current.CancellationToken);
        await SeedUserRoleAsync(db, actor.Id, adminRole, ct: TestContext.Current.CancellationToken);

        var target = await SeedUserAsync(db, "target2.user@example.com", "Test123!?", permissions: 0,
            ct: TestContext.Current.CancellationToken);
        await SeedUserRoleAsync(db, target.Id, staffRole, true, ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, new FakeCurrentUser { IsAuthenticated = true, UserId = actor.Id });

        await sut.AssignRoleAsync(target.Id, SystemRole.Staff);

        var links = await db.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == target.Id && ur.Role.Key == "staff")
            .ToListAsync(TestContext.Current.CancellationToken);

        links.Should().HaveCount(1);
        links.Single().IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveRoleAsync_WhenActorIsAdmin_SoftDeletesRoleAndRecomputesPermissions()
    {
        await using var db = await fx.CreateDbContextAsync();

        var adminRole = await db.Roles.SingleAsync(r => r.Key == "admin", TestContext.Current.CancellationToken);
        var staffRole = await db.Roles.SingleAsync(r => r.Key == "staff", TestContext.Current.CancellationToken);

        var actor = await SeedUserAsync(db, "actor3.admin@example.com", "Test123!?",
            permissions: adminRole.Permissions, ct: TestContext.Current.CancellationToken);
        await SeedUserRoleAsync(db, actor.Id, adminRole, ct: TestContext.Current.CancellationToken);

        var target = await SeedUserAsync(db, "target3.user@example.com", "Test123!?",
            permissions: staffRole.Permissions, ct: TestContext.Current.CancellationToken);
        await SeedUserRoleAsync(db, target.Id, staffRole, ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, new FakeCurrentUser { IsAuthenticated = true, UserId = actor.Id });

        await sut.RemoveRoleAsync(target.Id, SystemRole.Staff);

        var link = await db.UserRoles
            .Include(ur => ur.Role)
            .SingleAsync(ur => ur.UserId == target.Id && ur.Role.Key == "staff", TestContext.Current.CancellationToken);

        link.IsDeleted.Should().BeTrue();

        var updated = await db.Users.SingleAsync(u => u.Id == target.Id, TestContext.Current.CancellationToken);
        updated.Permissions.Should().Be(0);
    }

    [Fact]
    public async Task RemoveRoleAsync_WhenTargetDoesNotHaveRole_DoesNothing()
    {
        await using var db = await fx.CreateDbContextAsync();

        var adminRole = await db.Roles.SingleAsync(r => r.Key == "admin", TestContext.Current.CancellationToken);

        var actor = await SeedUserAsync(db, "actor4.admin@example.com", "Test123!?",
            permissions: adminRole.Permissions, ct: TestContext.Current.CancellationToken);
        await SeedUserRoleAsync(db, actor.Id, adminRole, ct: TestContext.Current.CancellationToken);

        var target = await SeedUserAsync(db, "target4.user@example.com", "Test123!?", permissions: 0,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, new FakeCurrentUser { IsAuthenticated = true, UserId = actor.Id });

        var act = async () => await sut.RemoveRoleAsync(target.Id, SystemRole.Staff);
        await act.Should().NotThrowAsync();

        var targetLinks = await db.UserRoles
            .Where(ur => ur.UserId == target.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        targetLinks.Should().BeEmpty();
    }
}
using backend.auth;
using backend.data;
using backend.dtos;
using backend.errors;
using backend.models;
using Backend.IntegrationTests.Fixtures;
using backend.services.implementations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.IntegrationTests;

public class RoleServiceTests(PostgresDbFixture fx) : IClassFixture<PostgresDbFixture>
{
    private static RoleService CreateSut(AppDbContext db) => new(db);

    private static string Hash(string plain) => BCrypt.Net.BCrypt.HashPassword(plain);

    private static async Task<User> SeedUserAsync(AppDbContext db, string email, string password, long permissions = 0,
        CancellationToken ct = default)
    {
        var user = new User
        {
            Email = email.Trim().ToLowerInvariant(),
            FirstName = "Test",
            LastName = "User",
            DateOfBirth = new DateOnly(2000, 1, 1),
            PasswordHash = Hash(password),
            IsActive = true,
            Permissions = permissions
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    private static async Task<Role> SeedRoleAsync(AppDbContext db, string key, string name, Permission permissions,
        int rank = 50, bool isSystem = false, bool isDeleted = false, CancellationToken ct = default)
    {
        var role = new Role
        {
            Key = key,
            Name = name,
            Permissions = (long)permissions,
            Rank = rank,
            IsSystem = isSystem,
            IsDeleted = isDeleted
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return role;
    }

    private static async Task<UserRole> SeedUserRoleAsync(AppDbContext db, Guid userId, Guid roleId,
        bool isDeleted = false, CancellationToken ct = default)
    {
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            IsDeleted = isDeleted
        };

        db.UserRoles.Add(userRole);
        await db.SaveChangesAsync(ct);
        return userRole;
    }

    [Fact]
    public async Task ListAsync_WhenRolesExist_ReturnsNonDeletedRolesOrderedByName()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        await SeedRoleAsync(db, "role-1", "Role 1", Permission.CatalogRead, ct: TestContext.Current.CancellationToken);
        await SeedRoleAsync(db, "role-2", "Role 2", Permission.AttendanceRead,
            ct: TestContext.Current.CancellationToken);

        var result = await sut.ListAsync();

        result.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.Name));
        result.Select(r => r.Name).Should().BeInAscendingOrder();
        result.Should().Contain(r => r.Key == "role-1");
        result.Should().Contain(r => r.Key == "role-2");
    }

    [Fact]
    public async Task ListAsync_WhenRoleIsSoftDeleted_ExcludesIt()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        await SeedRoleAsync(db, "visible", "Visible", Permission.CatalogRead,
            ct: TestContext.Current.CancellationToken);
        await SeedRoleAsync(db, "deleted", "Deleted", Permission.CatalogRead, isDeleted: true,
            ct: TestContext.Current.CancellationToken);

        var result = await sut.ListAsync();

        result.Should().Contain(r => r.Key == "visible");
        result.Should().NotContain(r => r.Key == "deleted");
    }

    [Fact]
    public async Task GetAsync_WhenRoleExists_ReturnsRole()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var role = await SeedRoleAsync(db, "support", "Support", Permission.AttendanceRead,
            ct: TestContext.Current.CancellationToken);

        var result = await sut.GetAsync(role.Id);

        result.Id.Should().Be(role.Id);
        result.Key.Should().Be("support");
        result.Name.Should().Be("Support");
        result.Permissions.Should().Be((long)Permission.AttendanceRead);
    }

    [Fact]
    public async Task GetAsync_WhenRoleNotFound_ThrowsRoleNotFound()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var act = async () => await sut.GetAsync(Guid.Empty);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(404);
        ex.ErrorCode.Should().Be("ROLE_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_WhenDtoIsValid_CreatesRoleWithDefaults()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var dto = new CreateRoleDto("  Teaching Assistant  ", (long)Permission.AttendanceWrite);

        var result = await sut.CreateAsync(dto);

        result.Name.Should().Be("Teaching Assistant");
        result.Key.Should().NotBeNullOrWhiteSpace();
        result.Rank.Should().Be(50);
        result.IsSystem.Should().BeFalse();
        result.Permissions.Should().Be((long)Permission.AttendanceWrite);

        var saved = await db.Roles.SingleAsync(r => r.Id == result.Id, TestContext.Current.CancellationToken);
        saved.Name.Should().Be("Teaching Assistant");
    }

    [Fact]
    public async Task UpdateAsync_WhenRoleExists_UpdatesRoleAndRecomputesUserPermissions()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var role = await SeedRoleAsync(db, "role-1", "Role 1", Permission.AttendanceRead,
            ct: TestContext.Current.CancellationToken);
        var user = await SeedUserAsync(db, "role.user@example.com", "Test123!?", permissions: role.Permissions,
            ct: TestContext.Current.CancellationToken);
        await SeedUserRoleAsync(db, user.Id, role.Id, ct: TestContext.Current.CancellationToken);

        var dto = new UpdateRoleDto("Role 2", (long)Permission.AttendanceWrite);

        var result = await sut.UpdateAsync(role.Id, dto);

        result.Name.Should().Be("Role 2");
        result.Permissions.Should().Be((long)Permission.AttendanceWrite);

        var updatedRole = await db.Roles.SingleAsync(r => r.Id == role.Id, TestContext.Current.CancellationToken);
        updatedRole.Name.Should().Be("Role 2");
        updatedRole.Permissions.Should().Be((long)Permission.AttendanceWrite);

        var updatedUser = await db.Users.SingleAsync(u => u.Id == user.Id, TestContext.Current.CancellationToken);
        updatedUser.Permissions.Should().Be((long)Permission.AttendanceWrite);
    }

    [Fact]
    public async Task UpdateAsync_WhenRoleIsSystem_ThrowsCannotModifySystemRole()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var adminRole = await db.Roles.SingleAsync(r => r.Key == "admin", TestContext.Current.CancellationToken);
        var dto = new UpdateRoleDto("New Admin Name", (long)Permission.CatalogRead);

        var act = async () => await sut.UpdateAsync(adminRole.Id, dto);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be("CANNOT_MODIFY_SYSTEM_ROLE");
    }

    [Fact]
    public async Task DeleteAsync_WhenRoleExists_SoftDeletesRole()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var role = await SeedRoleAsync(db, "delete", "Temp Delete", Permission.CatalogRead,
            ct: TestContext.Current.CancellationToken);

        await sut.DeleteAsync(role.Id);

        var deleted = await db.Roles.SingleAsync(r => r.Id == role.Id, TestContext.Current.CancellationToken);
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenRoleIsSystem_ThrowsCannotDeleteSystemRole()
    {
        await using var db = await fx.CreateDbContextAsync();
        var sut = CreateSut(db);

        var adminRole = await db.Roles.SingleAsync(r => r.Key == "admin", TestContext.Current.CancellationToken);

        var act = async () => await sut.DeleteAsync(adminRole.Id);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be("CANNOT_DELETE_SYSTEM_ROLE");
    }
}
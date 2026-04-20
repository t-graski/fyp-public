using System.Text.Json;
using backend.auth;
using backend.data;
using backend.errors;
using backend.models;
using backend.models.enums;
using backend.services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.services.implementations;

public class BootstrapService(AppDbContext db) : IBootstrapService
{
    public async Task Boostrap()
    {
        await SeedSystemRolesAsync();

        var adminRole = await db.Roles.FirstAsync(r => r.Key == "admin");
        var adminExists = await db.UserRoles.AnyAsync(r => r.RoleId == adminRole.Id && !r.IsDeleted);

        if (adminExists)
        {
            throw new AppException(409, "ADMIN_ALREADY_EXISTS", "An admin user already exists");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword("Test123!?");

        var user = new User
        {
            Email = "admin@uni.com",
            FirstName = "admin",
            LastName = "admin",
            DateOfBirth = DateOnly.MinValue,
            PasswordHash = hash,
            IsActive = true,
            Permissions = adminRole.Permissions
        };

        db.Users.Add(user);

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = adminRole.Id
        };

        db.UserRoles.Add(userRole);

        var staff = new Staff
        {
            UserId = user.Id,
            StaffNumber = "a000001",
            Department = "unassigned"
        };

        db.Staff.Add(staff);

        await db.SaveChangesAsync();
    }

    private async Task SeedSystemRolesAsync()
    {
        var rolesExist = await db.Roles.AnyAsync();
        if (rolesExist) return;
    
        var studentRole = new Role
        {
            Key = "student",
            Name = "Student",
            Permissions = (long)Permission.None,
            Rank = 10,
            IsSystem = true
        };
    
        var staffRole = new Role
        {
            Key = "staff",
            Name = "Staff",
            Permissions = (long)(Permission.CatalogRead | Permission.EnrollmentRead | Permission.AttendanceRead |
                                 Permission.AttendanceWrite),
            Rank = 20,
            IsSystem = true
        };
    
        var adminRole = new Role
        {
            Key = "admin",
            Name = "Administrator",
            Permissions = (long)Permission.SuperAdmin,
            Rank = 999,
            IsSystem = true
        };
    
        db.Roles.AddRange(studentRole, staffRole, adminRole);
        await db.SaveChangesAsync();
    }
}
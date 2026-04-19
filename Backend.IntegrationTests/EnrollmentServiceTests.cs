using backend.data;
using backend.dtos;
using backend.errors;
using backend.models;
using backend.models.enums;
using backend.services.implementations;
using Backend.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.IntegrationTests;

public class EnrollmentServiceTests(PostgresDbFixture fx) : IClassFixture<PostgresDbFixture>
{
    private static EnrollmentService CreateSut(AppDbContext db) => new(db);

    private static string Hash(string plain) => BCrypt.Net.BCrypt.HashPassword(plain);

    private static async Task<User> SeedUserAsync(AppDbContext db, string email, CancellationToken ct = default)
    {
        var user = new User
        {
            Email = email.Trim().ToLowerInvariant(),
            FirstName = "Test",
            LastName = "User",
            DateOfBirth = new DateOnly(2000, 1, 1),
            PasswordHash = Hash("Test123!?"),
            IsActive = true,
            Permissions = 0
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    private static async Task<Student> SeedStudentAsync(AppDbContext db, Guid userId, CancellationToken ct = default)
    {
        var student = new Student
        {
            UserId = userId,
            StudentNumber = $"u{Guid.NewGuid():N}"[..10],
            Status = "active",
            StartYear = 2025,
            StartMonth = 9
        };

        db.Students.Add(student);
        await db.SaveChangesAsync(ct);
        return student;
    }

    private static async Task<Course> SeedCourseAsync(AppDbContext db, string code, CancellationToken ct = default)
    {
        var course = new Course
        {
            CourseCode = code,
            Title = "Test Course",
            Description = "Test",
            Award = "BSc",
            DurationSemesters = 6,
            IsActive = true
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);
        return course;
    }

    private static async Task<Module> SeedModuleAsync(AppDbContext db, Guid courseId, string moduleCode,
        CancellationToken ct = default)
    {
        var module = new Module
        {
            CourseId = courseId,
            ModuleCode = moduleCode,
            Title = "Test Module",
            Description = "Desc",
            IsCore = true,
            AcademicYear = 2025,
            Credits = 20,
            Level = 4,
            SemesterOfStudy = 1,
            ScheduledDay = DayOfWeek.Monday,
            ScheduledStartLocal = new TimeOnly(9, 0),
            ScheduledEndLocal = new TimeOnly(11, 0),
            RunsFrom = new DateOnly(2025, 9, 1),
            RunsTo = new DateOnly(2026, 1, 31)
        };

        db.Modules.Add(module);
        await db.SaveChangesAsync(ct);
        return module;
    }

    private static async Task<StudentCourseEnrollment> SeedCourseEnrollmentAsync(AppDbContext db, Guid studentId,
        Guid courseId, int academicYear, int yearOfStudy, short semester, CourseEnrollmentStatus status,
        bool isDeleted = false, CancellationToken ct = default)
    {
        var enrollment = new StudentCourseEnrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            AcademicYear = academicYear,
            YearOfStudy = yearOfStudy,
            Semester = semester,
            Status = status,
            IsDeleted = isDeleted
        };

        db.StudentCourseEnrollments.Add(enrollment);
        await db.SaveChangesAsync(ct);
        return enrollment;
    }

    private static async Task<StudentModuleEnrollment> SeedModuleEnrollmentAsync(AppDbContext db, Guid studentId,
        Guid moduleId, int academicYear, int yearOfStudy, short semester, ModuleEnrollmentStatus status,
        bool isDeleted = false, CancellationToken ct = default)
    {
        var enrollment = new StudentModuleEnrollment
        {
            StudentId = studentId,
            ModuleId = moduleId,
            AcademicYear = academicYear,
            YearOfStudy = yearOfStudy,
            Semester = semester,
            Status = status,
            IsDeleted = isDeleted
        };

        db.StudentModuleEnrollments.Add(enrollment);
        await db.SaveChangesAsync(ct);
        return enrollment;
    }

    [Fact]
    public async Task EnrollStudentInCourseAsync_WhenValidRequest_CreatesActiveEnrollment()
    {
        await using var db = await fx.CreateDbContextAsync();

        var user = await SeedUserAsync(db, "enroll.course.ok@example.com", TestContext.Current.CancellationToken);
        var student = await SeedStudentAsync(db, user.Id, TestContext.Current.CancellationToken);
        var course = await SeedCourseAsync(db, "CS-ENROLL-1", TestContext.Current.CancellationToken);

        var sut = CreateSut(db);
        var dto = new EnrollInCourseDto(course.Id, 2025, 1, 1);

        var result = await sut.EnrollStudentInCourseAsync(student.Id, dto);

        result.CourseId.Should().Be(course.Id);
        result.AcademicYear.Should().Be(2025);
        result.YearOfStudy.Should().Be(1);
        result.Semester.Should().Be(1);
        result.Status.Should().Be(CourseEnrollmentStatus.Active);

        var row = await db.StudentCourseEnrollments
            .SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id,
                TestContext.Current.CancellationToken);
        row.Status.Should().Be(CourseEnrollmentStatus.Active);
    }

    [Fact]
    public async Task EnrollStudentInCourseAsync_WhenActiveEnrollmentExists_ThrowsConflict()
    {
        await using var db = await fx.CreateDbContextAsync();

        var user = await SeedUserAsync(db, "enroll.course.conflict@example.com", TestContext.Current.CancellationToken);
        var student = await SeedStudentAsync(db, user.Id, TestContext.Current.CancellationToken);
        var activeCourse = await SeedCourseAsync(db, "CS-ENROLL-2A", TestContext.Current.CancellationToken);
        var newCourse = await SeedCourseAsync(db, "CS-ENROLL-2B", TestContext.Current.CancellationToken);

        await SeedCourseEnrollmentAsync(db, student.Id, activeCourse.Id, 2025, 1, 1, CourseEnrollmentStatus.Active,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db);
        var dto = new EnrollInCourseDto(newCourse.Id, 2025, 1, 1);

        var act = async () => await sut.EnrollStudentInCourseAsync(student.Id, dto);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(409);
        ex.ErrorCode.Should().Be("COURSE_ENROLLMENT_EXISTS");
    }

    [Fact]
    public async Task EnrollStudentInModuleAsync_WhenStudentHasActiveCourseAndModuleBelongsToCourse_CreatesEnrollment()
    {
        await using var db = await fx.CreateDbContextAsync();

        var user = await SeedUserAsync(db, "enroll.module.ok@example.com", TestContext.Current.CancellationToken);
        var student = await SeedStudentAsync(db, user.Id, TestContext.Current.CancellationToken);
        var course = await SeedCourseAsync(db, "CS-MOD-1", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-ENROLL-1", TestContext.Current.CancellationToken);

        await SeedCourseEnrollmentAsync(db, student.Id, course.Id, 2025, 1, 1, CourseEnrollmentStatus.Active,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db);
        var dto = new EnrollInModuleDto(2025, 1, 1);

        var result = await sut.EnrollStudentInModuleAsync(student.Id, module.Id, dto);

        result.ModuleId.Should().Be(module.Id);
        result.AcademicYear.Should().Be(2025);
        result.YearOfStudy.Should().Be(1);
        result.Semester.Should().Be(1);
        result.Status.Should().Be(ModuleEnrollmentStatus.Enrolled);

        var row = await db.StudentModuleEnrollments
            .SingleAsync(x => x.StudentId == student.Id && x.ModuleId == module.Id,
                TestContext.Current.CancellationToken);
        row.Status.Should().Be(ModuleEnrollmentStatus.Enrolled);
    }

    [Fact]
    public async Task EnrollStudentInModuleAsync_WhenModuleIsNotInActiveCourse_ThrowsValidationError()
    {
        await using var db = await fx.CreateDbContextAsync();

        var user = await SeedUserAsync(db, "enroll.module.badcourse@example.com", TestContext.Current.CancellationToken);
        var student = await SeedStudentAsync(db, user.Id, TestContext.Current.CancellationToken);
        var courseA = await SeedCourseAsync(db, "CS-MOD-2A", TestContext.Current.CancellationToken);
        var courseB = await SeedCourseAsync(db, "CS-MOD-2B", TestContext.Current.CancellationToken);
        var foreignModule = await SeedModuleAsync(db, courseB.Id, "MOD-ENROLL-2", TestContext.Current.CancellationToken);

        await SeedCourseEnrollmentAsync(db, student.Id, courseA.Id, 2025, 1, 1, CourseEnrollmentStatus.Active,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db);
        var dto = new EnrollInModuleDto(2025, 1, 1);

        var act = async () => await sut.EnrollStudentInModuleAsync(student.Id, foreignModule.Id, dto);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(409);
        ex.ErrorCode.Should().Be("MODULE_NOT_IN_COURSE");
    }
}


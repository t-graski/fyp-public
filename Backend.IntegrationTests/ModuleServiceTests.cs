using System.Text.Json;
using backend.data;
using backend.dtos;
using backend.errors;
using backend.helpers;
using backend.models;
using backend.models.enums;
using backend.services.implementations;
using Backend.IntegrationTests.Fakes;
using Backend.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.IntegrationTests;

public class ModuleServiceTests(PostgresDbFixture fx) : IClassFixture<PostgresDbFixture>
{
    private static ModuleService CreateSut(AppDbContext db, Guid userId)
    {
        var currentUser = new FakeCurrentUser { IsAuthenticated = true, UserId = userId };
        var guards = new DatabaseGuards(db, currentUser);
        return new ModuleService(db, currentUser, guards);
    }

    private static JsonElement JsonOptions(string json)
        => JsonDocument.Parse(json).RootElement.Clone();

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

    private static async Task<Staff> SeedStaffAsync(AppDbContext db, Guid userId, CancellationToken ct = default)
    {
        var staff = new Staff
        {
            UserId = userId,
            StaffNumber = $"s{Guid.NewGuid():N}"[..10],
            Department = "Test"
        };

        db.Staff.Add(staff);
        await db.SaveChangesAsync(ct);
        return staff;
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

    private static async Task<ModuleStaff> SeedModuleStaffAsync(AppDbContext db, Guid moduleId, Guid staffId,
        CancellationToken ct = default)
    {
        var moduleStaff = new ModuleStaff
        {
            ModuleId = moduleId,
            StaffId = staffId,
            Role = "Lecturer"
        };

        db.ModuleStaff.Add(moduleStaff);
        await db.SaveChangesAsync(ct);
        return moduleStaff;
    }

    private static async Task<StudentModuleEnrollment> SeedStudentEnrollmentAsync(AppDbContext db, Guid moduleId,
        Guid studentId, CancellationToken ct = default)
    {
        var enrollment = new StudentModuleEnrollment
        {
            ModuleId = moduleId,
            StudentId = studentId,
            AcademicYear = 2025,
            YearOfStudy = 1,
            Semester = 1,
            Status = ModuleEnrollmentStatus.Enrolled
        };

        db.StudentModuleEnrollments.Add(enrollment);
        await db.SaveChangesAsync(ct);
        return enrollment;
    }

    private static async Task<ModuleElement> SeedElementAsync(AppDbContext db, Guid moduleId, ModuleElementType type,
        int sortOrder, double? assessmentWeight = null, bool marksPublished = false, string? iconKey = "icon",
        CancellationToken ct = default)
    {
        var element = new ModuleElement
        {
            ModuleId = moduleId,
            Type = type,
            SortOrder = sortOrder,
            IconKey = iconKey,
            Options = JsonDocument.Parse("{\"title\":\"x\"}"),
            AssessmentWeight = assessmentWeight,
            MarksPublished = marksPublished
        };

        db.ModuleElements.Add(element);
        await db.SaveChangesAsync(ct);
        return element;
    }

    private static async Task<AssessmentGrade> SeedGradeAsync(AppDbContext db, Guid assessmentElementId, Guid studentId,
        Guid gradedByStaffId, double grade = 70, string? feedback = "Good", CancellationToken ct = default)
    {
        var row = new AssessmentGrade
        {
            AssessmentElementId = assessmentElementId,
            StudentId = studentId,
            Grade = grade,
            Feedback = feedback,
            GradedByStaffId = gradedByStaffId,
            GradedAtUtc = DateTime.UtcNow
        };

        db.AssessmentGrades.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    [Fact]
    public async Task GetByIdAsync_WhenTeachingStaffAssigned_ReturnsModule()
    {
        await using var db = await fx.CreateDbContextAsync();

        var user = await SeedUserAsync(db, "staff.get@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, user.Id, TestContext.Current.CancellationToken);
        var studentUser = await SeedUserAsync(db, "student.get@example.com", TestContext.Current.CancellationToken);
        var student = await SeedStudentAsync(db, studentUser.Id, TestContext.Current.CancellationToken);
        var course = await SeedCourseAsync(db, "CS-GET-1", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-GET-1", TestContext.Current.CancellationToken);

        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);
        await SeedStudentEnrollmentAsync(db, module.Id, student.Id, TestContext.Current.CancellationToken);
        await SeedElementAsync(db, module.Id, ModuleElementType.Text, 2, ct: TestContext.Current.CancellationToken);
        await SeedElementAsync(db, module.Id, ModuleElementType.Headline, 1, ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, user.Id);

        var result = await sut.GetByIdAsync(module.Id);

        result.Id.Should().Be(module.Id);
        result.ModuleCode.Should().Be("MOD-GET-1");
        result.Elements.Select(e => e.SortOrder).Should().ContainInOrder(1, 2);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserCannotAccessModule_ThrowsForbidden()
    {
        await using var db = await fx.CreateDbContextAsync();

        var user = await SeedUserAsync(db, "staff.noaccess@example.com", TestContext.Current.CancellationToken);
        await SeedStaffAsync(db, user.Id, TestContext.Current.CancellationToken);

        var course = await SeedCourseAsync(db, "CS-GET-2", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-GET-2", TestContext.Current.CancellationToken);

        var sut = CreateSut(db, user.Id);

        var act = async () => await sut.GetByIdAsync(module.Id);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(403);
        ex.ErrorCode.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task CreateElementAsync_WhenAssessmentIsValid_CreatesAtNextSortOrder()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.create@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);
        var course = await SeedCourseAsync(db, "CS-CREATE-1", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-CREATE-1", TestContext.Current.CancellationToken);

        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);
        await SeedElementAsync(db, module.Id, ModuleElementType.Text, 1, ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);
        var dto = new CreateModuleElementDto(
            ModuleElementType.Assessment,
            SortOrder: 0,
            IconKey: "assignment",
            Options: JsonOptions("{\"title\":\"CW1\"}"),
            AssessmentWeight: 30);

        var created = await sut.CreateElementAsync(module.Id, dto);

        created.SortOrder.Should().Be(2);
        created.Type.Should().Be(ModuleElementType.Assessment);
        created.AssessmentWeight.Should().Be(30);
    }

    [Fact]
    public async Task CreateElementAsync_WhenTotalWeightWouldExceed100_Throws()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.create2@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);
        var course = await SeedCourseAsync(db, "CS-CREATE-2", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-CREATE-2", TestContext.Current.CancellationToken);

        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);
        await SeedElementAsync(db, module.Id, ModuleElementType.Assessment, 1, 80,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);
        var dto = new CreateModuleElementDto(
            ModuleElementType.Assessment,
            SortOrder: 0,
            IconKey: "assignment",
            Options: JsonOptions("{\"title\":\"CW2\"}"),
            AssessmentWeight: 30);

        var act = async () => await sut.CreateElementAsync(module.Id, dto);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be("WEIGHT_TOTAL_EXCEEDED");
    }

    [Fact]
    public async Task UpdateElementAsync_WhenElementExists_UpdatesElement()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.update@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);
        var course = await SeedCourseAsync(db, "CS-UPD-1", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-UPD-1", TestContext.Current.CancellationToken);

        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);
        var element = await SeedElementAsync(db, module.Id, ModuleElementType.Assessment, 1, 20,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);
        var dto = new UpdateModuleElementDto(
            SortOrder: 1,
            IconKey: "quiz",
            Options: JsonOptions("{\"title\":\"Updated\"}"),
            AssessmentWeight: 40);

        var updated = await sut.UpdateElementAsync(module.Id, element.Id, dto);

        updated.IconKey.Should().Be("quiz");
        updated.AssessmentWeight.Should().Be(40);
    }

    [Fact]
    public async Task UpdateElementAsync_WhenElementDoesNotExist_ThrowsElementNotFound()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.update2@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);
        var course = await SeedCourseAsync(db, "CS-UPD-2", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-UPD-2", TestContext.Current.CancellationToken);

        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);
        var dto = new UpdateModuleElementDto(1, "icon", JsonOptions("{}"), null);

        var act = async () => await sut.UpdateElementAsync(module.Id, Guid.Empty, dto);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(404);
        ex.ErrorCode.Should().Be("ELEMENT_NOT_FOUND");
    }

    [Fact]
    public async Task DeleteElementAsync_WhenElementMissing_DoesNothing()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.delete2@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);
        var course = await SeedCourseAsync(db, "CS-DEL-2", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-DEL-2", TestContext.Current.CancellationToken);

        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);
        var act = async () => await sut.DeleteElementAsync(module.Id, Guid.Empty);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReorderElementsAsync_WhenOrderIsValid_UpdatesSortOrder()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.reorder@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);
        var course = await SeedCourseAsync(db, "CS-RE-1", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-RE-1", TestContext.Current.CancellationToken);

        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);
        var one = await SeedElementAsync(db, module.Id, ModuleElementType.Headline, 1,
            ct: TestContext.Current.CancellationToken);
        var two = await SeedElementAsync(db, module.Id, ModuleElementType.Text, 2,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);
        var dto = new ReorderModuleElementsDto([two.Id, one.Id]);

        await sut.ReorderElementsAsync(module.Id, dto);

        var elements = await db.ModuleElements
            .Where(e => e.ModuleId == module.Id && !e.IsDeleted)
            .OrderBy(e => e.SortOrder)
            .ToListAsync(TestContext.Current.CancellationToken);

        elements.Select(e => e.Id).Should().ContainInOrder(two.Id, one.Id);
    }

    [Fact]
    public async Task ReorderElementsAsync_WhenOrderMissesAnElement_ThrowsOrderMismatch()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.reorder2@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);
        var course = await SeedCourseAsync(db, "CS-RE-2", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-RE-2", TestContext.Current.CancellationToken);

        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);
        await SeedElementAsync(db, module.Id, ModuleElementType.Headline, 1, ct: TestContext.Current.CancellationToken);
        var two = await SeedElementAsync(db, module.Id, ModuleElementType.Text, 2,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);
        var dto = new ReorderModuleElementsDto([two.Id]);

        var act = async () => await sut.ReorderElementsAsync(module.Id, dto);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be("ORDER_MISMATCH");
    }

    [Fact]
    public async Task ListAssessmentGradesAsync_WhenCurrentUserIsTeachingStaff_ReturnsAllGrades()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.listgrades@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);

        var sUser1 = await SeedUserAsync(db, "student.grade1@example.com", TestContext.Current.CancellationToken);
        var sUser2 = await SeedUserAsync(db, "student.grade2@example.com", TestContext.Current.CancellationToken);
        var student1 = await SeedStudentAsync(db, sUser1.Id, TestContext.Current.CancellationToken);
        var student2 = await SeedStudentAsync(db, sUser2.Id, TestContext.Current.CancellationToken);

        var course = await SeedCourseAsync(db, "CS-LG-1", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-LG-1", TestContext.Current.CancellationToken);
        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);

        await SeedStudentEnrollmentAsync(db, module.Id, student1.Id, TestContext.Current.CancellationToken);
        await SeedStudentEnrollmentAsync(db, module.Id, student2.Id, TestContext.Current.CancellationToken);

        var assessment = await SeedElementAsync(db, module.Id, ModuleElementType.Assessment, 1, 100, true,
            ct: TestContext.Current.CancellationToken);

        await SeedGradeAsync(db, assessment.Id, student1.Id, staff.Id, 80, ct: TestContext.Current.CancellationToken);
        await SeedGradeAsync(db, assessment.Id, student2.Id, staff.Id, 70, ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);

        var grades = await sut.ListAssessmentGradesAsync(module.Id, assessment.Id);

        grades.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAssessmentGradesAsync_WhenStudentAndMarksNotPublished_Throws()
    {
        await using var db = await fx.CreateDbContextAsync();

        var studentUser = await SeedUserAsync(db, "student.unpub@example.com", TestContext.Current.CancellationToken);
        var student = await SeedStudentAsync(db, studentUser.Id, TestContext.Current.CancellationToken);

        var course = await SeedCourseAsync(db, "CS-LG-2", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-LG-2", TestContext.Current.CancellationToken);
        await SeedStudentEnrollmentAsync(db, module.Id, student.Id, TestContext.Current.CancellationToken);

        var assessment = await SeedElementAsync(db, module.Id, ModuleElementType.Assessment, 1, 100, false,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, studentUser.Id);

        var act = async () => await sut.ListAssessmentGradesAsync(module.Id, assessment.Id);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(403);
        ex.ErrorCode.Should().Be("MARKS_NOT_PUBLISHED");
    }

    [Fact]
    public async Task UpsertAssessmentGradeAsync_WhenCalledTwice_CreatesThenUpdatesGrade()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.upsert@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);

        var studentUser = await SeedUserAsync(db, "student.upsert@example.com", TestContext.Current.CancellationToken);
        var student = await SeedStudentAsync(db, studentUser.Id, TestContext.Current.CancellationToken);

        var course = await SeedCourseAsync(db, "CS-UPS-1", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-UPS-1", TestContext.Current.CancellationToken);
        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);
        await SeedStudentEnrollmentAsync(db, module.Id, student.Id, TestContext.Current.CancellationToken);

        var assessment = await SeedElementAsync(db, module.Id, ModuleElementType.Assessment, 1, 100, false,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);

        var created = await sut.UpsertAssessmentGradeAsync(module.Id, assessment.Id, student.Id,
            new UpsertAssessmentGradeDto(65, "first"));

        var updated = await sut.UpsertAssessmentGradeAsync(module.Id, assessment.Id, student.Id,
            new UpsertAssessmentGradeDto(88, "updated"));

        created.Id.Should().Be(updated.Id);
        updated.Grade.Should().Be(88);
        updated.Feedback.Should().Be("updated");
    }

    [Fact]
    public async Task UpsertAssessmentGradeAsync_WhenGradeInvalid_ThrowsInvalidGrade()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.upsert2@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);

        var studentUser = await SeedUserAsync(db, "student.upsert2@example.com", TestContext.Current.CancellationToken);
        var student = await SeedStudentAsync(db, studentUser.Id, TestContext.Current.CancellationToken);

        var course = await SeedCourseAsync(db, "CS-UPS-2", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-UPS-2", TestContext.Current.CancellationToken);
        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);
        await SeedStudentEnrollmentAsync(db, module.Id, student.Id, TestContext.Current.CancellationToken);

        var assessment = await SeedElementAsync(db, module.Id, ModuleElementType.Assessment, 1, 100, false,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);

        var act = async () => await sut.UpsertAssessmentGradeAsync(module.Id, assessment.Id, student.Id,
            new UpsertAssessmentGradeDto(120, "bad"));

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(400);
        ex.ErrorCode.Should().Be("INVALID_GRADE");
    }

    [Fact]
    public async Task DeleteAssessmentGradeAsync_WhenGradeBelongsToModule_SoftDeletesGrade()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.delgrade@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);

        var studentUser =
            await SeedUserAsync(db, "student.delgrade@example.com", TestContext.Current.CancellationToken);
        var student = await SeedStudentAsync(db, studentUser.Id, TestContext.Current.CancellationToken);

        var course = await SeedCourseAsync(db, "CS-DG-1", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-DG-1", TestContext.Current.CancellationToken);
        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);

        var assessment = await SeedElementAsync(db, module.Id, ModuleElementType.Assessment, 1, 100, false,
            ct: TestContext.Current.CancellationToken);
        var grade = await SeedGradeAsync(db, assessment.Id, student.Id, staff.Id,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);

        await sut.DeleteAssessmentGradeAsync(module.Id, grade.Id);

        var deleted =
            await db.AssessmentGrades.SingleAsync(g => g.Id == grade.Id, TestContext.Current.CancellationToken);
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAssessmentGradeAsync_WhenGradeBelongsToAnotherModule_ThrowsForbidden()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.delgrade2@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);

        var studentUser =
            await SeedUserAsync(db, "student.delgrade2@example.com", TestContext.Current.CancellationToken);
        var student = await SeedStudentAsync(db, studentUser.Id, TestContext.Current.CancellationToken);

        var course = await SeedCourseAsync(db, "CS-DG-2", TestContext.Current.CancellationToken);
        var moduleA = await SeedModuleAsync(db, course.Id, "MOD-DG-2A", TestContext.Current.CancellationToken);
        var moduleB = await SeedModuleAsync(db, course.Id, "MOD-DG-2B", TestContext.Current.CancellationToken);

        await SeedModuleStaffAsync(db, moduleA.Id, staff.Id, TestContext.Current.CancellationToken);

        var assessmentOnB = await SeedElementAsync(db, moduleB.Id, ModuleElementType.Assessment, 1, 100, false,
            ct: TestContext.Current.CancellationToken);
        var grade = await SeedGradeAsync(db, assessmentOnB.Id, student.Id, staff.Id,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);

        var act = async () => await sut.DeleteAssessmentGradeAsync(moduleA.Id, grade.Id);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(403);
        ex.ErrorCode.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task PublishMarksAsync_WhenModuleHasAssessments_PublishesAllAssessments()
    {
        await using var db = await fx.CreateDbContextAsync();

        var staffUser = await SeedUserAsync(db, "staff.publish@example.com", TestContext.Current.CancellationToken);
        var staff = await SeedStaffAsync(db, staffUser.Id, TestContext.Current.CancellationToken);
        var course = await SeedCourseAsync(db, "CS-PUB-1", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-PUB-1", TestContext.Current.CancellationToken);

        await SeedModuleStaffAsync(db, module.Id, staff.Id, TestContext.Current.CancellationToken);
        var assessment1 = await SeedElementAsync(db, module.Id, ModuleElementType.Assessment, 1, 40, false,
            ct: TestContext.Current.CancellationToken);
        var assessment2 = await SeedElementAsync(db, module.Id, ModuleElementType.Assessment, 2, 60, false,
            ct: TestContext.Current.CancellationToken);

        var sut = CreateSut(db, staffUser.Id);

        await sut.PublishMarksAsync(module.Id, publish: false);

        var updated = await db.ModuleElements
            .Where(e => e.Id == assessment1.Id || e.Id == assessment2.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        updated.Should().OnlyContain(e => e.MarksPublished);
    }

    [Fact]
    public async Task PublishMarksAsync_WhenUserIsNotTeachingStaff_ThrowsForbidden()
    {
        await using var db = await fx.CreateDbContextAsync();

        var user = await SeedUserAsync(db, "staff.guard@example.com", TestContext.Current.CancellationToken);
        await SeedStaffAsync(db, user.Id, TestContext.Current.CancellationToken);

        var course = await SeedCourseAsync(db, "CS-PUB-2", TestContext.Current.CancellationToken);
        var module = await SeedModuleAsync(db, course.Id, "MOD-PUB-2", TestContext.Current.CancellationToken);

        var sut = CreateSut(db, user.Id);

        var act = async () => await sut.PublishMarksAsync(module.Id, publish: true);

        var ex = await Assert.ThrowsAsync<AppException>(act);
        ex.StatusCode.Should().Be(403);
        ex.ErrorCode.Should().Be("FORBIDDEN");
    }
}
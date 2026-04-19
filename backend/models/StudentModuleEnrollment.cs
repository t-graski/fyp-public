using backend.models.@base;
using backend.models.enums;

namespace backend.models;

public class StudentModuleEnrollment : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public int AcademicYear { get; set; }
    public int YearOfStudy { get; set; }
    public short Semester { get; set; }

    public ModuleEnrollmentStatus Status { get; set; } = ModuleEnrollmentStatus.Enrolled;

    public DateTimeOffset EnrolledAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
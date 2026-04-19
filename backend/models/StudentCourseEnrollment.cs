using backend.models.@base;
using backend.models.enums;

namespace backend.models;

public class StudentCourseEnrollment : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int AcademicYear { get; set; }
    public int YearOfStudy { get; set; }
    public short Semester { get; set; }

    public CourseEnrollmentStatus Status { get; set; } = CourseEnrollmentStatus.Active;

    public DateTimeOffset StartDateUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndDateUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
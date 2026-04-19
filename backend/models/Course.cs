using backend.models.@base;

namespace backend.models;

public class Course : SoftDeletableEntity<Guid>
{
    public string CourseCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;

    public string? Award { get; set; }
    public int? DurationSemesters { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Module> Modules { get; init; } = [];

    public ICollection<StudentCourseEnrollment> StudentEnrollments { get; init; } = [];
}
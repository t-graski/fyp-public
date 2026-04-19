using backend.models.@base;
using backend.models.enums;

namespace backend.models;

public class Module : SoftDeletableEntity<Guid>
{
    public Guid CourseId { get; init; }
    public Course Course { get; init; } = null!;

    public string ModuleCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;

    public bool IsCore { get; set; }
    public int? AcademicYear { get; set; }

    public int? Credits { get; set; }
    public int? Level { get; set; }
    public int? SemesterOfStudy { get; set; }
    public string? Term { get; set; }

    public DayOfWeek ScheduledDay { get; set; }
    public TimeOnly ScheduledStartLocal { get; set; }
    public TimeOnly ScheduledEndLocal { get; init; }

    public DateOnly RunsFrom { get; set; }
    public DateOnly RunsTo { get; set; }

    public ICollection<ModuleStaff> TeachingStaff { get; init; } = [];
    public ICollection<StudentModuleEnrollment> StudentEnrollments { get; init; } = [];
    public ICollection<ModuleElement> Elements { get; init; } = [];
}
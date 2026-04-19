using backend.models.@base;

namespace backend.models;

public class StudentAttendance : AuditableEntity<Guid>
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null;

    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null;

    public DateOnly Date { get; set; }
    public DateTimeOffset CheckedInAtUtc { get; set; }
}